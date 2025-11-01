/**
 * Enemy 공격 상태
 * - 플레이어가 공격 사거리에 들어왔을 때의 상태
 * - IEnemyAttack 인터페이스를 통해 타입별 공격 실행
 * - 공격 완료 후 쿨타임을 거쳐 다음 상태로 전환
 */
public class EnemyAttackState : EnemyBaseState
{
    /// <summary>현재 사용 중인 공격 컴포넌트</summary>
    private IEnemyAttack attackComponent;

    /// <summary>공격 시작 시간 (쿨타임 계산용)</summary>
    private float attackStartTime;

    /// <summary>공격 완료 플래그</summary>
    private bool attackCompleted;

    public EnemyAttackState(EnemyStateMachine stateMachine) : base(stateMachine)
    {
    }

    /// <summary>
    /// 공격 상태 진입
    /// - 이동 중지
    /// - 공격 컴포넌트 찾기 및 공격 시작
    /// </summary>
    public override void Enter()
    {
        base.Enter();

        // 이동 중지
        stateMachine.MovementSpeedModifier = 0f;

        // NavMesh 이동 중지
        var nav = stateMachine.Enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (nav != null && nav.enabled)
        {
            nav.ResetPath();
            nav.velocity = UnityEngine.Vector3.zero;
        }

        // 공격 애니메이션 시작
        if (stateMachine.Enemy.AnimationData != null)
        {
            StartAnimation(stateMachine.Enemy.AnimationData.AttackParameterHash);
            StartAnimation(stateMachine.Enemy.AnimationData.BaseAttackParameterHash);
        }

        // 공격 컴포넌트 찾기 및 공격 시작
        InitializeAttack();
    }

    /// <summary>
    /// 공격 상태 종료
    /// - 공격 애니메이션 중지
    /// - 공격 컴포넌트 정리
    /// </summary>
    public override void Exit()
    {
        base.Exit();

        // 공격 애니메이션 중지
        if (stateMachine.Enemy.AnimationData != null)
        {
            StopAnimation(stateMachine.Enemy.AnimationData.AttackParameterHash);
            StopAnimation(stateMachine.Enemy.AnimationData.BaseAttackParameterHash);
        }

        // 공격 중단 (혹시 아직 진행 중이라면)
        if (attackComponent != null && attackComponent.IsAttacking())
        {
            attackComponent.StopAttack();
        }
    }

    /// <summary>
    /// 공격 상태 업데이트
    /// - 공격 진행 상황 체크
    /// - 공격 완료 시 다음 상태로 전환
    /// </summary>
    public override void Update()
    {
        // 기본 이동은 하지 않음 (공격 중이므로)
        // base.Update() 호출 안함

        // 공격 진행 상황 체크
        if (attackComponent != null)
        {
            // 공격이 완료되었는지 확인
            if (!attackComponent.IsAttacking() && !attackCompleted)
            {
                attackCompleted = true;
                UnityEngine.Debug.Log($"[EnemyAttack] {stateMachine.Enemy.name} 공격 완료");
            }

            // 공격 완료 후 쿨타임 체크
            if (attackCompleted)
            {
                float timeSinceAttack = UnityEngine.Time.time - attackStartTime;
                float cooldownTime = attackComponent.GetCooldownTime();

                if (timeSinceAttack >= cooldownTime)
                {
                    // 쿨타임 완료 - 다음 상태 결정
                    DecideNextState();
                }
            }
        }
        else
        {
            // 공격 컴포넌트가 없으면 즉시 다음 상태로
            UnityEngine.Debug.LogWarning($"[EnemyAttack] {stateMachine.Enemy.name}에 공격 컴포넌트가 없음");
            TransitionToNextState();
        }

        // 공격 중에도 플레이어 쪽으로 회전 유지
        RotateTowardsTarget();
    }

    /// <summary>
    /// 공격 초기화 및 시작
    /// </summary>
    private void InitializeAttack()
    {
        // 적의 타입에 따라 해당하는 공격 컴포넌트 찾기
        attackComponent = stateMachine.Enemy.GetComponent<IEnemyAttack>();

        if (attackComponent != null)
        {
            // 공격 시작
            attackComponent.StartAttack();
            attackStartTime = UnityEngine.Time.time;
            attackCompleted = false;

            UnityEngine.Debug.Log($"[EnemyAttack] {stateMachine.Enemy.name} 공격 시작 ({attackComponent.GetType().Name})");
        }
        else
        {
            UnityEngine.Debug.LogError($"[EnemyAttack] {stateMachine.Enemy.name}에서 IEnemyAttack 구현체를 찾을 수 없음");
        }
    }

    /// <summary>
    /// 공격 완료 후 다음 상태 결정
    /// </summary>
    private void DecideNextState()
    {
        // 타겟이 유효하지 않으면 대기 상태
        if (!stateMachine.IsTargetValid())
        {
            TransitionToNextState();
            return;
        }

        // 플레이어가 아직 공격 사거리에 있으면 연속 공격
        if (stateMachine.IsPlayerInAttackRange())
        {
            UnityEngine.Debug.Log($"[EnemyAttack] {stateMachine.Enemy.name} 연속 공격 시작!");

            // 현재 상태를 유지하면서 새로운 공격 시작
            RestartAttackInCurrentState();
            return;
        }

        // 플레이어가 범위를 벗어났으면 다른 상태로 전환
        TransitionToNextState();
    }

    /// <summary>
    /// 현재 Attack 상태에서 새로운 공격 시작 (상태 전환 없이)
    /// </summary>
    private void RestartAttackInCurrentState()
    {
        // 기존 공격 정리
        if (attackComponent != null && attackComponent.IsAttacking())
        {
            attackComponent.StopAttack();
        }

        // 새로운 공격 시작
        if (attackComponent != null)
        {
            attackComponent.StartAttack();
            attackStartTime = UnityEngine.Time.time;
            attackCompleted = false;

            UnityEngine.Debug.Log($"[EnemyAttack] {stateMachine.Enemy.name} 연속 공격 재시작");
        }
    }

    /// <summary>
    /// 다음 상태로 전환 (공격 범위를 벗어났을 때)
    /// </summary>
    private void TransitionToNextState()
    {
        // 타겟이 유효하지 않으면 대기 상태
        if (!stateMachine.IsTargetValid())
        {
            stateMachine.ChangeState(stateMachine.IdleState);
            return;
        }

        // 플레이어가 탐지 범위에 있으면 추적
        if (stateMachine.IsPlayerInDetectRange())
        {
            stateMachine.ChangeState(stateMachine.ChasingState);
            return;
        }

        // 그 외의 경우 대기 상태
        stateMachine.ChangeState(stateMachine.IdleState);
    }
}
