// =============================================================================
// RewardDatabase.cs - 보상 데이터베이스 관리 시스템
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using static RewardData;

/// <summary>
/// 모든 보상 데이터를 중앙에서 관리하는 데이터베이스 클래스
/// ScriptableObject로 생성하여 Inspector에서 보상 목록을 관리할 수 있음
/// </summary>
[CreateAssetMenu(fileName = "RewardDatabase", menuName = "Reward System/Reward Database")]
public class RewardDatabase : ScriptableObject
{
    [Header("보상 데이터 목록")]
    [Tooltip("게임에서 사용할 모든 보상 데이터를 여기에 할당")]
    public RewardData[] allRewards;

    [Header("디버그 정보")]
    [SerializeField, Tooltip("등급별 보상 개수 (읽기 전용)")]
    private string debugInfo = "";

    /// <summary>
    /// 특정 등급의 보상들만 필터링하여 반환
    /// 확률 기반 선택 시 등급별로 나누어 선택하기 위해 사용
    /// </summary>
    /// <param name="rarity">필터링할 보상 등급</param>
    /// <returns>해당 등급의 유효한 보상 배열</returns>
    public RewardData[] GetRewardsByRarity(RewardRarity rarity)
    {
        List<RewardData> filteredRewards = new List<RewardData>();

        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.rarity == rarity && reward.isValid())
            {
                filteredRewards.Add(reward);
            }
        }

        return filteredRewards.ToArray();
    }

    /// <summary>
    /// 특정 카테고리의 보상들만 필터링하여 반환
    /// 카테고리별 보상 선택이나 UI 표시 시 사용
    /// </summary>
    /// <param name="category">필터링할 보상 카테고리</param>
    /// <returns>해당 카테고리의 유효한 보상 배열</returns>
    public RewardData[] GetRewardsByCategory(RewardCategory category)
    {
        List<RewardData> filteredRewards = new List<RewardData>();

        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.category == category && reward.isValid())
            {
                filteredRewards.Add(reward);
            }
        }

        return filteredRewards.ToArray();
    }

    /// <summary>
    /// 특정 스탯 타입의 보상들만 필터링하여 반환
    /// 중복 방지나 특정 스탯 관련 보상만 찾을 때 사용
    /// </summary>
    /// <param name="statType">필터링할 스탯 타입</param>
    /// <returns>해당 스탯의 유효한 보상 배열</returns>
    public RewardData[] GetRewardsByStat(StatType statType)
    {
        List<RewardData> filteredRewards = new List<RewardData>();

        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.targetStat == statType && reward.isValid())
            {
                filteredRewards.Add(reward);
            }
        }

        return filteredRewards.ToArray();
    }

    /// <summary>
    /// 모든 유효한 보상 데이터 반환
    /// 전체 보상 풀에서 선택할 때 사용
    /// </summary>
    /// <returns>유효성 검사를 통과한 모든 보상 배열</returns>
    public RewardData[] GetValidRewards()
    {
        List<RewardData> validRewards = new List<RewardData>();

        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.isValid())
            {
                validRewards.Add(reward);
            }
        }

        return validRewards.ToArray();
    }

    /// <summary>
    /// 특정 ID의 보상 데이터 검색
    /// 저장된 보상 정보를 불러올 때나 특정 보상을 찾을 때 사용
    /// </summary>
    /// <param name="rewardID">검색할 보상의 고유 ID</param>
    /// <returns>해당 ID의 보상 데이터, 없으면 null</returns>
    public RewardData GetRewardByID(string rewardID)
    {
        if (string.IsNullOrEmpty(rewardID))
        {
            Debug.LogWarning("[RewardDatabase] 검색할 보상 ID가 비어있습니다.");
            return null;
        }

        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.rewardID == rewardID)
            {
                return reward;
            }
        }

        Debug.LogWarning($"[RewardDatabase] ID '{rewardID}'에 해당하는 보상을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 중복 제거를 위한 보상 선택
    /// 이미 선택된 보상들을 제외하고 새로운 보상을 반환
    /// </summary>
    /// <param name="excludeRewards">제외할 보상 목록</param>
    /// <returns>제외 목록에 포함되지 않은 유효한 보상 배열</returns>
    public RewardData[] GetRewardsExcluding(RewardData[] excludeRewards)
    {
        List<RewardData> availableRewards = new List<RewardData>();
        HashSet<string> excludeIDs = new HashSet<string>();

        // 제외할 보상들의 ID 수집
        foreach (RewardData exclude in excludeRewards)
        {
            if (exclude != null)
            {
                excludeIDs.Add(exclude.rewardID);
            }
        }

        // 제외 목록에 없는 보상들만 수집
        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.isValid() && !excludeIDs.Contains(reward.rewardID))
            {
                availableRewards.Add(reward);
            }
        }

        return availableRewards.ToArray();
    }

    /// <summary>
    /// 등급별 보상 개수 반환
    /// 밸런싱이나 디버깅 시 등급 분포 확인용
    /// </summary>
    /// <returns>등급별 보상 개수 딕셔너리</returns>
    public Dictionary<RewardRarity, int> GetRewardCountByRarity()
    {
        Dictionary<RewardRarity, int> counts = new Dictionary<RewardRarity, int>();

        // 모든 등급을 0으로 초기화
        foreach (RewardRarity rarity in System.Enum.GetValues(typeof(RewardRarity)))
        {
            counts[rarity] = 0;
        }

        // 유효한 보상들의 등급별 개수 카운트
        foreach (RewardData reward in allRewards)
        {
            if (reward != null && reward.isValid())
            {
                counts[reward.rarity]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// 데이터베이스 유효성 검사
    /// Inspector의 버튼이나 에디터 스크립트에서 호출
    /// </summary>
    [ContextMenu("데이터베이스 유효성 검사")]
    public void ValidateDatabase()
    {
        int validCount = 0;
        int invalidCount = 0;
        List<string> duplicateIDs = new List<string>();
        List<string> emptyFields = new List<string>();

        // ID 중복 검사용 HashSet
        HashSet<string> seenIDs = new HashSet<string>();

        foreach (RewardData reward in allRewards)
        {
            if (reward == null)
            {
                invalidCount++;
                emptyFields.Add("null 참조");
                continue;
            }

            // 기본 유효성 검사
            if (reward.isValid())
            {
                validCount++;

                // ID 중복 검사
                if (seenIDs.Contains(reward.rewardID))
                {
                    duplicateIDs.Add(reward.rewardID);
                }
                else
                {
                    seenIDs.Add(reward.rewardID);
                }
            }
            else
            {
                invalidCount++;

                // 구체적인 오류 원인 파악
                if (string.IsNullOrEmpty(reward.rewardID))
                    emptyFields.Add($"{reward.name}: rewardID 누락");
                if (string.IsNullOrEmpty(reward.rewardName))
                    emptyFields.Add($"{reward.name}: rewardName 누락");
                if (reward.baseValue <= 0)
                    emptyFields.Add($"{reward.name}: baseValue 잘못됨 ({reward.baseValue})");
            }
        }

        // 결과 출력
        Debug.Log($"=== 보상 데이터베이스 검사 결과 ===");
        Debug.Log($"총 보상 수: {allRewards.Length}");
        Debug.Log($"유효한 보상: {validCount}개");
        Debug.Log($"무효한 보상: {invalidCount}개");

        if (duplicateIDs.Count > 0)
        {
            Debug.LogError($"중복된 보상 ID: {string.Join(", ", duplicateIDs)}");
        }

        if (emptyFields.Count > 0)
        {
            Debug.LogWarning($"오류 세부사항:\n{string.Join("\n", emptyFields)}");
        }

        // 등급별 분포 출력
        var rarityCount = GetRewardCountByRarity();
        Debug.Log("=== 등급별 보상 분포 ===");
        foreach (var kvp in rarityCount)
        {
            Debug.Log($"{kvp.Key.GetDisplayText()}: {kvp.Value}개");
        }

        // 디버그 정보 업데이트
        UpdateDebugInfo();
    }

    /// <summary>
    /// Inspector에 표시할 디버깅 정보 업데이트
    /// </summary>
    private void UpdateDebugInfo()
    {
        var counts = GetRewardCountByRarity();
        var infoLines = new List<string>();

        foreach (var kvp in counts)
        {
            infoLines.Add($"{kvp.Key}: {kvp.Value}개");
        }

        debugInfo = string.Join(" | ", infoLines);
    }

    /// <summary>
    /// 에디터에서 데이터 변경 시 자동으로 디버그 정보 업데이트
    /// </summary>
    private void OnValidate()
    {
        UpdateDebugInfo();
    }

    /// <summary>
    /// 빈 보상 데이터 생성 (테스트용)
    /// </summary>
    [ContextMenu("테스트 보상 데이터 생성")]
    public void CreateTestRewards()
    {
        Debug.Log("[RewardDatabase] 테스트 보상 데이터 생성 기능은 에디터 스크립트에서 구현해주세요.");
        // 실제 구현은 Editor 폴더의 스크립트에서 진행
    }
}