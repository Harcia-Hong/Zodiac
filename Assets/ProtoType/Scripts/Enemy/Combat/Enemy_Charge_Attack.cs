using System.Collections;
using UnityEngine;

/// <summary>
/// 적의 돌진(Charge) 근접 공격
/// - 쿼터뷰(XZ 평면) 전용: 모든 방향/거리 계산에서 Y 성분은 제거
/// - 이동은 Lerp로 보간하며, 시작 높이(fixedY)를 끝까지 유지
/// </summary>
public class Enemy_Charge_Attack : MonoBehaviour, IEnemyAttack
{
    [Header("Attack Settings")]
    public float attackRange = 4f;      // 공격 개시 사거리(플레이어와의 거리)
    public float attackDelay = 1.2f;    // 돌진 후 후딜레이
    public int attackDamage = 15;       // 플레이어에게 입힐 데미지

    [Header("Charge Move")]
    public float chargeDistance = 4f;   // 최대 돌진 거리
    public float chargeDuration = 0.3f; // 돌진에 소요되는 시간(초)
    public Transform moveRoot;          // 실제로 이동시킬 트랜스폼(리그/본 루트 등)

    [Header("Weapon HitBox (Animation Events)")]
    public EnemyWeapon weapon;          // 애니메이션 이벤트로 Enable/Disable

    private Enemy enemy;
    private Transform player;
    private Animator anim;
    private bool isAttacking = false;
    private bool hasHit = false;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        anim = GetComponentInChildren<Animator>();
        enemy = GetComponent<Enemy>();

        // 안전장치: moveRoot 미지정 시 자신 기준 이동
        if (moveRoot == null) moveRoot = transform;
    }

    private void Update()
    {
        if (player == null || isAttacking || enemy == null || enemy.isHit || enemy.isGroggy) return;

        var hitReactionSystem = enemy.GetComponent<EnemyHitReactionSystem>();
        // if (hitReactionSystem != null && hitReactionSystem.IsInGroggy()) return;

        // === XZ 평면에서만 거리/각도 계산 ===
        Vector3 enemyPos = transform.position; enemyPos.y = 0f;
        Vector3 playerPos = player.position; playerPos.y = 0f;

        float distance = Vector3.Distance(enemyPos, playerPos); // 평면 거리

        // Vector3.Angle(a,b): 두 벡터(단위벡터 권장) 사이의 각도(0~180)를 반환
        // - 여기서는 "정면으로 보고 있는가?" 여부 판단에 사용
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward; // 정규화 안전장치
        fwd.Normalize();

        Vector3 dirToPlayer = (playerPos - enemyPos);
        if (dirToPlayer.sqrMagnitude > 1e-6f) dirToPlayer.Normalize();

        float angle = Vector3.Angle(fwd, dirToPlayer);

        // 사거리 & 시야각 조건 만족 시 돌진 개시
        if (distance <= attackRange && angle <= 60f)
            StartCoroutine(ChargeAttackRoutine());
    }

    /// <summary>
    /// 돌진 공격 루틴
    /// - 준비(트리거) → 잠깐 대기 → 목표지점 계산(XZ) → 돌진 → 후딜레이 → 임시 대기 상태
    /// </summary>
    private IEnumerator ChargeAttackRoutine()
    {
        isAttacking = true;
        hasHit = false;

        // 돌진 시작 시 플레이어 방향으로 정확히 회전
        LookAtPlayerInstant();

        anim.SetTrigger("doChargeAttack");

        // 애니메이션 선행 동작(윈드업) 시간
        yield return new WaitForSeconds(0.3f);

        // === 돌진 목표 지점 계산 (현재 플레이어 위치 기준) ===
        Vector3 start = moveRoot.position;
        float fixedY = start.y;

        // 돌진 시작 직전 플레이어 위치로 다시 계산
        Vector3 dir = player != null ?
            (player.position - start) : transform.forward;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist > 1e-6f) dir /= dist;
        else dir = transform.forward;

        float moveDist = Mathf.Min(dist, chargeDistance);
        Vector3 targetPos = start + dir * moveDist;
        targetPos.y = fixedY;

        // 이동
        yield return StartCoroutine(ChargeMove(targetPos));

        // 후딜레이
        yield return new WaitForSeconds(attackDelay);

        isAttacking = false;

        if (enemy != null)
            enemy.SetTemporaryIdle(2f);
    }

    /// <summary>즉시 플레이어 방향으로 회전</summary>
    private void LookAtPlayerInstant()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = targetRotation;
        }
    }


    /// <summary>
    /// 돌진 이동 실행 (XZ 평면 전용)
    /// - Vector3.Lerp(a,b,t): a와 b 사이를 t(0~1) 비율로 선형 보간하는 API
    /// - 쿼터뷰에서는 수직 이동이 어색하므로 시작 높이(fixedY)를 끝까지 유지
    /// </summary>
    private IEnumerator ChargeMove(Vector3 targetPos)
    {
        float time = 0f;
        Vector3 start = moveRoot.position;
        float fixedY = start.y;

        targetPos.y = fixedY; // 목적지도 Y 고정

        while (time < chargeDuration)
        {
            float t = time / chargeDuration;     // 0 → 1
            Vector3 pos = Vector3.Lerp(start, targetPos, t);
            pos.y = fixedY;                      // 이동 중에도 Y 고정
            moveRoot.position = pos;

            time += Time.deltaTime;
            yield return null;
        }

        moveRoot.position = new Vector3(targetPos.x, fixedY, targetPos.z); // 종착점 스냅
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출: 플레이어 타격 판정
    /// - 평면 거리 기준으로 공격 범위 체크
    /// </summary>
    public void HitPlayer()
    {
        if (hasHit || player == null) return;

        Vector3 enemyPos = transform.position; enemyPos.y = 0f;
        Vector3 playerPos = player.position; playerPos.y = 0f;

        float dist = Vector3.Distance(enemyPos, playerPos); // XZ 평면 거리
        if (dist <= attackRange + 0.5f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                hasHit = true;
            }
        }
    }

    /// <summary>진행 중인 돌진 공격 강제 취소 (피격 시 호출)</summary>
    public void CancelAttack()
    {
        if (isAttacking)
        {
            // 돌진 코루틴 중단
            StopAllCoroutines();

            // 상태 리셋
            isAttacking = false;
            hasHit = false;

            // 무기 히트박스 비활성화
            weapon?.DisableHitBox();

            // 애니메이션 리셋? 필요하면 추가해보죠
            if (anim != null)
            {
                anim.SetBool("isCharging", false);
            }

            Debug.Log("[Enemy_Charge_Attack] 돌진 공격 취소됨");
        }
    }

    // 애니메이션 이벤트 훅
    public void EnableHitBox() => weapon?.EnableHitBox();
    public void DisableHitBox() => weapon?.DisableHitBox();

    public bool IsAttacking() => isAttacking;

    #region IEnemyAttack 구현

    /// <summary>
    /// 공격 시작 (FSM에서 호출)
    /// </summary>
    public void StartAttack()
    {
        if (!isAttacking && gameObject.activeInHierarchy)
        {
            // 기존 공격 로직 호출 (코루틴이나 메서드 이름에 맞게 수정)
            StartCoroutine(ChargeAttackRoutine()); // 기존 메서드명에 맞게 수정
        }
    }

    /// <summary>
    /// 공격 중단/취소
    /// </summary>
    public void StopAttack()
    {
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
            // 기존 변수들 초기화
            Debug.Log($"[Enemy_Charge_Attack] 돌진 공격 중단");
        }
    }

    /// <summary>
    /// 이 공격의 사거리 반환
    /// </summary>
    public float GetAttackRange()
    {
        return attackRange; // 기존 attackRange 변수 사용
    }

    /// <summary>
    /// 공격 쿨타임 반환
    /// </summary>
    public float GetCooldownTime()
    {
        return attackDelay; // 기존 attackDelay 변수 사용
    }

    #endregion
}
