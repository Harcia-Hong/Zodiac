using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 지상 공격 시스템 - 빠른 3단 콤보
/// 좌클릭 1번 → 자동 3타 연속 공격 (총 0.65초)
/// 우클릭으로 언제든 캔슬 가능 (Shape of Dreams 스타일)
/// </summary>
public class PlayerGroundAttack : MonoBehaviour
{
    #region Serialized Fields
    [Header("Combo Settings - 빠른 3단 콤보")]
    [SerializeField, Tooltip("콤보 애니메이션 트리거 이름들")]
    private List<string> comboTriggers = new List<string>
    {
        "Attack1", "Attack2", "Attack3"  // 3단 콤보
    };

    [SerializeField, Tooltip("각 공격 간 쿨다운 (초)")]
    private List<float> comboCooldowns = new List<float>
    {
        0.2f, 0.2f, 0.25f  // 빠른 공격 속도
    };

    [SerializeField, Tooltip("각 공격 시 전진 거리")]
    private List<float> forwardDistances = new List<float>
    {
        0.3f, 0.3f, 0.4f  // 거의 제자리 공격
    };

    [SerializeField, Tooltip("전진 이동 속도")]
    private float forwardDuration = 0.15f;  // 전진 속도도 빠르게

    [Header("Collision & Layers")]
    [SerializeField, Tooltip("적 레이어")]
    private LayerMask enemyLayer = -1;

    [SerializeField, Tooltip("벽 레이어")]
    private LayerMask wallLayer = -1;

    [Header("Cancel System")]
    [SerializeField, Tooltip("우클릭 캔슬 허용")]
    private bool allowRightClickCancel = true;
    #endregion

    #region Component References
    // 컴포넌트 참조
    private Animator anim;
    private PlayerController playerController;
    private PlayerLockOn lockOn;
    private Weapon weapon;
    private Rigidbody rigid;
    private PlayerCombatStateMachine combatStateMachine;  // 새로 추가
    #endregion

    #region Combo State Variables
    // 콤보 상태 관리
    private int comboIndex = 0;
    private float comboStartTime = 0f;
    private Coroutine comboCoroutine;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

    // 캔슬 상태
    private bool isCancelled = false;

    // 홀드/클릭 시스템
    private bool isHoldingAttack = false;  // 마우스를 누르고 있는지
    private bool waitingForNextClick = false;  // 다음 클릭 대기 중
    private float comboWindowTime = 0.5f;  // 콤보 입력 유효 시간
    #endregion

    #region Movement Variables
    // 전진 이동 제어
    private bool isMovingForward = false;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;
    #endregion

    #region Public Properties
    /// <summary>현재 지상 공격 중인지</summary>
    public bool isGroundAttacking { get; private set; } = false;

    /// <summary>지상 공격 준비 상태인지</summary>
    public bool isGroundAttackReady { get; private set; } = true;

    // 다른 스크립트와의 호환성을 위한 프로퍼티
    public bool isAttacking => isGroundAttacking;
    public bool isFireReady => isGroundAttackReady;
    #endregion

    #region Unity Lifecycle
    /// <summary>컴포넌트 초기화</summary>
    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>매 프레임 업데이트</summary>
    private void Update()
    {
        // 공격 중 홀드/캔슬 입력 체크
        if (isGroundAttacking)
        {
            CheckHoldInput();
            CheckCancelInput();
        }

        // 전진 중 충돌 체크
        if (isMovingForward)
        {
            CheckMovementCollision();
        }
    }
    #endregion

    #region Initialization
    /// <summary>컴포넌트 참조 설정</summary>
    private void InitializeComponents()
    {
        anim = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
        lockOn = GetComponent<PlayerLockOn>();
        weapon = GetComponentInChildren<Weapon>();
        rigid = GetComponent<Rigidbody>();
        combatStateMachine = GetComponent<PlayerCombatStateMachine>();

        if (anim == null)
            Debug.LogError("[PlayerGroundAttack] Animator를 찾을 수 없습니다!");

        if (combatStateMachine == null)
            Debug.LogWarning("[PlayerGroundAttack] PlayerCombatStateMachine이 없습니다. 추가해주세요!");
    }
    #endregion

    #region Public Interface
    /// <summary>외부에서 호출: 지상 공격 시작 (클릭 또는 홀드)</summary>
    public void StartGroundCombo()
    {
        // 이미 공격 중이면 콤보 진행 시도
        if (isGroundAttacking)
        {
            TryNextCombo();
            return;
        }

        if (!CanGroundAttack())
        {
            Debug.Log("[PlayerGroundAttack] 공격 불가능 상태");
            return;
        }

        // 상태 초기화
        isGroundAttacking = true;
        isGroundAttackReady = false;
        isCancelled = false;
        isHoldingAttack = true;  // 처음 클릭 시 홀드 시작
        comboIndex = 0;
        comboStartTime = Time.time;
        hitEnemies.Clear();

        // 준비 작업
        ResetAllTriggers();
        RotateToTarget();

        // 첫 공격 실행 + 홀드/클릭 대기
        comboCoroutine = StartCoroutine(ComboRoutine());

        Debug.Log("[PlayerGroundAttack] 공격 시작!");
    }

    /// <summary>다음 콤보 시도 (이미 공격 중일 때 클릭)</summary>
    private void TryNextCombo()
    {
        if (waitingForNextClick && comboIndex < comboTriggers.Count - 1)
        {
            waitingForNextClick = false;
            isHoldingAttack = true;  // 다시 홀드 상태로
            Debug.Log($"[PlayerGroundAttack] 다음 콤보 입력! ({comboIndex + 1} → {comboIndex + 2})");
        }
    }

    /// <summary>지상 공격 가능 여부 확인</summary>
    public bool CanGroundAttack()
    {
        if (playerController == null)
            return false;

        return !isGroundAttacking &&
               isGroundAttackReady &&
               !playerController.isDodging &&
               !playerController.isSkillCasting;
    }

    /// <summary>지상 공격 강제 취소</summary>
    public void CancelGroundAttack()
    {
        if (!isGroundAttacking) return;

        isCancelled = true;
        EndGroundCombo();

        Debug.Log("[PlayerGroundAttack] 공격 캔슬됨");
    }
    #endregion

    #region Combo System - Click/Hold Hybrid
    /// <summary>콤보 루틴 - 클릭/홀드 하이브리드</summary>
    private IEnumerator ComboRoutine()
    {
        while (comboIndex < comboTriggers.Count && isGroundAttacking)
        {
            // 캔슬되었으면 중단
            if (isCancelled)
            {
                Debug.Log($"[PlayerGroundAttack] {comboIndex + 1}단 공격 전 캔슬됨");
                break;
            }

            // 현재 공격 실행
            ExecuteAttack(comboIndex);

            // 공격 애니메이션 시간 대기
            float cooldown = comboIndex < comboCooldowns.Count ? comboCooldowns[comboIndex] : 0.2f;
            yield return new WaitForSeconds(cooldown);

            // 마지막 공격이면 종료
            if (comboIndex >= comboTriggers.Count - 1)
            {
                Debug.Log("[PlayerGroundAttack] 마지막 공격 완료");
                break;
            }

            // 다음 공격 조건 체크
            if (isHoldingAttack)
            {
                // 홀드 중이면 자동으로 다음 공격
                Debug.Log("[PlayerGroundAttack] 홀드 중 - 다음 공격 자동 실행");
                comboIndex++;
            }
            else
            {
                // 홀드 안 하면 다음 클릭 대기
                Debug.Log("[PlayerGroundAttack] 다음 클릭 대기 중...");
                waitingForNextClick = true;

                float waitTime = 0f;
                while (waitTime < comboWindowTime && waitingForNextClick)
                {
                    waitTime += Time.deltaTime;
                    yield return null;

                    // 대기 중 홀드하면 계속 진행
                    if (isHoldingAttack)
                    {
                        waitingForNextClick = false;
                        comboIndex++;
                        break;
                    }
                }

                // 시간 초과 시 콤보 종료
                if (waitingForNextClick)
                {
                    Debug.Log("[PlayerGroundAttack] 콤보 입력 시간 초과");
                    break;
                }
            }
        }

        // 콤보 종료
        EndGroundCombo();
    }

    /// <summary>특정 인덱스의 공격 실행</summary>
    private void ExecuteAttack(int attackIndex)
    {
        if (attackIndex >= comboTriggers.Count)
        {
            Debug.LogWarning($"[PlayerGroundAttack] 잘못된 공격 인덱스: {attackIndex}");
            return;
        }

        // 타겟 방향 회전
        RotateToTarget();

        // 애니메이션 트리거
        string triggerName = comboTriggers[attackIndex];
        anim.SetTrigger(triggerName);

        // 전진 이동
        MoveForward(attackIndex);

        // 히트박스 처리
        StartCoroutine(HitBoxWindow());

        Debug.Log($"[PlayerGroundAttack] {attackIndex + 1}단 공격 실행: {triggerName}");
    }
    #endregion

    #region Input Handling
    /// <summary>홀드 입력 체크</summary>
    private void CheckHoldInput()
    {
        // 마우스 왼쪽 버튼 상태 확인
        if (Input.GetMouseButton(0))
        {
            isHoldingAttack = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isHoldingAttack = false;
            Debug.Log("[PlayerGroundAttack] 홀드 해제");
        }
    }

    /// <summary>캔슬 입력 체크 (우클릭)</summary>
    private void CheckCancelInput()
    {
        if (!allowRightClickCancel)
            return;

        // 우클릭 입력 감지
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("[PlayerGroundAttack] 우클릭 캔슬 입력 감지");
            CancelGroundAttack();
        }
    }
    #endregion

    #region Hit Detection
    /// <summary>히트박스 타이밍 제어</summary>
    private IEnumerator HitBoxWindow()
    {
        // 애니메이션 시작 후 잠시 대기
        yield return new WaitForSeconds(0.05f);

        EnableHitBox();

        // 히트박스 활성 시간
        yield return new WaitForSeconds(0.2f);

        DisableHitBox();
    }

    /// <summary>히트박스 활성화 - 적 탐지 및 데미지</summary>
    private void EnableHitBox()
    {
        if (weapon == null)
        {
            Debug.LogWarning("[PlayerGroundAttack] Weapon이 없습니다!");
            return;
        }

        Vector3 attackPos = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapSphere(attackPos, 1.5f, enemyLayer);

        foreach (var hit in hits)
        {
            GameObject enemyObj = hit.gameObject;

            // 이미 맞은 적은 스킵
            if (hitEnemies.Contains(enemyObj))
                continue;

            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead)
            {
                // 데미지 계산 (마지막 공격은 2배)
                int damage = weapon.GetDamage();
                if (comboIndex == comboTriggers.Count - 1)
                {
                    damage *= 2;
                    Debug.Log("[PlayerGroundAttack] 마지막 공격! 데미지 2배!");
                }

                // 데미지 적용
                enemy.ApplySkillDamage(damage);
                hitEnemies.Add(enemyObj);

                Debug.Log($"[PlayerGroundAttack] {enemy.name}에게 {damage} 데미지!");
            }
        }
    }

    /// <summary>히트박스 비활성화</summary>
    private void DisableHitBox()
    {
        // 현재는 특별한 처리 없음
    }
    #endregion

    #region Movement & Rotation
    /// <summary>전진 이동 처리</summary>
    private void MoveForward(int attackIndex)
    {
        if (attackIndex >= forwardDistances.Count)
            return;

        float distance = forwardDistances[attackIndex];
        if (distance <= 0f)
            return;

        // 이동 방향 결정 (락온 우선)
        Vector3 dir = GetMoveDirection();

        // 타겟 위치 계산 (Y축 고정)
        Vector3 targetPos = transform.position + new Vector3(dir.x, 0, dir.z) * distance;
        targetPos.y = transform.position.y;

        // 벽 충돌 체크
        CheckWallCollision(ref targetPos, distance);

        // DoTween으로 부드러운 이동
        transform.DOMove(targetPos, forwardDuration).SetEase(Ease.OutCubic);
    }

    /// <summary>이동 방향 계산</summary>
    private Vector3 GetMoveDirection()
    {
        // 락온 타겟이 있으면 그쪽으로
        if (lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null)
        {
            return (lockOn.currentTarget.position - transform.position).normalized;
        }

        // 없으면 전방
        return transform.forward;
    }

    /// <summary>벽 충돌 체크 및 경로 보정</summary>
    private void CheckWallCollision(ref Vector3 targetPos, float distance)
    {
        Vector3 direction = (targetPos - transform.position).normalized;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, wallLayer))
        {
            // 벽 앞에서 정지하도록 보정
            targetPos = hit.point - direction * 0.1f;
        }
    }

    /// <summary>전진 중 적 충돌 체크</summary>
    private void CheckMovementCollision()
    {
        // 전방 0.5f 거리에서 적 충돌 체크
        if (Physics.Raycast(transform.position, transform.forward, 0.5f, enemyLayer))
        {
            // 적과 충돌하면 이동 정지 (애니메이션은 계속)
            isMovingForward = false;
        }
    }

    /// <summary>타겟 방향 회전</summary>
    private void RotateToTarget()
    {
        Vector3 dir = GetLookDirection();
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = rot;
        }
    }

    /// <summary>바라볼 방향 계산</summary>
    private Vector3 GetLookDirection()
    {
        // 락온 타겟이 있으면 그쪽으로
        if (lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null)
        {
            return (lockOn.currentTarget.position - transform.position).normalized;
        }

        // 없으면 마우스 방향
        if (playerController != null)
        {
            return playerController.GetMouseDirection();
        }

        return transform.forward;
    }
    #endregion

    #region Combo End
    /// <summary>지상 콤보 종료</summary>
    private void EndGroundCombo()
    {
        // 코루틴 중지
        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
            comboCoroutine = null;
        }

        // 상태 초기화
        isGroundAttacking = false;
        comboIndex = 0;
        hitEnemies.Clear();
        isCancelled = false;
        isHoldingAttack = false;  // 홀드 상태 초기화
        waitingForNextClick = false;  // 대기 상태 초기화

        // CombatStateMachine에 알림
        if (combatStateMachine != null)
        {
            combatStateMachine.ReturnToIdle();
        }

        // 쿨다운 후 다시 공격 가능
        StartCoroutine(AttackCooldown());

        Debug.Log("[PlayerGroundAttack] 콤보 종료");
    }

    /// <summary>공격 쿨다운</summary>
    private IEnumerator AttackCooldown()
    {
        isGroundAttackReady = false;
        yield return new WaitForSeconds(0.1f);  // 아주 짧은 쿨다운
        isGroundAttackReady = true;
    }
    #endregion

    #region Utility
    /// <summary>모든 애니메이터 트리거 리셋</summary>
    private void ResetAllTriggers()
    {
        if (anim == null)
            return;

        foreach (string trigger in comboTriggers)
        {
            anim.ResetTrigger(trigger);
        }

        anim.ResetTrigger("Dodge");
        anim.ResetTrigger("Skill_Q");
        anim.ResetTrigger("Skill_E");
    }
    #endregion

    #region Debug Visualization
    /// <summary>기즈모로 공격 범위 표시</summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        // 공격 범위 표시
        Gizmos.color = Color.red;
        Vector3 attackPos = transform.position + transform.forward * 1f;
        Gizmos.DrawWireSphere(attackPos, 1.5f);

        // 전진 방향 표시
        Gizmos.color = Color.blue;
        if (comboIndex < forwardDistances.Count)
        {
            float distance = forwardDistances[comboIndex];
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * distance);
        }
    }
    #endregion
}
