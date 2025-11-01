using UnityEngine;

[CreateAssetMenu(fileName = "RewardData", menuName = "Scriptable Objects/RewardData")]
public class RewardData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("보상의 고유 ID ( 중복 방지 용도 )")]
    public string rewardID;

    [Header("보상 이름 : UI에 표시")]
    public string rewardName;

    [Header("보상 설명 : 상세 내용")]
    public string description;

    [Header("보상 등급 및 카테고리")]
    [Tooltip("보상의 희귀도")]
    public RewardRarity rarity = RewardRarity.Common;

    [Tooltip("보상의 카테고리")]
    public RewardCategory category = RewardCategory.Stats;

    [Header("보상 타입")]
    public RewardType rewardType = RewardType.SingleStat;

    [Header("수치 설정")]
    [Tooltip("수치 적용 방식 (퍼센트 or 고정값)")]
    public ValueType valueType = ValueType.Percentage;

    [Tooltip("기본 수치 (3, 5, 10 또는 고정값)")]
    public float baseValue = 5f;

    [Tooltip("영향을 줄 스탯 종류")]
    public StatType targetStat = StatType.AttackPower;

    [Header("복합 스탯 보상")]
    public MultiStatReward[] multiStats;

    [Header("UI 리소스")]
    [Tooltip("카드에 표시될 아이콘")]
    public Sprite iconSprite;

    [Tooltip("카드 배경 (등급별로 다를 수 있음)")]
    public Sprite cardBackground;

    public string GetDisplayText()
    {
        if(rewardType == RewardType.SingleStat) // 단일 스탯 처리
        {
            string statName = GetStatDisplayName(targetStat);

            if (valueType == ValueType.Percentage)
            {
                return $"{statName} +{baseValue}%";
            }
            else
            {
                return $"{statName} +{baseValue:F0}";
            }
        }
        else if(rewardType == RewardType.MultiStat)
        {
            if(multiStats.Length == 1)
            {
                var stat = multiStats[0];
                string statName = GetStatDisplayName(stat.statType);
                if (stat.valueType == ValueType.Percentage)
                    return $"{statName} +{stat.value}%";
                else
                    return $"{statName} +{stat.value:F0}";
            }
            else
            {
                // 여러 스탯인 경우 첫 번째만 표시하고 "& more" 추가
                var firstStat = multiStats[0];
                string statName = GetStatDisplayName(firstStat.statType);
                string valueText = firstStat.valueType == ValueType.Percentage ?
                    $"+{firstStat.value}%" : $"+{firstStat.value:F0}";

                return $"{statName} {valueText} & {multiStats.Length - 1} more";
            }
        }

        return "Unknown Reward";
    }

    public string GetDetailedDisplayText()
    {
        if(rewardType == RewardType.SingleStat)
        {
            return GetDisplayText();
        }
        else if(rewardType == RewardType.MultiStat && multiStats != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for(int i = 0; i<multiStats.Length; i++)
            {
                var stat = multiStats[i];
                string statName = GetStatDisplayName(stat.statType);
                string valueText = stat.valueType == ValueType.Percentage ?
                    $"+{stat.value}%" : $"+{stat.value:F0}";

                sb.Append($"{statName} {valueText}");

                if (i < multiStats.Length - 1)
                    sb.Append("\n");
            }

            return sb.ToString();
        }

        return "Invalid Reward";
    }

    private string GetStatDisplayName(StatType stat)
    {
        switch (stat)
        {
            case StatType.AttackPower: return "Attack Power";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.MovementSpeed: return "Movement Speed";
            case StatType.JumpPower: return "Jump Power";
            case StatType.Health: return "Health";
            case StatType.SkillDamage: return "Skill Damage";
            case StatType.CriticalChance: return "Critical Chance";
            case StatType.CriticalDamage: return "Critical Damage";
            case StatType.AttackRange: return "Attack Range";

            // 추가 스텟
            case StatType.SkillCooldown: return "Skill Cooldown";
            case StatType.HPRegernation: return "HP Regen";
            case StatType.LiftSteal: return "Life Steal";
            case StatType.AttackSpeedProgressive: return "Progressive Attack Speed";
            case StatType.DamageReduction: return "Damage Reduction";
            case StatType.RushDistance: return "Rush Distance";
            default: return stat.ToString();
        }
    }

    public bool isValid()
    {
        if (string.IsNullOrEmpty(rewardID) || string.IsNullOrEmpty(rewardName))
            return false;

        if (rewardType == RewardType.SingleStat)
        {
            return baseValue > 0;
        }
        else if (rewardType == RewardType.MultiStat)
        {
            if (multiStats == null || multiStats.Length == 0)
                return false;

            foreach (var stat in multiStats)
            {
                if (stat.value <= 0)
                    return false;
            }
            return true;
        }

        return false;
    }

    public override string ToString()
    {
        return $"[{rarity}] {rewardName} : {GetDisplayText()}";
    }


    // 열거형 정의
    // ================================================================

    // 열거형 확장
    public enum RewardType
    {
        SingleStat,
        MultiStat
    };

    // 보상의 희귀도 분류
    [System.Serializable]
    public class MultiStatReward
    {
        public StatType statType;
        public float value;
        public ValueType valueType;
    }

    public enum RewardRarity
    {
        Common = 0,     // 일반 65%
        Uncommon = 1,   // 희귀 20%
        Rare = 2,       // 고급 12%
        Epic = 3,       // 영웅 2.5%
        Legendary = 4   // 전설 0.5%
    };

    // 보상의 카테고리 분류
    public enum RewardCategory
    {
        Stats,         // 기본 스텟
        Combat,        // 전투 관련
        Movement,      // 이동 관련
        Special        // 특수 
    };

    // 수치 적용 방식
    public enum ValueType
    {
        Percentage, // 퍼센트 형식 ( 기본 값에 % 증가 )
        FlatValue   // 고정 값 형식 ( 가본 값에 + )
    }

    // 영향을 받을 스탯 종류
    public enum StatType
    {
        // Basic Stat
        AttackPower,
        Health,

        // Combat Stat
        AttackSpeed,
        SkillDamage,
        CriticalChance,
        CriticalDamage,
        AttackRange,

        // Movement Stat
        MovementSpeed,
        JumpPower,

        // 추가 스텟
        SkillCooldown,
        HPRegernation,
        LiftSteal,
        AttackSpeedProgressive,
        DamageReduction,
        RushDistance
    };


}