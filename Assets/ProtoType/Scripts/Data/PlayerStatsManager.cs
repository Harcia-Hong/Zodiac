using System.Collections.Generic;
using UnityEngine;
using static RewardData;

public class PlayerStatsManager : MonoBehaviour
{
    [Header("기본 스탯 설정")]
    [Tooltip("게임 시작 시 기본 스탯들 (초기값)")]
    public PlayerBaseStats baseStats = new PlayerBaseStats();

    [Header("현재 적용된 스탯 (읽기 전용)")]
    [SerializeField, Tooltip("보상 적용 후 최종 스탯들")]
    private PlayerCurrentStats currentStats = new PlayerCurrentStats();

    [Header("디버깅 정보")]
    [SerializeField] private int totalRewardsApplied = 0;
    [SerializeField] private string lastAppliedReward = "";

    // 세션 동안 적용된 보상들 관리
    private List<RewardData> appliedRewards = new List<RewardData>();

    // 스탯별 누적 배율 관리 (퍼센트 보상용)
    private Dictionary<StatType, float> statMultipliers = new Dictionary<StatType, float>();

    // 스탯별 추가값 관리 (고정값 보상용)  
    private Dictionary<StatType, float> statBonuses = new Dictionary<StatType, float>();

    // 싱글톤 패턴 (옵션)
    public static PlayerStatsManager Instance { get; private set; }

    private void Awake() // 초기화, 싱글톤 설정
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStats();
        }
        else
            Destroy(gameObject);
    }

    // 스텟 시스템 초기화
    // 기본적으로 모든 스텟 설정, 누적 데이터는 초기화
    void InitializeStats()
    {
        Debug.Log("[PlayerStatsManager] 스탯 시스템 초기화");

        // 누적 데이터 초기화
        appliedRewards.Clear();
        statMultipliers.Clear();
        statBonuses.Clear();
        totalRewardsApplied = 0;
        lastAppliedReward = "";

        // 모든 스탯 타입에 대해 기본 배율(1.0) 설정
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            statMultipliers[statType] = 1f; // 100% (기본값)
            statBonuses[statType] = 0f;     // +0 (추가값 없음)
        }

        // 기본 스탯을 현재 스탯으로 복사
        CopyBaseToCurrentStats();

        Debug.Log($"[PlayerStatsManager] 초기 스탯: 공격력 {currentStats.attackPower}, 체력 {currentStats.health}");
    }

    // 기본 스텟을 현재 스텟으로 복사
    void CopyBaseToCurrentStats()
    {
        currentStats.attackPower = baseStats.attackPower;
        currentStats.health = baseStats.health;
        currentStats.attackSpeed = baseStats.attackSpeed;
        currentStats.movementSpeed = baseStats.movementSpeed;
        currentStats.jumpPower = baseStats.jumpPower;
        currentStats.skillDamage = baseStats.skillDamage;
        currentStats.criticalChance = baseStats.criticalChance;
        currentStats.criticalDamage = baseStats.criticalDamage;
        currentStats.attackRange = baseStats.attackRange;
        
        // 고급 스탯
        currentStats.skillCoolDown = baseStats.skillCoolDown;
        currentStats.hpRegernation = baseStats.hpRegernation;
        currentStats.lifeSteal = baseStats.lifeSteal;
        currentStats.attackSpeedProgressive = baseStats.attackSpeedProgressive;
        currentStats.damageReduction = baseStats.damageReduction;
        currentStats.rushDistance = baseStats.rushDistance;

    }

    // 보상 적용 - 메인 인터페이스
    // RewardUIManager에서 플레이어가 보상을 선택했을 때 호출
    public void ApplyReward(RewardData reward)
    {
        if (reward == null || !reward.isValid())
        {
            Debug.LogError("[PlayerStatsManager] 유효하지 않은 보상입니다!");
            return;
        }

        Debug.Log($"[PlayerStatsManager] 보상 적용: {reward.ToString()}");

        // 중복 적용 방지 (같은 ID의 보상을 여러번 적용하는 것은 허용)
        appliedRewards.Add(reward);

        // 보상 타입에 따라 적용
        if(reward.rewardType == RewardType.SingleStat)
        {
            // 기존 단일 스탯 처릐
            if (reward.valueType == ValueType.Percentage)
                ApplyPercentageReward(reward.targetStat, reward.baseValue); 
            else
                ApplyFlatValueReward(reward.targetStat, reward.baseValue);
        }
        else if(reward.rewardType == RewardType.MultiStat)
        {
            foreach(var multiStat in reward.multiStats)
            {
                if (multiStat.valueType == ValueType.Percentage)
                    ApplyPercentageReward(multiStat.statType, multiStat.value);
                else
                    ApplyFlatValueReward(multiStat.statType, multiStat.value);
            }
        }

        // 최종 스탯 재계산
        RecalculateCurrentStats();

        // 디버깅 정보 업데이트
        totalRewardsApplied++;
        lastAppliedReward = reward.rewardName;

        Debug.Log($"[PlayerStatsManager] 적용 후: {reward.targetStat} = {GetCurrentStatValue(reward.targetStat):F1}");
    }

    // % 기반 보상 적용 ( 곱 연산 )
    void ApplyPercentageReward(StatType statType, float value)
    {
        float multiplierIncrease = value / 100f; // 5% → 0.05
        float newMultiplier = 1f + multiplierIncrease;      // 1.05

        // 기존 배율에 곱연산 적용
        if (statMultipliers.ContainsKey(statType))
        {
            statMultipliers[statType] *= newMultiplier;
        }
        else
        {
            statMultipliers[statType] = newMultiplier;
        }

        Debug.Log($"[PlayerStatsManager] {statType} 배율: {statMultipliers[statType]:F3}");
    }

    // 고정 값 기반 보상 적용
    void ApplyFlatValueReward(StatType statType, float value)
    {
        if (statBonuses.ContainsKey(statType))
        {
            statBonuses[statType] += value;
        }
        else
        {
            statBonuses[statType] = value;
        }

        Debug.Log($"[PlayerStatsManager] {statType} 추가값: +{statBonuses[statType]:F1}");
    }

    // 모든 스텟 기본 값 + 보상 값으로 다시 계산 
    // 순서는 (기본 * 배율) + 추가 값
    void RecalculateCurrentStats()
    {
        // 기본 스탯
        currentStats.attackPower = CalculateFinalStatValue(StatType.AttackPower, baseStats.attackPower);
        currentStats.health = CalculateFinalStatValue(StatType.Health, baseStats.health);
        currentStats.attackSpeed = CalculateFinalStatValue(StatType.AttackSpeed, baseStats.attackSpeed);
        currentStats.movementSpeed = CalculateFinalStatValue(StatType.MovementSpeed, baseStats.movementSpeed);
        currentStats.jumpPower = CalculateFinalStatValue(StatType.JumpPower, baseStats.jumpPower);
        currentStats.skillDamage = CalculateFinalStatValue(StatType.SkillDamage, baseStats.skillDamage);
        currentStats.criticalChance = CalculateFinalStatValue(StatType.CriticalChance, baseStats.criticalChance);
        currentStats.criticalDamage = CalculateFinalStatValue(StatType.CriticalDamage, baseStats.criticalDamage);
        currentStats.attackRange = CalculateFinalStatValue(StatType.AttackRange, baseStats.attackRange);

        // 고급 스탯
        currentStats.skillCoolDown = CalculateFinalStatValue(StatType.SkillCooldown, baseStats.skillCoolDown);
        currentStats.hpRegernation = CalculateFinalStatValue(StatType.HPRegernation, baseStats.hpRegernation);
        currentStats.lifeSteal = CalculateFinalStatValue(StatType.LiftSteal, baseStats.lifeSteal);
        currentStats.attackSpeedProgressive = CalculateFinalStatValue(StatType.AttackSpeedProgressive, baseStats.attackSpeedProgressive);
        currentStats.damageReduction = CalculateFinalStatValue(StatType.DamageReduction, baseStats.damageReduction);
        currentStats.rushDistance = CalculateFinalStatValue(StatType.RushDistance, baseStats.rushDistance);


        // 다른 시스템들에게 스탯 변경 알림
        NotifyStatsChanged();
    }

    // 개별 스텟 최종 값 계산
    float CalculateFinalStatValue(StatType statType, float baseValue)
    {
        float multiplier = statMultipliers.ContainsKey(statType) ? statMultipliers[statType] : 1f;
        float bonus = statBonuses.ContainsKey(statType) ? statBonuses[statType] : 0f;

        return (baseValue * multiplier) + bonus;
    }

    // 스텟 변경을 다른 시스템들에게 알림 전달
    // PlayerController, Weapon 등에서 실제 게임플레이에 반영
    void NotifyStatsChanged()
    {
        // PlayerController에 이동속도 적용
        var playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.moveSpeed = currentStats.movementSpeed;
        }

        // PlayerHealth에 체력 적용
        var playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            // 현재 체력 비율 유지하면서 최대 체력 증가
            float healthRatio = (float)playerHealth.curHealth / playerHealth.maxHealth;
            playerHealth.maxHealth = Mathf.RoundToInt(currentStats.health);
            playerHealth.curHealth = Mathf.RoundToInt(playerHealth.maxHealth * healthRatio);

            // UI 업데이트
            if (playerHealth.hpSlider != null)
            {
                playerHealth.hpSlider.maxValue = playerHealth.maxHealth;
                playerHealth.hpSlider.value = playerHealth.curHealth;
            }
        }

        // Weapon에 공격력 적용
        var weapon = FindFirstObjectByType<Weapon>();
        if (weapon != null)
        {
            weapon.damage = Mathf.RoundToInt(currentStats.attackPower);
        }

        Debug.Log("[PlayerStatsManager] 스탯 변경 사항이 게임에 반영되었습니다.");
    }

    // 공개 인터페이스 (다른 시스템에서 사용)

    // 특정 스탯의 현재 값 반환
    public float GetCurrentStatValue(StatType statType)
    {
        switch (statType)
        {
            // 기존 스탯
            case StatType.AttackPower: return currentStats.attackPower;
            case StatType.Health: return currentStats.health;
            case StatType.AttackSpeed: return currentStats.attackSpeed;
            case StatType.MovementSpeed: return currentStats.movementSpeed;
            case StatType.JumpPower: return currentStats.jumpPower;
            case StatType.SkillDamage: return currentStats.skillDamage;
            case StatType.CriticalChance: return currentStats.criticalChance;
            case StatType.CriticalDamage: return currentStats.criticalDamage;
            case StatType.AttackRange: return currentStats.attackRange;

            // 고급 스탯
            case StatType.SkillCooldown: return currentStats.skillDamage;
            case StatType.HPRegernation: return currentStats.hpRegernation;
            case StatType.LiftSteal: return currentStats.lifeSteal;
            case StatType.AttackSpeedProgressive: return currentStats.attackSpeedProgressive;
            case StatType.DamageReduction: return currentStats.damageReduction;
            case StatType.RushDistance: return currentStats.rushDistance;

            default: return 0f;
        }
    }

    // 특정 스탯의 기본 값 반환
    public float GetBaseStatValue(StatType statType)
    {
        switch (statType)
        {
            // 기본 스탯
            case StatType.AttackPower: return baseStats.attackPower;
            case StatType.Health: return baseStats.health;
            case StatType.AttackSpeed: return baseStats.attackSpeed;
            case StatType.MovementSpeed: return baseStats.movementSpeed;
            case StatType.JumpPower: return baseStats.jumpPower;
            case StatType.SkillDamage: return baseStats.skillDamage;
            case StatType.CriticalChance: return baseStats.criticalChance;
            case StatType.CriticalDamage: return baseStats.criticalDamage;
            case StatType.AttackRange: return baseStats.attackRange;

            // 고급 스탯
            case StatType.SkillCooldown: return baseStats.skillCoolDown;
            case StatType.HPRegernation: return baseStats.hpRegernation;
            case StatType.LiftSteal: return baseStats.lifeSteal;
            case StatType.AttackSpeedProgressive: return baseStats.attackSpeedProgressive;
            case StatType.DamageReduction: return baseStats.damageReduction;
            case StatType.RushDistance: return baseStats.rushDistance;

            default: return 0f;
        }
    }

    // 현재 적용 된 모든 보상 목록 반환
    public List<RewardData> GetAppliedRewards()
    {
        return new List<RewardData>(appliedRewards);
    }

    // 세션 초기화 ( 재시작, 사망 등등 여러 이유 )
    public void ResetSession()
    {
        Debug.Log("[PlayerStatsManager] 세션 초기화 - 모든 보상 효과 제거");
        InitializeStats();
    }

    // 특정 스탯 증가율 반환 ( 디버깅 용도 )
    public float GetStatIncreasePercentage(StatType statType)
    {
        float baseValue = GetBaseStatValue(statType);
        float currentValue = GetCurrentStatValue(statType);

        if (baseValue <= 0) return 0f;

        return ((currentValue - baseValue) / baseValue) * 100f;
    }

    // 모든 스탯의 정보를 로그로 출력 ( 디버깅 용도 )[ContextMenu("현재 스탯 정보 출력")]
    public void LogCurrentStats()
    {
        Debug.Log("=== 현재 플레이어 스탯 ===");
        foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
        {
            float baseValue = GetBaseStatValue(statType);
            float currentValue = GetCurrentStatValue(statType);
            float increasePercent = GetStatIncreasePercentage(statType);

            Debug.Log($"{statType}: {baseValue:F1} → {currentValue:F1} (+{increasePercent:F1}%)");
        }
        Debug.Log($"총 적용된 보상 수: {totalRewardsApplied}");
    }
}

// =============================================================================
// 데이터 구조체들
// =============================================================================

/// <summary>
/// 플레이어의 기본 스탯 (게임 시작 시 초기값)
/// </summary>
[System.Serializable]
public class PlayerBaseStats
{
    [Header("기본 전투 스탯")]
    public float attackPower = 50f;      // 기본 공격력
    public float health = 100f;          // 기본 체력

    [Header("전투 특화 스탯")]
    public float attackSpeed = 1f;       // 기본 공격 속도 (배율)
    public float skillDamage = 50f;      // 기본 스킬 피해량
    public float criticalChance = 5f;    // 기본 치명타 확률 (%)
    public float criticalDamage = 150f;  // 기본 치명타 데미지 (%)
    public float attackRange = 2f;       // 기본 공격 범위

    [Header("이동 스탯")]
    public float movementSpeed = 10f;     // 기본 이동 속도
    public float jumpPower = 7f;         // 기본 점프력

    [Header("고급 전투 스탯")]
    public float skillCoolDown = 0f;
    public float hpRegernation = 0f;
    public float lifeSteal = 0f;
    public float attackSpeedProgressive = 0f;
    public float damageReduction = 0f;
    public float rushDistance = 0f;
}

/// <summary>
/// 현재 적용된 스탯 (보상 효과 포함)
/// </summary>
[System.Serializable]
public class PlayerCurrentStats
{
    [Header("현재 전투 스탯")]
    public float attackPower;
    public float health;

    [Header("현재 전투 특화 스탯")]
    public float attackSpeed;
    public float skillDamage;
    public float criticalChance;
    public float criticalDamage;
    public float attackRange;

    [Header("현재 이동 스탯")]
    public float movementSpeed;
    public float jumpPower;

    [Header("현재 고급 전투 스탯")]
    public float skillCoolDown;
    public float hpRegernation;
    public float lifeSteal;
    public float attackSpeedProgressive;
    public float damageReduction;
    public float rushDistance;
}
