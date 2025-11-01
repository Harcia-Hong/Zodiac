using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static RewardData;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana System")]
    public int maxMana = 100;
    public int curMana;
    public Slider mpSlider;

    // 마나 리젠
    float regenTimer = 0f;
    float regenInteravl = 1f; // 1s 마다 재생

    private void Awake()
    {
        curMana = maxMana;

        if (mpSlider != null)
        {
            mpSlider.maxValue = maxMana;
            mpSlider.value = curMana;
        }
    }

    private void Update()
    {
        HandleManaRegen();
    }

    void HandleManaRegen()
    {
        if (curMana >= maxMana) return;

        regenTimer += Time.deltaTime;

        if (regenTimer >= regenInteravl)
        {
            regenTimer = 0f;

            // 기본 마나 리젠
            float regenAmount = 5f;

            // 추가 스탯으로 마나 리젠 증가시
            /*if (PlayerStatsManager.Instance != null)
            {
                regenAmount += PlayerStatsManager.Instance.GetManaRegen();
            }*/

            curMana += Mathf.RoundToInt(regenAmount);
            curMana = Mathf.Clamp(curMana, 0, maxMana);

            // DoTween
            if (mpSlider != null)
            {
                mpSlider.DOValue(curMana, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }

    public bool TryConsumeMana(int amount)
    {
        if (curMana >= amount)
        {
            curMana -= amount;
            curMana = Mathf.Clamp(curMana, 0, maxMana);

            // DoTween
            if (mpSlider != null)
            {
                mpSlider.DOValue(curMana, 0.3f).SetEase(Ease.OutQuad);
            }

            return true;
        }

        Debug.Log("[마나 부족] 필요: " + amount + ", 현재: " + curMana);
        return false;
    }
}
