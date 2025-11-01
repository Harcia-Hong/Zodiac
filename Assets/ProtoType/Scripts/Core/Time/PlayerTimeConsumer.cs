using UnityEngine;

/// <summary>
/// 플레이어 시간 소모 시스템
/// 이동, 공격, 스킬 등 모든 행동에 시간 소모 적용
/// </summary>
public class PlayerTimeConsumer : MonoBehaviour
{
    [Header("시간 소모 설정")]
    [SerializeField, Tooltip("이동 1m당 소모되는 시간")]
    private float timePerMeter = 0.5f;

    [SerializeField, Tooltip("일반 공격 시 소모 시간")]
    private float attackTimeCost = 2f;

    [SerializeField, Tooltip("회피 시 소모 시간")]
    private float dodgeTimeCost = 3f;

    [SerializeField, Tooltip("스킬 사용 시 기본 소모 시간")]
    private float skillTimeCost = 5f;

    [Header("거리 누적 설정")]
    [SerializeField, Tooltip("시간 차감을 위한 최소 이동 거리 (m)")]
    private float distanceThreshold = 1f;

    // 컴포넌트 참조
    private TimeResource timeResource;
    private PlayerController playerController;

    // 이동 거리 추적
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    /// <summary>초기화</summary>
    private void Awake()
    {
        timeResource = GetComponent<TimeResource>();
        playerController = GetComponent<PlayerController>();

        if (timeResource == null)
        {
            Debug.LogError("[PlayerTimeConsumer] TimeResource 컴포넌트를 찾을 수 없습니다!");
        }
    }

    /// <summary>시작 시 위치 저장</summary>
    private void Start()
    {
        lastPosition = transform.position;
    }

    /// <summary>매 프레임 이동 거리 계산</summary>
    private void Update()
    {
        TrackMovementDistance();
    }

    // =============================================================================
    // 이동 시간 소모
    // =============================================================================

    /// <summary>
    /// 이동 거리 추적 및 시간 소모
    /// 일정 거리 이동 시마다 시간 차감
    /// </summary>
    private void TrackMovementDistance()
    {
        if (timeResource == null || timeResource.IsDead) return;

        // 현재 위치와 이전 위치의 XZ 평면 거리 계산 (Y축 제외)
        Vector3 currentPos = transform.position;
        Vector3 lastPosXZ = new Vector3(lastPosition.x, 0f, lastPosition.z);
        Vector3 currentPosXZ = new Vector3(currentPos.x, 0f, currentPos.z);

        float distanceMoved = Vector3.Distance(lastPosXZ, currentPosXZ);

        // 거리 누적
        if (distanceMoved > 0.01f) // 아주 작은 떨림 무시
        {
            accumulatedDistance += distanceMoved;

            // 일정 거리 이상 누적되면 시간 차감
            if (accumulatedDistance >= distanceThreshold)
            {
                float timeToConsume = accumulatedDistance * timePerMeter;
                timeResource.ConsumeTime(timeToConsume);

                // 누적 거리 초기화
                accumulatedDistance = 0f;
            }
        }

        // 위치 업데이트
        lastPosition = currentPos;
    }

    // =============================================================================
    // 행동별 시간 소모 (외부에서 호출)
    // =============================================================================

    /// <summary>
    /// 공격 시 시간 소모
    /// PlayerGroundAttack 등에서 호출
    /// </summary>
    public void ConsumeTimeForAttack()
    {
        if (timeResource != null && !timeResource.IsDead)
        {
            timeResource.ConsumeTime(attackTimeCost);
            Debug.Log($"[PlayerTimeConsumer] 공격으로 {attackTimeCost}초 소모");
        }
    }

    /// <summary>
    /// 회피 시 시간 소모
    /// PlayerController의 회피 로직에서 호출
    /// </summary>
    public void ConsumeTimeForDodge()
    {
        if (timeResource != null && !timeResource.IsDead)
        {
            timeResource.ConsumeTime(dodgeTimeCost);
            Debug.Log($"[PlayerTimeConsumer] 회피로 {dodgeTimeCost}초 소모");
        }
    }

    /// <summary>
    /// 스킬 사용 시 시간 소모
    /// PlayerSkill 등에서 호출
    /// </summary>
    /// <param name="customCost">커스텀 시간 소모량 (0이면 기본값 사용)</param>
    public void ConsumeTimeForSkill(float customCost = 0f)
    {
        if (timeResource != null && !timeResource.IsDead)
        {
            float cost = customCost > 0f ? customCost : skillTimeCost;
            timeResource.ConsumeTime(cost);
            Debug.Log($"[PlayerTimeConsumer] 스킬로 {cost}초 소모");
        }
    }

    /// <summary>
    /// 범용 시간 소모 (다른 행동들을 위해)
    /// </summary>
    /// <param name="amount">소모할 시간</param>
    public void ConsumeTime(float amount)
    {
        if (timeResource != null && !timeResource.IsDead && amount > 0f)
        {
            timeResource.ConsumeTime(amount);
        }
    }

    // =============================================================================
    // 설정 변경 (런타임 중)
    // =============================================================================

    /// <summary>이동 시 시간 소모율 변경 (버프/디버프 등)</summary>
    public void SetMovementTimeCost(float costPerMeter)
    {
        timePerMeter = Mathf.Max(0f, costPerMeter);
    }

    /// <summary>공격 시간 소모량 변경</summary>
    public void SetAttackTimeCost(float cost)
    {
        attackTimeCost = Mathf.Max(0f, cost);
    }

    // =============================================================================
    // 디버그
    // =============================================================================

    /// <summary>현재 누적 거리 확인</summary>
    private void OnGUI()
    {
        if (Application.isPlaying && timeResource != null)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"누적 거리: {accumulatedDistance:F2}m");
            GUI.Label(new Rect(10, 120, 300, 20), $"이동 소모율: {timePerMeter}초/m");
        }
    }

    /// <summary>디버그용: 공격 시간 소모 테스트</summary>
    [ContextMenu("테스트: 공격 시간 소모")]
    private void DebugAttack()
    {
        ConsumeTimeForAttack();
    }

    /// <summary>디버그용: 회피 시간 소모 테스트</summary>
    [ContextMenu("테스트: 회피 시간 소모")]
    private void DebugDodge()
    {
        ConsumeTimeForDodge();
    }

    /// <summary>디버그용: 스킬 시간 소모 테스트</summary>
    [ContextMenu("테스트: 스킬 시간 소모")]
    private void DebugSkill()
    {
        ConsumeTimeForSkill();
    }
}
