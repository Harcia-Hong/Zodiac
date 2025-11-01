using System.Collections.Generic;
using UnityEngine;
using static RewardData;

/// <summary>
/// 확률 기반으로 보상을 선택하는 시스템
/// 65%, 20%, 12%, 2.5%, 0.5% 확률로 등급별 보상을 중복 없이 3개 선택
/// </summary>
public class WeightedRandomSelector : MonoBehaviour
{
    [Header("보상 선택 설정")]
    [SerializeField, Tooltip("선택할 보상 개수")]
    int rewardCount = 3;

    [SerializeField, Tooltip("보상 데이터 베이스")]
    RewardDatabase rewardDatabase;

    [Header("확률 테스트 (디버깅 용)")]
    [SerializeField, Tooltip("테스트 실행 횟수")]
    int testIterations = 1000;

    [SerializeField, Tooltip("등급별 선택 통계")]
    RarityTestResults testResults = new RarityTestResults();

    /// <summary>
    /// 중복 없는 보상 3개 선택 - 메인 인터페이스
    /// RewardUIManager에서 호출하여 보상 카드 3개를 생성할 때 사용
    /// </summary>
    public RewardData[] SelectRewards()
    {
        if(rewardDatabase == null)
        {
            Debug.LogError("[WeightedRandomSelector] RewardDatabase가 설정되지 않았습니다!");
            return new RewardData[0];
        }

        var validRewards = rewardDatabase.GetValidRewards();
        if(validRewards.Length < rewardCount)
        {
            Debug.LogError($"[WeightedRandomSelector] 유효한 보상이 {rewardCount}개보다 적습니다! ({validRewards.Length}개)");
            return validRewards;
        }

        List<RewardData> selectedRewards = new List<RewardData>();
        List<RewardData> availableRewards = new List<RewardData>(validRewards);

        Debug.Log($"[WeightedRandomSelector] {rewardCount}개 보상 선택 시작 (전체 {validRewards.Length}개 중)");

        // 지정된 개수만큼 보상 선택
        for (int i = 0; i < rewardCount; i++)
        {
            if (availableRewards.Count == 0)
            {
                Debug.LogWarning("[WeightedRandomSelector] 선택 가능한 보상이 부족합니다!");
                break;
            }

            // 가중치 기반 랜덤 선택
            RewardData selectedReward = SelectSingleRewardByWeight(availableRewards);

            if (selectedReward != null)
            {
                selectedRewards.Add(selectedReward);
                availableRewards.Remove(selectedReward); // 중복 방지

                Debug.Log($"[WeightedRandomSelector] {i + 1}번째 보상 선택: {selectedReward.ToString()}");
            }
        }

        Debug.Log($"[WeightedRandomSelector] 선택 완료: 총 {selectedRewards.Count}개");
        return selectedRewards.ToArray();
    }

    /// <summary>
    /// 가중치 기반으로 단일 보상 선택
    /// 각 보상의 등급에 따른 확률로 선택
    /// </summary>
    /// <param name="availableRewards">선택 가능한 보상 목록</param>
    /// <returns>선택된 보상</returns>
    private RewardData SelectSingleRewardByWeight(List<RewardData> availableRewards)
    {
        if (availableRewards == null || availableRewards.Count == 0)
            return null;

        // 1단계: 전체 가중치 합계 계산
        float totalWeight = 0f;
        foreach (RewardData reward in availableRewards)
        {
            totalWeight += reward.rarity.GetWeight();
        }

        // 2단계: 0 ~ 총 가중치 범위에서 랜덤 값 선택
        float randomValue = Random.Range(0f, totalWeight);

        // 3단계: 누적 가중치로 해당하는 보상 찾기
        float currentWeight = 0f;
        foreach (RewardData reward in availableRewards)
        {
            currentWeight += reward.rarity.GetWeight();

            if (randomValue <= currentWeight)
            {
                return reward;
            }
        }

        // 폴백: 마지막 보상 반환 (부동소수점 오차 대비)
        return availableRewards[availableRewards.Count - 1];
    }

    /// <summary>
    /// 특정 등급의 보상만 중복 없이 선택
    /// 특별한 상황에서 사용 (예: 이벤트, 보장된 등급 등)
    /// </summary>
    /// <param name="targetRarity">선택할 등급</param>
    /// <param name="count">선택할 개수</param>
    /// <returns>선택된 보상 배열</returns>
    public RewardData[] SelectRewardsByRarity(RewardRarity targetRarity, int count = 1)
    {
        if (rewardDatabase == null)
        {
            Debug.LogError("[WeightedRandomSelector] RewardDatabase가 설정되지 않았습니다!");
            return new RewardData[0];
        }

        var rarityRewards = rewardDatabase.GetRewardsByRarity(targetRarity);
        if (rarityRewards.Length == 0)
        {
            Debug.LogWarning($"[WeightedRandomSelector] {targetRarity} 등급 보상이 없습니다!");
            return new RewardData[0];
        }

        // 요청된 개수만큼 랜덤 선택 (중복 방지)
        List<RewardData> availableRewards = new List<RewardData>(rarityRewards);
        List<RewardData> selectedRewards = new List<RewardData>();

        for (int i = 0; i < count && availableRewards.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableRewards.Count);
            RewardData selected = availableRewards[randomIndex];

            selectedRewards.Add(selected);
            availableRewards.RemoveAt(randomIndex);
        }

        return selectedRewards.ToArray();
    }

    /// <summary>
    /// 특정 카테고리의 보상만 선택
    /// </summary>
    /// <param name="category">선택할 카테고리</param>
    /// <param name="count">선택할 개수</param>
    /// <returns>선택된 보상 배열</returns>
    public RewardData[] SelectRewardsByCategory(RewardCategory category, int count = 1)
    {
        if (rewardDatabase == null)
        {
            Debug.LogError("[WeightedRandomSelector] RewardDatabase가 설정되지 않았습니다!");
            return new RewardData[0];
        }

        var categoryRewards = rewardDatabase.GetRewardsByCategory(category);
        if (categoryRewards.Length == 0)
        {
            Debug.LogWarning($"[WeightedRandomSelector] {category} 카테고리 보상이 없습니다!");
            return new RewardData[0];
        }

        // 카테고리 내에서 가중치 기반 선택
        List<RewardData> availableRewards = new List<RewardData>(categoryRewards);
        List<RewardData> selectedRewards = new List<RewardData>();

        for (int i = 0; i < count && availableRewards.Count > 0; i++)
        {
            RewardData selected = SelectSingleRewardByWeight(availableRewards);
            if (selected != null)
            {
                selectedRewards.Add(selected);
                availableRewards.Remove(selected);
            }
        }

        return selectedRewards.ToArray();
    }

    /// <summary>
    /// 보장된 등급 시스템
    /// 예: 10번째 선택은 반드시 Rare 이상, 50번째는 Epic 이상 등
    /// </summary>
    /// <param name="minRarity">최소 보장 등급</param>
    /// <param name="count">선택할 개수</param>
    /// <returns>선택된 보상 배열</returns>
    public RewardData[] SelectRewardsWithMinRarity(RewardRarity minRarity, int count = 1)
    {
        if (rewardDatabase == null)
        {
            Debug.LogError("[WeightedRandomSelector] RewardDatabase가 설정되지 않았습니다!");
            return new RewardData[0];
        }

        // 최소 등급 이상의 보상들만 필터링
        List<RewardData> qualifiedRewards = new List<RewardData>();
        var allRewards = rewardDatabase.GetValidRewards();

        foreach (RewardData reward in allRewards)
        {
            if (reward.rarity >= minRarity)
            {
                qualifiedRewards.Add(reward);
            }
        }

        if (qualifiedRewards.Count == 0)
        {
            Debug.LogWarning($"[WeightedRandomSelector] {minRarity} 이상 등급 보상이 없습니다!");
            return new RewardData[0];
        }

        // 자격을 갖춘 보상들 중에서 가중치 기반 선택
        List<RewardData> selectedRewards = new List<RewardData>();

        for (int i = 0; i < count && qualifiedRewards.Count > 0; i++)
        {
            RewardData selected = SelectSingleRewardByWeight(qualifiedRewards);
            if (selected != null)
            {
                selectedRewards.Add(selected);
                qualifiedRewards.Remove(selected);
            }
        }

        return selectedRewards.ToArray();
    }

    // =============================================================================
    // 디버깅 및 테스트 기능
    // =============================================================================

    /// <summary>
    /// 확률 분포 테스트 실행
    /// 개발 중 확률이 올바르게 작동하는지 확인용
    /// </summary>
    [ContextMenu("확률 분포 테스트")]
    public void TestProbabilityDistribution()
    {
        if (rewardDatabase == null)
        {
            Debug.LogError("RewardDatabase가 설정되지 않았습니다!");
            return;
        }

        // 테스트 결과 초기화
        testResults.Reset();

        Debug.Log($"=== 확률 분포 테스트 시작 ({testIterations}회) ===");

        // 지정된 횟수만큼 보상 선택 테스트
        for (int i = 0; i < testIterations; i++)
        {
            var selectedRewards = SelectRewards();

            foreach (RewardData reward in selectedRewards)
            {
                if (reward != null)
                {
                    testResults.AddResult(reward.rarity);
                }
            }
        }

        // 결과 출력
        testResults.LogResults(testIterations * rewardCount);
    }

    /// <summary>
    /// 단일 선택 테스트
    /// 한 번의 선택 결과를 즉시 확인
    /// </summary>
    [ContextMenu("단일 선택 테스트")]
    public void TestSingleSelection()
    {
        var rewards = SelectRewards();

        Debug.Log("=== 단일 선택 테스트 결과 ===");
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] != null)
            {
                Debug.Log($"카드 {i + 1}: {rewards[i].ToString()}");
            }
        }
    }

    /// <summary>
    /// 설정된 RewardDatabase 유효성 검사
    /// </summary>
    private void OnValidate()
    {
        if (rewardDatabase != null)
        {
            var validRewards = rewardDatabase.GetValidRewards();
            if (validRewards.Length < rewardCount)
            {
                Debug.LogWarning($"[WeightedRandomSelector] 유효한 보상({validRewards.Length}개)이 요구사항({rewardCount}개)보다 적습니다!");
            }
        }
    }
}

// =============================================================================
// 테스트 결과 데이터 구조
// =============================================================================

/// <summary>
/// 확률 분포 테스트 결과를 저장하는 클래스
/// </summary>
[System.Serializable]
public class RarityTestResults
{
    [Header("등급별 선택 횟수")]
    public int commonCount = 0;
    public int uncommonCount = 0;
    public int rareCount = 0;
    public int epicCount = 0;
    public int legendaryCount = 0;

    /// <summary>
    /// 결과 초기화
    /// </summary>
    public void Reset()
    {
        commonCount = 0;
        uncommonCount = 0;
        rareCount = 0;
        epicCount = 0;
        legendaryCount = 0;
    }

    /// <summary>
    /// 선택 결과 추가
    /// </summary>
    /// <param name="rarity">선택된 등급</param>
    public void AddResult(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common: commonCount++; break;
            case RewardRarity.Uncommon: uncommonCount++; break;
            case RewardRarity.Rare: rareCount++; break;
            case RewardRarity.Epic: epicCount++; break;
            case RewardRarity.Legendary: legendaryCount++; break;
        }
    }

    /// <summary>
    /// 테스트 결과를 로그로 출력
    /// </summary>
    /// <param name="totalSelections">총 선택 횟수</param>
    public void LogResults(int totalSelections)
    {
        Debug.Log("=== 확률 분포 테스트 결과 ===");
        Debug.Log($"총 선택 횟수: {totalSelections}");

        LogRarityResult(RewardRarity.Common, commonCount, totalSelections, 65f);
        LogRarityResult(RewardRarity.Uncommon, uncommonCount, totalSelections, 20f);
        LogRarityResult(RewardRarity.Rare, rareCount, totalSelections, 12f);
        LogRarityResult(RewardRarity.Epic, epicCount, totalSelections, 2.5f);
        LogRarityResult(RewardRarity.Legendary, legendaryCount, totalSelections, 0.5f);
    }

    /// <summary>
    /// 개별 등급 결과 출력
    /// </summary>
    private void LogRarityResult(RewardRarity rarity, int count, int total, float expectedPercent)
    {
        float actualPercent = total > 0 ? (float)count / total * 100f : 0f;
        float difference = actualPercent - expectedPercent;

        string differenceText = difference > 0 ? $"+{difference:F1}%" : $"{difference:F1}%";

        Debug.Log($"{rarity.GetDisplayText()}: {count}회 ({actualPercent:F1}%) " +
                 $"[예상: {expectedPercent}%, 차이: {differenceText}]");
    }
}
