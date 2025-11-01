// =============================================================================
// RewardRarityExtensions.cs - 보상 등급별 설정 확장 메서드
// =============================================================================
using UnityEngine;
using static RewardData;

/// <summary>
/// 보상 등급별 추가 정보를 제공하는 확장 메서드들
/// 확률, 색상, 배율 등을 중앙에서 관리
/// </summary>
public static class RewardRarityExtensions
{
    /// <summary>
    /// 등급별 확률 가중치 반환
    /// 총 100%가 되도록 설정 (65% + 20% + 12% + 2.5% + 0.5% = 100%)
    /// </summary>
    public static float GetWeight(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common: return 65f;      // 65%
            case RewardRarity.Uncommon: return 20f;    // 20%
            case RewardRarity.Rare: return 12f;        // 12%
            case RewardRarity.Epic: return 2.5f;       // 2.5%
            case RewardRarity.Legendary: return 0.5f;  // 0.5%
            default:
                Debug.LogWarning($"정의되지 않은 보상 등급: {rarity}");
                return 1f;
        }
    }

    /// <summary>
    /// 등급별 UI 표시 색상 반환
    /// 카드 테두리, 텍스트 색상 등에 사용
    /// </summary>
    public static Color GetColor(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common:
                return new Color(0.7f, 0.7f, 0.7f);        // 밝은 회색
            case RewardRarity.Uncommon:
                return new Color(0.2f, 0.8f, 0.2f);        // 밝은 녹색
            case RewardRarity.Rare:
                return new Color(0.3f, 0.6f, 1f);          // 밝은 파랑
            case RewardRarity.Epic:
                return new Color(0.7f, 0.3f, 1f);          // 밝은 보라
            case RewardRarity.Legendary:
                return new Color(1f, 0.85f, 0.2f);         // 밝은 황금
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 등급별 어두운 색상 반환 (배경용)
    /// 카드 배경, 그림자 등에 사용
    /// </summary>
    public static Color GetDarkColor(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common:
                return new Color(0.4f, 0.4f, 0.4f);        // 어두운 회색
            case RewardRarity.Uncommon:
                return new Color(0.1f, 0.4f, 0.1f);        // 어두운 녹색
            case RewardRarity.Rare:
                return new Color(0.1f, 0.3f, 0.6f);        // 어두운 파랑
            case RewardRarity.Epic:
                return new Color(0.4f, 0.1f, 0.6f);        // 어두운 보라
            case RewardRarity.Legendary:
                return new Color(0.6f, 0.5f, 0.1f);        // 어두운 황금
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// 등급별 표시 텍스트 반환
    /// UI에서 "Common", "Rare" 등으로 표시할 때 사용
    /// </summary>
    public static string GetDisplayText(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common: return "Common";
            case RewardRarity.Uncommon: return "Uncommon";
            case RewardRarity.Rare: return "Rare";
            case RewardRarity.Epic: return "Epic";
            case RewardRarity.Legendary: return "Legendary";
            default: return rarity.ToString();
        }
    }

    /// <summary>
    /// 등급에 따른 기본 수치 배율 반환
    /// 같은 보상이라도 등급이 높으면 더 강한 효과를 가질 수 있음
    /// 현재는 사용하지 않지만 추후 확장 시 활용 가능
    /// </summary>
    public static float GetValueMultiplier(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common: return 1f;       // 기본값 (100%)
            case RewardRarity.Uncommon: return 1.2f;   // 20% 증가
            case RewardRarity.Rare: return 1.5f;       // 50% 증가
            case RewardRarity.Epic: return 2f;         // 100% 증가
            case RewardRarity.Legendary: return 3f;    // 200% 증가
            default: return 1f;
        }
    }

    /// <summary>
    /// 등급별 카드 빛나는 효과 강도 반환
    /// 추후 시각 효과 구현 시 사용
    /// </summary>
    public static float GetGlowIntensity(this RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.Common: return 0f;       // 빛나지 않음
            case RewardRarity.Uncommon: return 0.3f;   // 약간 빛남
            case RewardRarity.Rare: return 0.6f;       // 중간 빛남
            case RewardRarity.Epic: return 0.9f;       // 많이 빛남
            case RewardRarity.Legendary: return 1.5f;  // 강하게 빛남
            default: return 0f;
        }
    }

    /// <summary>
    /// 등급별 카테고리 표시 접두사 반환
    /// UI에서 ">Stats", ">ARMs" 등으로 표시할 때 사용
    /// </summary>
    public static string GetCategoryPrefix(this RewardCategory category)
    {
        switch (category)
        {
            case RewardCategory.Stats: return ">Stats";
            case RewardCategory.Combat: return ">Combat";
            case RewardCategory.Movement: return ">Movement";   
            case RewardCategory.Special: return ">Special";
            default: return ">" + category.ToString();
        }
    }

    /// <summary>
    /// 전체 등급별 확률 정보를 디버그 로그로 출력
    /// 개발 중 확률 분포 확인용
    /// </summary>
    public static void LogProbabilityDistribution()
    {
        float totalWeight = 0f;
        var rarities = System.Enum.GetValues(typeof(RewardRarity));

        foreach (RewardRarity rarity in rarities)
        {
            totalWeight += rarity.GetWeight();
        }

        Debug.Log("=== 보상 확률 분포 ===");
        foreach (RewardRarity rarity in rarities)
        {
            float weight = rarity.GetWeight();
            float percentage = (weight / totalWeight) * 100f;
            Debug.Log($"{rarity.GetDisplayText()}: {weight} ({percentage:F1}%)");
        }
        Debug.Log($"총합: {totalWeight} (100%)");
    }
}