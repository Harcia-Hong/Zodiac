using UnityEngine;

/// <summary>
/// 애니메이션 이벤트 핸들러
/// 애니메이션 클립에서 호출되는 이벤트를 받아 EffectGenerator에 전달
/// </summary>
public class AnimationEventHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("EffectGenerator 참조")]
    public EffectGenerator effectGenerator;

    /// <summary>초기화</summary>
    private void Awake()
    {
        // EffectGenerator 자동 찾기
        if (effectGenerator == null)
        {
            effectGenerator = GetComponentInChildren<EffectGenerator>();
        }

        if (effectGenerator == null)
        {
            Debug.LogError("[AnimationEventHandler] EffectGenerator를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 기본 공격 이펙트 재생 (애니메이션 이벤트에서 호출)
    /// </summary>
    /// <param name="comboIndex">콤보 인덱스 (0=1타, 1=2타)</param>
    public void PlayComboAttackEffect(int comboIndex)
    {
        if (effectGenerator == null)
        {
            Debug.LogError("[AnimationEventHandler] EffectGenerator가 없습니다!");
            return;
        }

        // SwordEffects 이펙트 가져오기
        IEffect swordEffect = effectGenerator.GetWeaponEffect("SwordEffects");

        if (swordEffect != null)
        {
            swordEffect.PlayComboAttackEffect(comboIndex);
            Debug.Log($"[AnimationEventHandler] {comboIndex + 1}타 공격 이펙트 재생");
        }
        else
        {
            Debug.LogWarning("[AnimationEventHandler] SwordEffects를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// FlashSlash 이펙트 재생 (애니메이션 이벤트에서 호출)
    /// </summary>
    public void PlayFlashSlashEffect()
    {
        if (effectGenerator == null)
        {
            Debug.LogError("[AnimationEventHandler] EffectGenerator가 없습니다!");
            return;
        }

        IEffect swordEffect = effectGenerator.GetWeaponEffect("SwordEffects");

        if (swordEffect != null)
        {
            swordEffect.PlayFlashSlashEffect();
            Debug.Log("[AnimationEventHandler] FlashSlash 이펙트 재생");
        }
        else
        {
            Debug.LogWarning("[AnimationEventHandler] SwordEffects를 찾을 수 없습니다!");
        }
    }
}
