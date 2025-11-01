using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static RewardData;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int curHealth;
    public Slider hpSlider;

    Animator anim;
    public bool isDead = false;

    // 스탯을 통한 채력 재생 관련 변수
    float regenTimer = 0f;
    float regenInterval = 1f; // 1초마다 재생

    private void Awake()
    {
        curHealth = maxHealth;
        anim = GetComponentInChildren<Animator>();
        
        if(hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
        }
    }

    private void Update()
    {
        // 스탯으로 얻은 체력 재생 처리
        HandleHPRegeneration();
    }

    void HandleHPRegeneration()
    {
        if (isDead || curHealth >= maxHealth) return;

        regenTimer += Time.deltaTime;

        if(regenTimer >= regenInterval)
        {
            regenTimer = 0f;

            if(PlayerStatsManager.Instance != null)
            {
                float regenAmount = PlayerStatsManager.Instance.GetCurrentStatValue(RewardData.StatType.HPRegernation);

                if(regenAmount > 0)
                {
                    curHealth += Mathf.RoundToInt(regenAmount);
                    curHealth = Mathf.Clamp(curHealth, 0, maxHealth);

                    // UI 업데이트
                    if(hpSlider != null)
                    {
                        hpSlider.DOValue(curHealth, 0.3f).SetEase(Ease.OutQuad);
                    }

                    Debug.Log($"[체력 재생] +{regenAmount} HP (현재: {curHealth}/{maxHealth})");
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 뎀감을 통한 피감 적용
        float finalDamage = damage;
        if(PlayerStatsManager.Instance != null)
        {
            float damageReduction = PlayerStatsManager.Instance.GetCurrentStatValue(RewardData.StatType.DamageReduction);
            finalDamage = damage * (1f - (damageReduction / 100f));
        }

        curHealth -= Mathf.RoundToInt(finalDamage);
        curHealth = Mathf.Clamp(curHealth, 0, maxHealth); // - 를 통한 음수 방지

        Debug.Log($"[피해] 원래: {damage} → 최종: {finalDamage:F1} (감소율: {PlayerStatsManager.Instance?.GetCurrentStatValue(StatType.DamageReduction) ?? 0}%)");

        // DOTween 애니메이션 적용
        if (hpSlider != null)
        {
            hpSlider.DOValue(curHealth, 0.3f).SetEase(Ease.OutQuad); // SetEase : 빠르게 시작해서, 천천히 감속하는 감속 속선, hp가 부드럽게 감소하는 느낌 연출
        }

        if (curHealth > 0)
        // anim.SetTrigger("doHit");피격 애니메이션 삭제
            { }
        else
            Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("doDie");
    }
}
