using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 전투 UI를 관리
public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance;

    [Header("Skill Slots")]
    public Image[] skillIcons;
    public Image[] skillCoolDownOverlays;

    [Header("Sword Slot")]
    public Image swordIcon;
    public Animator swordShineEffect;

    [Header("Item Slots")]
    public Image[] itemIcons;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetSkillIcon(int index, Sprite icon)
    {
        if(index >= 0 && index < skillIcons.Length)
        {
            skillIcons[index].sprite = icon;
        }
    }

    public void StartSkillCoolDown(int index, float duration)
    {
        if (index >= 0 && index < skillCoolDownOverlays.Length)
            StartCoroutine(CooldownRoutine(skillCoolDownOverlays[index], duration));
    }

    IEnumerator CooldownRoutine(Image overlay, float duration)
    {
        overlay.fillAmount = 1f; // 시작 시 오버레이 가득 찬 상태
        float time = 0f;
        while( time < duration)
        {
            time += Time.unscaledDeltaTime; // 일시 정지 중에도 정상적으로 작동하게
            overlay.fillAmount = 1f - (time / duration);
            yield return null;
        }
        overlay.fillAmount = 0f;
    }

    public void SetSwordIcon(Sprite icon)
    {
        if(swordIcon != null)
            swordIcon.sprite = icon; // 검술 아이콘을 새롭게 지정한 Icon으로 변경하는거
    }

    public void SetSwordShineEffect(bool active)
    {
        if (swordShineEffect != null)
            swordShineEffect.SetBool("isShining", active); 
    }

    public void SetItemIcon(int index, Sprite icon)
    {
        if(index >= 0 && index < itemIcons.Length)
            itemIcons[index].sprite = icon;
    }

}
