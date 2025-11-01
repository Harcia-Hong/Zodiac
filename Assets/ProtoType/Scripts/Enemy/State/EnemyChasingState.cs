/**
 * Enemy 추적 상태
 * - 플레이어를 발견했을 때의 상태
 * - NavMesh를 이용해 플레이어 추적
 * - 공격 사거리 진입 시 Attack 상태로 전환
 * - 플레이어를 놓치면 Idle 상태로 복귀
 */
public class EnemyChasingState : EnemyBaseState
{
    /// <summary>타겟 위치 업데이트 간격 (성능 최적화)</summary>
    private const float TARGET_UPDATE_INTERVAL = 0.2f;
    private float lastTargetUpdateTime;

    public EnemyChasingState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    /// <summary>
    /// 추적 상태 진입
    /// - 이동 속도 설정
    /// - 걷기 애니메이션 시작
    /// </summary>
    public override void Enter()
    {
        base.Enter();

        // 이동 속도 설정 (보통 기본 속도보다 약간 느림)
        stateMachine.MovementSpeedModifier = 0.8f;

        // 걷기 애니메이션 시작
        if (stateMachine.Enemy.AnimationData != null)
        {
            StartAnimation(stateMachine.Enemy.AnimationData.GroundParameterHash);
            StartAnimation(stateMachine.Enemy.AnimationData.WalkParameterHash);
        }

        // 첫 타겟 업데이트
        lastTargetUpdateTime = 0f;
    }

    /// <summary>
    /// 추적 상태 종료
    /// - 걷기 애니메이션 중지
    /// </summary>
    public override void Exit()
    {
        base.Exit();

        // 걷기 애니메이션 중지
        if (stateMachine.Enemy.AnimationData != null)
        {
            StopAnimation(stateMachine.Enemy.AnimationData.GroundParameterHash);
            StopAnimation(stateMachine.Enemy.AnimationData.WalkParameterHash);
        }

        // NavMesh 경로 초기화
        var nav = stateMachine.Enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null && nav.enabled)
        {
            nav.ResetPath();
        }
    }

    /// <summary>
    /// 추적 상태 업데이트
    /// - 플레이어 추적
    /// - 공격 사거리 체크
    /// - 타겟 잃어버림 체크
    /// </summary>
    public override void Update()
    {
        // 타겟이 유효하지 않으면 대기 상태로 복귀
        if (!stateMachine.IsTargetValid())
        {
            stateMachine.ChangeState(stateMachine.IdleState);
            return;
        }

        // 플레이어가 공격 사거리에 들어왔는지 확인
        if (stateMachine.IsPlayerInAttackRange())
        {
            stateMachine.ChangeState(stateMachine.AttackState);
            return;
        }

        // 플레이어가 탐지 범위를 벗어났는지 확인 (추적 포기)
        if (!stateMachine.IsPlayerInDetectRange())
        {
            stateMachine.ChangeState(stateMachine.IdleState);
            return;
        }

        // 성능 최적화: 일정 간격으로만 타겟 위치 업데이트
        if (UnityEngine.Time.time - lastTargetUpdateTime >= TARGET_UPDATE_INTERVAL)
        {
            UpdateTargetTracking();
            lastTargetUpdateTime = UnityEngine.Time.time;
        }

        // 플레이어를 향해 회전
        RotateTowardsTarget();
    }

    /// <summary>
    /// 타겟 추적 업데이트
    /// NavMesh 경로를 플레이어 위치로 설정
    /// </summary>
    private void UpdateTargetTracking()
    {
        if (!stateMachine.IsTargetValid()) return;

        var nav = stateMachine.Enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null && nav.enabled && nav.isOnNavMesh)
        {
            // 이동 속도 적용
            float finalSpeed = stateMachine.MovementSpeed * stateMachine.MovementSpeedModifier;
            nav.speed = finalSpeed;

            // 플레이어 위치로 경로 설정
            nav.SetDestination(stateMachine.Target.position);

            // 디버그 정보
            UnityEngine.Debug.Log($"[EnemyChasing] {stateMachine.Enemy.name} → Player 추적 중. 거리: {UnityEngine.Vector3.Distance(stateMachine.Enemy.transform.position, stateMachine.Target.position):F1}m");
        }
    }
}
