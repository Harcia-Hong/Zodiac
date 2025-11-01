using System.Collections;
using UnityEngine;

/**
 * Enemy 기본 근접 공격 (Standard Type)
 * IEnemyAttack 인터페이스를 구현하여 FSM과 연동
 * 기존 로직은 그대로 유지하면서 인터페이스만 추가
 */
public class Enemy_A_Attack : MonoBehaviour, IEnemyAttack
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDelay = 1.5f;
    public int attackDamage = 10;

    [Header("Movement")]
    public float forwardMoveAmoumt = 1.2f;
    public float forwardMoveSpeed = 5f;
    public Transform moveRoot;

    [Header("Weapon")]
    public EnemyWeapon weapon;

    // 기존 변수들
    Enemy enemy;
    Transform player;
    Animator anim;
    bool isAttacking = false;
    bool hasHit = false; // 중복 공격 방지

    #region 기존 코드 (FSM 연동으로 수정)

    private void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
        anim = GetComponentInChildren<Animator>();
        enemy = GetComponent<Enemy>();

        if (moveRoot == null) moveRoot = transform;
    }

    void Update()
    {
        // FSM이 공격을 제어하므로 자동 공격 로직 비활성화
        // 기존 Update 로직은 주석 처리하여 호환성 유지

        /*if (player == null || isAttacking || (enemy != null && (enemy.isHit || enemy.isGroggy))) return;

        var hitReactionSystem = enemy.GetComponent<EnemyHitReactionSystem>();
        if (hitReactionSystem != null && hitReactionSystem.IsInGroggy()) return;

        // XZ 평면 거리로만 판단 (쿼터뷰 전용)
        Vector3 e = transform.position; e.y = 0f;
        Vector3 p = player.position; p.y = 0f;
        float distance = Vector3.Distance(e, p); // y 제외한 평면 거리

        if (distance <= attackRange)
            StartCoroutine(AttackRoutine());*/
    }

    /// <summary>
    /// 공격 루틴 - FSM 전용으로 수정
    /// Trigger 대신 FSM의 Bool 파라미터 사용
    /// </summary>
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        hasHit = false;

        // 공격 시 플레이어 방향으로 회전시키기
        LookAtPlayer();
        Debug.Log($"[Enemy_A_Attack] 공격 시작 - FSM이 애니메이션 제어");

        yield return new WaitForSeconds(attackDelay);
        isAttacking = false;

        Debug.Log($"[Enemy_A_Attack] 공격 완료");
    }

    /// <summary>
    /// 플레이어 방향으로 회전
    /// </summary>
    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dirToPlayer = (player.position - transform.position);
        dirToPlayer.y = 0f; // XZ 만 쓰기 때문

        if (dirToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer.normalized);
            transform.rotation = targetRotation; // 즉시 회전
        }
    }

    public void HitPlayer()
    {
        if (hasHit || player == null) return;

        // XZ 평면 거리로만 타격 판정
        Vector3 e = transform.position; e.y = 0f;
        Vector3 p = player.position; p.y = 0f;
        float distance = Vector3.Distance(e, p);

        if (distance <= attackRange)
        {
            hasHit = true;

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"[Enemy_A_Attack] 플레이어에게 {attackDamage} 데미지");
            }
        }
    }

    public void MoveForward()
    {
        if (moveRoot == null) return;

        Vector3 forward = transform.forward;
        forward.y = 0f; // XZ 평면에서만 이동

        moveRoot.position += forward * forwardMoveAmoumt;
    }

    #endregion

    #region IEnemyAttack 구현

    /// <summary>
    /// 현재 공격 중인지 확인
    /// FSM에서 상태 전환 조건 체크에 사용
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// 공격 시작 (FSM에서 호출)
    /// 기존의 공격 로직을 FSM에서 호출할 수 있도록 래핑
    /// </summary>
    public void StartAttack()
    {
        if (!isAttacking && gameObject.activeInHierarchy)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    /// <summary>
    /// 공격 중단/취소
    /// 상태 전환이나 피격 시 호출
    /// </summary>
    public void StopAttack()
    {
        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
            hasHit = false;
            Debug.Log($"[Enemy_A_Attack] 공격 중단");
        }
    }

    /// <summary>
    /// 이 공격의 사거리 반환
    /// FSM에서 공격 가능 거리 판단에 사용
    /// </summary>
    public float GetAttackRange()
    {
        return attackRange;
    }

    /// <summary>
    /// 공격 쿨타임 반환
    /// FSM에서 다음 공격까지의 대기 시간 계산에 사용
    /// </summary>
    public float GetCooldownTime()
    {
        return attackDelay;
    }

    #endregion
}
