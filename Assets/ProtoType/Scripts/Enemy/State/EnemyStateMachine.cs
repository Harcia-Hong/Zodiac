using UnityEngine;

/**
 * Enemy 전용 상태머신
 * Enemy의 모든 상태와 상태 전환을 관리
 * Enemy 컴포넌트에서 이 클래스를 통해 AI 로직 실행
 */
public class EnemyStateMachine : StateMachine
{
    /// <summary>이 상태머신이 관리하는 Enemy</summary>
    public Enemy Enemy { get; }

    /// <summary>플레이어 타겟 (추적 대상)</summary>
    public Transform Target { get; private set; }

    /// <summary>플레이어 Health 컴포넌트 (생존 여부 체크용)</summary>
    public PlayerHealth TargetHealth { get; private set; }

    // 이동 관련 속성들
    public float MovementSpeed { get; private set; }
    public float RotationDamping { get; private set; }
    public float MovementSpeedModifier { get; set; } = 1f;

    // 각 상태 인스턴스들 (한 번만 생성하여 재사용)
    public EnemyIdleState IdleState { get; }
    public EnemyChasingState ChasingState { get; }
    public EnemyAttackState AttackState { get; }

    /// <summary>
    /// 생성자 - Enemy 컴포넌트에서 호출
    /// </summary>
    /// <param name="enemy">이 상태머신이 관리할 Enemy</param>
    public EnemyStateMachine(Enemy enemy)
    {
        Enemy = enemy;

        // Enemy 데이터에서 설정값 가져오기
        if (Enemy.Data != null)
        {
            MovementSpeed = Enemy.Data.baseSpeed;
            RotationDamping = Enemy.Data.baseRotationDamping;
        }
        else
        {
            // 기본값 (데이터가 없는 경우)
            MovementSpeed = 3.5f;
            RotationDamping = 10f;
            Debug.LogWarning($"[EnemyStateMachine] {Enemy.name}에 EnemyData가 설정되지 않음. 기본값 사용.");
        }

        // 플레이어 타겟 찾기
        FindPlayerTarget();

        // 각 상태 인스턴스 생성
        IdleState = new EnemyIdleState(this);
        ChasingState = new EnemyChasingState(this);
        AttackState = new EnemyAttackState(this);
    }

    /// <summary>
    /// 플레이어 타겟 찾기 및 설정
    /// </summary>
    private void FindPlayerTarget()
    {
        // "Player" 태그로 플레이어 찾기
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");

        if (playerGO != null)
        {
            Target = playerGO.transform;
            TargetHealth = playerGO.GetComponent<PlayerHealth>();

            if (TargetHealth == null)
            {
                Debug.LogWarning($"[EnemyStateMachine] Player에 Health 컴포넌트가 없음: {playerGO.name}");
            }
        }
        else
        {
            Debug.LogError($"[EnemyStateMachine] Player를 찾을 수 없음. Player 태그 확인 필요.");
        }
    }

    /// <summary>
    /// 타겟이 유효한지 확인
    /// 플레이어가 살아있고, 너무 멀지 않은지 체크
    /// </summary>
    /// <returns>타겟이 유효하면 true</returns>
    public bool IsTargetValid()
    {
        // 타겟이 없으면 무효
        if (Target == null) return false;

        // 플레이어가 죽었으면 무효
        if (TargetHealth != null && TargetHealth.isDead) return false;

        // 너무 멀리 있으면 무효 (추적 포기 범위)
        if (Enemy.Data != null)
        {
            float distance = Vector3.Distance(Enemy.transform.position, Target.position);
            return distance <= Enemy.Data.chasingRange;
        }

        return true;
    }

    /// <summary>
    /// 플레이어가 탐지 범위 안에 있는지 확인
    /// </summary>
    /// <returns>탐지 범위 안에 있으면 true</returns>
    public bool IsPlayerInDetectRange()
    {
        if (!IsTargetValid()) return false;

        if (Enemy.Data != null)
        {
            float distance = Vector3.Distance(Enemy.transform.position, Target.position);
            return distance <= Enemy.Data.detectRadius;
        }

        return false;
    }

    /// <summary>
    /// 플레이어가 공격 범위 안에 있는지 확인
    /// </summary>
    /// <returns>공격 범위 안에 있으면 true</returns>
    public bool IsPlayerInAttackRange()
    {
        if (!IsTargetValid()) return false;

        // 현재 공격 컴포넌트의 사거리 사용
        IEnemyAttack attackComponent = Enemy.GetComponent<IEnemyAttack>();
        float attackRange = attackComponent?.GetAttackRange() ?? 2f;

        float distance = Vector3.Distance(Enemy.transform.position, Target.position);
        return distance <= attackRange;
    }
}
