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

    [Header("UI Expansion (Ctrl Key)")]
    [Tooltip("Ctrl키를 눌렀을 때 나타나는 플레이어 상세 정보창")]
    [SerializeField] private GameObject playerStatsPanel;

    [Tooltip("스킬 슬롯(RMB, Q, E, R, T, LMB)을 감싸는 부모 RectTransform")]
    [SerializeField] private RectTransform skillBarContainer;

    [Tooltip("확장 시 적용할 배율")]
    [SerializeField] private float expandedScale = 1.2f;

    private Vector3 originalSkillBarScale;
    private bool isExpanded = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (skillBarContainer != null)
            originalSkillBarScale = skillBarContainer.localScale;
        if (playerStatsPanel != null)
            playerStatsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            if (!isExpanded) ExpandUI(true);
            else
            if (isExpanded) ExpandUI(false);
    }

    private void ExpandUI(bool expand)
    {
        isExpanded = expand;

        // 1. 플레이어 상세 정보창 활성화/비활성화
        if (playerStatsPanel != null)
        {
            playerStatsPanel.SetActive(expand);
        }

        // 2. 스킬 바 스케일 변경
        if (skillBarContainer != null)
        {
            // (개선) DOTween이 있다면 부드럽게 스케일링 (예: skillBarContainer.DOScale(targetScale, 0.2f);)
            // 지금은 즉시 변경
            if (expand)
            {
                skillBarContainer.localScale = originalSkillBarScale * expandedScale;
            }
            else
            {
                skillBarContainer.localScale = originalSkillBarScale;
            }
        }

        // 3. (중요) 확장 상태일 때만 스킬 슬롯이 마우스 클릭/드래그 이벤트를 받도록 설정
        // 이 로직은 4단계에서 SkillSlotUI.cs가 구현될 때 추가됩니다.
        // 예: foreach(SkillSlotUI slot in allSkillSlots) { slot.SetManageable(expand); }
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
