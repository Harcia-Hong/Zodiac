using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 검 무기 이펙트 관리 클래스
/// 기본 공격(1타, 2타) 및 FlashSlash 이펙트 재생
/// </summary>
public class SwordEffect : MonoBehaviour, IEffect
{
    [Header("Sword Attack VFX")]
    [Tooltip("1타 공격 VFX")]
    public VisualEffect attack1VFX;
    public VisualEffect attack1_groundVFX;

    [Tooltip("FlashSlash VFX")]
    public VisualEffect flashSlashVFX;

    /// <summary>기본 공격 이펙트 재생</summary>
    /// <param name="comboIndex">콤보 인덱스 (0=1타, 1=2타)</param>
    public void PlayComboAttackEffect(int comboIndex)
    {
        switch (comboIndex)
        {
            case 0:
                if (attack1VFX != null)
                {
                    attack1VFX.SendEvent("OnPlay");
                    Debug.Log("[SwordEffect] 1타 이펙트 재생");
                }

                if (attack1_groundVFX != null)
                {
                    attack1_groundVFX.SendEvent("OnPlay");
                }
                break;

            default:
                Debug.LogWarning($"[SwordEffect] 잘못된 콤보 인덱스: {comboIndex}");
                break;
        }
    }

    /// <summary>FlashSlash 이펙트 재생</summary>
    public void PlayFlashSlashEffect()
    {
        if (flashSlashVFX != null)
        {
            flashSlashVFX.SendEvent("OnPlay");
            Debug.Log("[SwordEffect] FlashSlash 이펙트 재생");
        }
    }
}
