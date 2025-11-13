using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FlashSlash 스킬 - 순간 돌진 베기
/// 우클릭으로 발동, 마우스/락온 방향으로 빠르게 돌진하며 경로상의 적들에게 데미지
/// 몬스터는 관통, 장애물은 관통 불가
/// </summary>
public class PlayerFlashSlash : MonoBehaviour
{
    #region Serialized Fields
    [Header("Dash Settings")]
    [SerializeField, Tooltip("돌진 거리")]
    private float dashDistance = 10f;

    [SerializeField, Tooltip("돌진 지속 시간 (애니메이션 시간)")]
    private float dashDuration = 0.7f;

    [SerializeField, Tooltip("스킬 쿨다운")]
    private float cooldownTime = 5f;

    [Header("Damage Settings")]
    [SerializeField, Tooltip("스킬 데미지")]
    private int damage = 50;

    [SerializeField, Tooltip("공격 범위 너비 (경로 좌우)")]
    private float pathWidth = 3f;

    [SerializeField, Tooltip("적 레이어")]
    private LayerMask enemyLayer = -1;

    [SerializeField, Tooltip("장애물 레이어 (벽, 펜스 등)")]
    private LayerMask obstacleLayer = -1;

    [Header("VFX & Audio")]
    [SerializeField, Tooltip("돌진 이펙트")]
    private GameObject dashEffectPrefab;

    [SerializeField, Tooltip("히트 이펙트")]
    private GameObject hitEffectPrefab;

    [Header("References")]
    [SerializeField] private Weapon weapon;
    #endregion

    #region Component References
    private PlayerController playerController;
    private PlayerLockOn playerLockOn;
    private Rigidbody rigid;
    private Animator anim;
    #endregion

    #region Skill State
    /// <summary>현재 스킬 실행 중인지</summary>
    private bool isSkillActive = false;

    /// <summary>스킬 사용 가능한지</summary>
    private bool isSkillReady = true;

    /// <summary>히트한 적들 (중복 히트 방지)</summary>
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
    #endregion

    #region Unity Lifecycle
    /// <summary>컴포넌트 초기화</summary>
    private void Awake()
    {
        InitializeComponents();
    }
    #endregion

    #region Initialization
    /// <summary>컴포넌트 참조 초기화</summary>
    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        playerLockOn = GetComponent<PlayerLockOn>();
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
            Debug.LogError("[PlayerFlashSlash] Animator를 찾을 수 없습니다!");

        if (playerController == null)
            Debug.LogError("[PlayerFlashSlash] PlayerController를 찾을 수 없습니다!");

        if (weapon == null)
            weapon = GetComponentInChildren<Weapon>();

        Debug.Log("[PlayerFlashSlash] 초기화 완료");
    }
    #endregion

    #region Public Interface
    /// <summary>외부에서 호출: FlashSlash 스킬 사용</summary>
    public void UseSkill()
    {
        if (!CanUseSkill())
        {
            Debug.Log("[PlayerFlashSlash] 스킬 사용 불가");
            return;
        }

        StartCoroutine(FlashSlashRoutine());
    }

    /// <summary>스킬 사용 가능 여부</summary>
    public bool CanUseSkill()
    {
        return isSkillReady &&
               !isSkillActive &&
               playerController != null &&
               !playerController.isDodging;
    }

    /// <summary>현재 스킬 실행 중인지</summary>
    public bool IsActive()
    {
        return isSkillActive;
    }
    #endregion

    #region Skill Execution
    /// <summary>FlashSlash 스킬 실행 루틴</summary>
    private IEnumerator FlashSlashRoutine()
    {
        // 1. 스킬 시작 준비
        StartSkill();

        // 2. 돌진 방향 및 목표 위치 계산
        Vector3 dashDirection = GetDashDirection();
        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculateTargetPosition(startPos, dashDirection);

        // 3. 애니메이션 시작
        if (anim != null)
        {
            anim.SetTrigger("doSpinAtt");  // Spin_Attack 애니메이션 사용
        }

        // 4. 돌진 이동
        yield return StartCoroutine(DashMovement(startPos, targetPos));

        // 5. 경로상의 적들에게 데미지 (한 번에 계산)
        DamageEnemiesInPath(startPos, targetPos, dashDirection);

        // 6. 스킬 종료
        EndSkill();

        // 7. 쿨다운
        yield return new WaitForSeconds(cooldownTime);
        isSkillReady = true;

        Debug.Log("[PlayerFlashSlash] 쿨다운 종료, 스킬 준비 완료");
    }

    /// <summary>스킬 시작 처리</summary>
    private void StartSkill()
    {
        isSkillActive = true;
        isSkillReady = false;
        hitEnemies.Clear();

        // 플레이어 이동 금지
        if (playerController != null)
        {
            playerController.isSkillCasting = true;
        }

        Debug.Log("[PlayerFlashSlash] 스킬 시작!");
    }

    /// <summary>스킬 종료 처리</summary>
    private void EndSkill()
    {
        isSkillActive = false;

        // 플레이어 이동 허용
        if (playerController != null)
        {
            playerController.isSkillCasting = false;
        }

        Debug.Log("[PlayerFlashSlash] 스킬 종료");
    }
    #endregion

    #region Direction & Position Calculation
    /// <summary>돌진 방향 계산</summary>
    private Vector3 GetDashDirection()
    {
        Vector3 direction;

        // 락온 중이면 락온된 적 방향
        if (playerLockOn != null && playerLockOn.isLockOn && playerLockOn.currentTarget != null)
        {
            direction = (playerLockOn.currentTarget.position - transform.position).normalized;
        }
        // 아니면 마우스 방향
        else if (playerController != null)
        {
            direction = playerController.GetMouseDirection();
        }
        else
        {
            direction = transform.forward;
        }

        // Y축 제거 (XZ 평면에서만 이동)
        direction.y = 0f;
        direction.Normalize();

        return direction;
    }

    /// <summary>목표 위치 계산 (장애물 체크 포함)</summary>
    private Vector3 CalculateTargetPosition(Vector3 startPos, Vector3 direction)
    {
        Vector3 targetPos = startPos + direction * dashDistance;

        // 장애물 체크 (벽, 펜스 등은 관통 불가)
        if (Physics.Raycast(startPos, direction, out RaycastHit hit, dashDistance, obstacleLayer))
        {
            // 장애물 앞에서 멈춤 (약간의 여유 공간)
            targetPos = hit.point - direction * 0.5f;
            Debug.Log($"[PlayerFlashSlash] 장애물 감지: {hit.collider.name}, 거리 조정");
        }

        // Y축 고정
        targetPos.y = startPos.y;

        return targetPos;
    }
    #endregion

    #region Movement
    /// <summary>돌진 이동 코루틴</summary>
    private IEnumerator DashMovement(Vector3 startPos, Vector3 targetPos)
    {
        float elapsed = 0f;

        // 캐릭터가 목표 방향을 바라보도록 회전
        Vector3 lookDirection = (targetPos - startPos).normalized;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // 애니메이션 재생 시간 동안 이동
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;

            // Ease Out Cubic 커브 (처음 빠르게, 끝에 천천히)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // 위치 보간
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, easedT);
            newPos.y = startPos.y; // Y축 고정

            // Rigidbody를 사용하여 이동 (물리 충돌 고려)
            if (rigid != null)
            {
                rigid.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }

            yield return null;
        }

        // 최종 위치 확정
        if (rigid != null)
        {
            rigid.MovePosition(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }

        Debug.Log($"[PlayerFlashSlash] 돌진 완료: {startPos} → {targetPos}");
    }
    #endregion

    #region Damage System
    /// <summary>경로상의 적들에게 데미지</summary>
    private void DamageEnemiesInPath(Vector3 startPos, Vector3 endPos, Vector3 direction)
    {
        // 경로 중심점 계산
        Vector3 pathCenter = (startPos + endPos) / 2f;
        float pathLength = Vector3.Distance(startPos, endPos);

        // BoxCast로 경로상의 모든 적 감지
        // 박스 크기: 길이 x 너비 x 높이
        Vector3 boxHalfExtents = new Vector3(pathWidth / 2f, 1f, pathLength / 2f);

        // 회전 (돌진 방향으로)
        Quaternion boxRotation = Quaternion.LookRotation(direction);

        // OverlapBox로 적 감지 (몬스터만, 장애물 제외)
        Collider[] hits = Physics.OverlapBox(pathCenter, boxHalfExtents, boxRotation, enemyLayer);

        Debug.Log($"[PlayerFlashSlash] 경로상 적 감지: {hits.Length}명");

        // 감지된 모든 적에게 데미지
        foreach (var hitCollider in hits)
        {
            // 중복 히트 방지
            if (hitEnemies.Contains(hitCollider.gameObject))
                continue;

            // Enemy 컴포넌트 확인
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 데미지 적용
                ApplyDamage(enemy);

                // 히트 이펙트
                SpawnHitEffect(hitCollider.transform.position);

                // 중복 히트 방지용 등록
                hitEnemies.Add(hitCollider.gameObject);
            }
        }
    }

    /// <summary>적에게 데미지 적용</summary>
    private void ApplyDamage(Enemy enemy)
    {
        if (enemy == null) return;

        // 무기 데미지 적용
        int finalDamage = damage;
        if (weapon != null)
        {
            finalDamage += weapon.damage;
        }

        // Enemy의 ApplySkillDamage 호출
        enemy.ApplySkillDamage(finalDamage, HitEffectType.Slashing_Flash);

        Debug.Log($"[PlayerFlashSlash] {enemy.name}에게 {finalDamage} 데미지!");
    }

    /// <summary>히트 이펙트 생성</summary>
    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    #endregion

    #region Debug Visualization
    /// <summary>경로 시각화 (Scene 뷰)</summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        if (isSkillActive)
        {
            // 돌진 방향 표시
            Gizmos.color = Color.cyan;
            Vector3 direction = GetDashDirection();
            Gizmos.DrawLine(transform.position, transform.position + direction * dashDistance);

            // 공격 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + direction * (dashDistance / 2f),
                                new Vector3(pathWidth, 2f, dashDistance));
        }
    }
    #endregion
}
