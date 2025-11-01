using UnityEngine;

/// <summary>
/// 시간 보상 데이터 (ScriptableObject)
/// 몬스터 등급별 시간 회복량 정의
/// </summary>
[CreateAssetMenu(fileName = "TimeRewardData", menuName = "Time System/Time Reward Data", order = 0)]
public class TimeRewardData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField, Tooltip("보상 데이터 이름")]
    private string rewardName = "기본 시간 보상";

    [SerializeField, TextArea(2, 4), Tooltip("보상 설명")]
    private string description = "적 처치 시 지급되는 시간 보상";

    [Header("몬스터 등급별 보상")]
    [SerializeField, Tooltip("일반 몬스터 보상 시간")]
    private float normalReward = 5f;

    [SerializeField, Tooltip("엘리트 몬스터 보상 시간")]
    private float eliteReward = 15f;

    [SerializeField, Tooltip("보스 몬스터 보상 시간")]
    private float bossReward = 60f;

    [Header("추가 보상 설정")]
    [SerializeField, Tooltip("콤보 보너스 활성화")]
    private bool enableComboBonus = false;

    [SerializeField, Tooltip("콤보 당 추가 보상 비율 (%)")]
    private float comboBonusPercent = 5f;

    [SerializeField, Tooltip("최대 콤보 보너스 (%)")]
    private float maxComboBonusPercent = 50f;

    // 프로퍼티
    public string RewardName => rewardName;
    public string Description => description;
    public float NormalReward => normalReward;
    public float EliteReward => eliteReward;
    public float BossReward => bossReward;
    public bool EnableComboBonus => enableComboBonus;
    public float ComboBonusPercent => comboBonusPercent;
    public float MaxComboBonusPercent => maxComboBonusPercent;

    /// <summary>
    /// 몬스터 등급에 따른 시간 보상 반환
    /// </summary>
    /// <param name="rank">몬스터 등급</param>
    /// <returns>보상 시간</returns>
    public float GetRewardByRank(EnemyRank rank)
    {
        switch (rank)
        {
            case EnemyRank.Normal:
                return normalReward;

            case EnemyRank.Elite:
                return eliteReward;

            case EnemyRank.Boss:
                return bossReward;

            default:
                Debug.LogWarning($"[TimeRewardData] 알 수 없는 몬스터 등급: {rank}. 기본값 반환.");
                return normalReward;
        }
    }

    /// <summary>
    /// 콤보 보너스 적용된 시간 보상 계산
    /// </summary>
    /// <param name="rank">몬스터 등급</param>
    /// <param name="currentCombo">현재 콤보 수</param>
    /// <returns>보너스 적용된 보상 시간</returns>
    public float GetRewardWithCombo(EnemyRank rank, int currentCombo)
    {
        float baseReward = GetRewardByRank(rank);

        if (!enableComboBonus || currentCombo <= 1)
            return baseReward;

        // 콤보 보너스 계산 (최대치 제한)
        float bonusPercent = Mathf.Min((currentCombo - 1) * comboBonusPercent, maxComboBonusPercent);
        float bonusMultiplier = 1f + (bonusPercent / 100f);

        float finalReward = baseReward * bonusMultiplier;

        return finalReward;
    }

    /// <summary>
    /// 에디터용: 보상 정보 출력
    /// </summary>
    [ContextMenu("보상 정보 출력")]
    private void PrintRewardInfo()
    {
        Debug.Log("=== 시간 보상 정보 ===");
        Debug.Log($"이름: {rewardName}");
        Debug.Log($"일반: {normalReward}초");
        Debug.Log($"엘리트: {eliteReward}초");
        Debug.Log($"보스: {bossReward}초");

        if (enableComboBonus)
        {
            Debug.Log($"콤보 보너스: {comboBonusPercent}% (최대 {maxComboBonusPercent}%)");
            Debug.Log($"10콤보 일반 몬스터: {GetRewardWithCombo(EnemyRank.Normal, 10):F1}초");
        }
    }
}
