using System.Collections;
using System.Collections.Generic; // [수정] Dictionary 사용
using UnityEngine;
using UnityEngine.UI;

// 전투 UI를 관리
public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance;

    // [수정] 기존 배열은 SkillSlotUI 배열로 대체됩니다.
    [Header("Skill Slots (Draggable)")]
    [Tooltip("드래그 가능한 Q, E, R 슬롯의 SkillSlotUI 스크립트 배열")]
    [SerializeField] private SkillSlotUI[] manageableSkillSlots; // Q, E, R 슬롯

    [Header("Skill Slots (Fixed)")]
    [Tooltip("고정된 T, LMB, RMB 슬롯의 아이콘 Image")]
    [SerializeField] private Image tSlotIcon;
    [SerializeField] private Image lmbSlotIcon;
    [SerializeField] private Image rmbSlotIcon;
    // TODO: 고정 슬롯의 쿨다운 오버레이도 필요하면 여기에 추가

    // 쿨다운 오버레이 이미지를 KeyCode로 빠르게 찾기 위한 Dictionary
    private Dictionary<KeyCode, Image> skillCooldownOverlays = new Dictionary<KeyCode, Image>();


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

        // [수정] 쿨다운 딕셔너리 초기화
        InitializeCooldownDictionary();
    }

    /// <summary>
    /// [신규] 쿨다운 딕셔너리를 초기화합니다.
    /// </summary>
    private void InitializeCooldownDictionary()
    {
        skillCooldownOverlays.Clear();
        if (manageableSkillSlots != null)
        {
            foreach (var slot in manageableSkillSlots)
            {
                // SkillSlotUI에서 Key와 CooldownImage를 가져와 딕셔너리에 등록
                if (slot != null && slot.GetCooldownOverlayImage() != null)
                {
                    skillCooldownOverlays[slot.GetSlotKey()] = slot.GetCooldownOverlayImage();
                }
            }
        }
        // TODO: T, LMB, RMB 슬롯의 쿨다운 오버레이도 딕셔너리에 추가
    }


    private void Update()
    {
        // [수정] Ctrl 키 로직을 더 명확하게
        bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (isCtrlPressed && !isExpanded)
        {
            ExpandUI(true);
        }
        else if (!isCtrlPressed && isExpanded)
        {
            ExpandUI(false);
        }
    }

    /// <summary>
    /// [수정] UI 확장 또는 축소 및 SkillSlotUI에 상태 전파
    /// </summary>
    /// <param name="expand">true: 확장, false: 축소</param>
    public void ExpandUI(bool expand) // [수정] public으로 변경 (SkillManager가 호출)
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
            if (expand)
            {
                skillBarContainer.localScale = originalSkillBarScale * expandedScale;
            }
            else
            {
                skillBarContainer.localScale = originalSkillBarScale;
            }
        }

        // 3. [수정] 확장 상태일 때만 Q,E,R 슬롯이 마우스 이벤트를 받도록 설정
        if (manageableSkillSlots != null)
        {
            foreach (SkillSlotUI slot in manageableSkillSlots)
            {
                if (slot != null) slot.SetManageable(expand);
            }
        }
    }

    /// <summary>
    /// (사용 안 함) SetSkillIcon은 UpdateSkillSlot으로 대체되었습니다.
    /// </summary>
    public void SetSkillIcon(int index, Sprite icon)
    {
        Debug.LogWarning("SetSkillIcon(int) is deprecated. Use SkillManager's functions instead.");
    }

    /// <summary>
    /// [신규] SkillManager가 호출하여 특정 슬롯의 UI를 갱신
    /// </summary>
    public void UpdateSkillSlot(KeyCode key, SkillData skillData)
    {
        SkillSlotUI targetSlot = null;

        // 1. 관리 가능한 슬롯(Q,E,R)에서 찾기
        if (manageableSkillSlots != null)
        {
            foreach (SkillSlotUI slot in manageableSkillSlots)
            {
                if (slot != null && slot.GetSlotKey() == key)
                {
                    targetSlot = slot;
                    break;
                }
            }
        }

        if (targetSlot != null)
        {
            // SkillSlotUI의 UpdateDisplay 함수 호출
            targetSlot.UpdateDisplay(skillData);
            return;
        }

        // 2. 관리 불가능한 고정 슬롯(T, LMB, RMB)에서 찾기 (아이콘만 교체)
        Image targetIcon = null;
        if (key == KeyCode.T) targetIcon = tSlotIcon;
        else if (key == KeyCode.Mouse0) targetIcon = lmbSlotIcon; // KeyCode.Mouse0 = 좌클릭
        else if (key == KeyCode.Mouse1) targetIcon = rmbSlotIcon; // KeyCode.Mouse1 = 우클릭

        if (targetIcon != null)
        {
            if (skillData != null)
            {
                targetIcon.sprite = skillData.skillIcon;
                targetIcon.color = Color.white;
                targetIcon.enabled = true;
            }
            else
            {
                targetIcon.sprite = null; // TODO: 고정 슬롯용 기본 아이콘
                targetIcon.color = new Color(1, 1, 1, 0.5f);
                targetIcon.enabled = (targetIcon.sprite != null); // 기본 아이콘이 없으면 비활성화
            }
        }
    }


    /// <summary>
    /// [신규] 쿨다운 UI를 KeyCode 기반으로 시작
    /// </summary>
    /// <param name="key">스킬 키 (KeyCode.Q 등)</param>
    /// <param name="duration">쿨다운 시간</param>
    public void StartSkillCoolDown(KeyCode key, float duration)
    {
        if (skillCooldownOverlays.TryGetValue(key, out Image overlay))
        {
            if (overlay != null)
            {
                StartCoroutine(CooldownRoutine(overlay, duration));
            }
        }
    }

    /// <summary>
    /// (기존 함수 수정) 인덱스 기반 쿨다운은 KeyCode 기반으로 변경
    /// </summary>
    public void StartSkillCoolDown(int index, float duration)
    {
        Debug.LogWarning("StartSkillCoolDown(int index)는 호환성용입니다. KeyCode 기반 함수를 사용하세요.");
        // 호환성을 위해 임시로 index 0,1,2를 Q,E,R로 매핑
        if (index == 0) StartSkillCoolDown(KeyCode.Q, duration);
        else if (index == 1) StartSkillCoolDown(KeyCode.E, duration);
        else if (index == 2) StartSkillCoolDown(KeyCode.R, duration);
    }


    IEnumerator CooldownRoutine(Image overlay, float duration)
    {
        overlay.fillAmount = 1f; // 시작 시 오버레이 가득 찬 상태
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // 일시 정지 중에도 정상적으로 작동하게
            overlay.fillAmount = 1f - (time / duration);
            yield return null;
        }
        overlay.fillAmount = 0f;
    }

    public void SetSwordIcon(Sprite icon)
    {
        if (swordIcon != null)
            swordIcon.sprite = icon; // 검술 아이콘을 새롭게 지정한 Icon으로 변경하는거
    }

    public void SetSwordShineEffect(bool active)
    {
        if (swordShineEffect != null)
            swordShineEffect.SetBool("isShining", active);
    }

    public void SetItemIcon(int index, Sprite icon)
    {
        if (index >= 0 && index < itemIcons.Length)
            itemIcons[index].sprite = icon;
    }
}
