using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 개별 스킬 슬롯의 UI 상호작용(클릭, 드래그, 드롭, 툴팁)을 담당합니다.
/// Q, E, R 슬롯에 이 스크립트를 추가해야 합니다.
/// </summary>
public class SkillSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IDropHandler, IEndDragHandler
{
    [Header("슬롯 설정")]
    [Tooltip("이 슬롯에 해당하는 KeyCode (예: KeyCode.Q)")]
    [SerializeField] private KeyCode slotKey;

    [Header("UI 참조")]
    [Tooltip("스킬 아이콘을 표시할 Image 컴포넌트")]
    [SerializeField] private Image skillIconImage;
    [Tooltip("쿨다운 오버레이 Image 컴포넌트")]
    [SerializeField] private Image cooldownOverlayImage;
    [Tooltip("비어있을 때 표시할 기본 아이콘 (옵션)")]
    [SerializeField] private Sprite defaultIcon;

    // --- 내부 참조 ---
    private SkillManager skillManager;
    private SkillTooltipUI skillTooltip; // 툴팁 UI 참조
    private SkillData currentSkill;
    private bool isManageable = false; // Ctrl키가 눌린 상태 (관리 가능 상태)

    // --- 정적 변수 (드래그 상태 공유) ---
    private static SkillSlotUI slotBeingDragged;    // 현재 드래그 중인 슬롯
    private static GameObject dragIconInstance;     // 드래그 시 마우스를 따라다니는 임시 아이콘
    private static Canvas rootCanvas;               // 최상위 캔버스 (임시 아이콘을 그리기 위함)

    void Start()
    {
        // SkillManager는 플레이어에게 있으므로 찾아옵니다.
        skillManager = FindFirstObjectByType<SkillManager>();

        // 툴팁 UI를 찾습니다. (씬에 하나만 있다고 가정)
        skillTooltip = FindFirstObjectByType<SkillTooltipUI>(FindObjectsInactive.Include);

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        UpdateDisplay(null); // 초기 상태는 비어있음
    }

    /// <summary>
    /// CombatUIManager가 호출: Ctrl키 상태에 따라 관리 가능 여부 설정
    /// </summary>
    public void SetManageable(bool manageable)
    {
        isManageable = manageable;
        if (!isManageable)
        {
            // 관리 모드가 아닐 때 툴팁 강제 숨김
            skillTooltip?.Hide();
        }
    }

    /// <summary>
    /// SkillManager가 호출: 이 슬롯의 UI를 갱신
    /// </summary>
    public void UpdateDisplay(SkillData skillData)
    {
        currentSkill = skillData;
        if (currentSkill != null)
        {
            skillIconImage.sprite = currentSkill.skillIcon;
            skillIconImage.color = Color.white;
            skillIconImage.enabled = true;
        }
        else
        {
            skillIconImage.sprite = defaultIcon;
            skillIconImage.color = new Color(1, 1, 1, 0.5f); // 비어있으면 반투명
            skillIconImage.enabled = (defaultIcon != null);
        }
    }

    // --- 1. 툴팁 (사진 3) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isManageable && currentSkill != null && skillTooltip != null)
        {
            skillTooltip.Show(currentSkill);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (skillTooltip != null)
        {
            skillTooltip.Hide();
        }
    }

    // --- 2. 장착 해제 (사진 3, 좌클릭) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그가 끝났을 때도 Click 이벤트가 호출될 수 있으므로, 드래그 중이 아닐 때만
        if (isManageable && currentSkill != null && slotBeingDragged == null && eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"[SkillSlotUI] {slotKey} 슬롯 클릭: 스킬 버리기");
            skillTooltip?.Hide();
            skillManager.UnequipSkill(slotKey);
        }
    }

    // --- 3. 드래그 앤 드롭 (사진 4) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 관리 모드이고, 빈 슬롯이 아니고, 다른 드래그가 없을 때만
        if (isManageable && currentSkill != null && slotBeingDragged == null)
        {
            // 드래그 시작
            slotBeingDragged = this;
            skillTooltip?.Hide();

            // 마우스를 따라다닐 임시 아이콘 생성
            dragIconInstance = new GameObject("DragIcon");
            dragIconInstance.transform.SetParent(rootCanvas.transform, false);
            dragIconInstance.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 80); // 아이콘 크기
            Image img = dragIconInstance.AddComponent<Image>();
            img.sprite = currentSkill.skillIcon;
            img.raycastTarget = false; // 이 아이콘이 마우스 이벤트를 막지 않도록 함

            dragIconInstance.transform.position = eventData.position; // 마우스 위치로
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconInstance != null)
        {
            // 임시 아이콘을 마우스 위치로 계속 이동
            dragIconInstance.transform.position = eventData.position;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 관리 모드이고, 이 슬롯이 드래그 중인 슬롯이 아니며, 다른 슬롯에서 드래그 중일 때
        if (isManageable && slotBeingDragged != null && slotBeingDragged != this)
        {
            Debug.Log($"[SkillSlotUI] {slotBeingDragged.slotKey} -> {this.slotKey} 스킬 스왑");

            // SkillManager에 스킬 교체 요청
            skillManager.SwapSkills(slotBeingDragged.slotKey, this.slotKey);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (slotBeingDragged == this) // 내가 시작한 드래그가 끝났을 때
        {
            // 1. 마우스를 따라다니던 임시 아이콘 파괴
            if (dragIconInstance != null)
            {
                Destroy(dragIconInstance);
            }

            // 2. 드롭 대상 확인
            // eventData.pointerEnter는 마우스 포인터가 마지막으로 올라가 있던 오브젝트
            SkillSlotUI dropTargetSlot = (eventData.pointerEnter != null) ? eventData.pointerEnter.GetComponent<SkillSlotUI>() : null;

            if (dropTargetSlot == null || dropTargetSlot == this)
            {
                // UI 바깥(땅)이나 자기 자신에게 드롭 -> 스킬 버리기 (사진 4)
                Debug.Log($"[SkillSlotUI] {slotKey} 슬롯 드래그: UI 바깥에 버리기");
                skillManager.UnequipSkill(slotKey);
            }
            // (다른 슬롯에 드롭된 경우는 OnDrop에서 이미 처리됨)

            // 3. 정적 변수 리셋
            slotBeingDragged = null;
            dragIconInstance = null;
        }
    }

    // KeyCode 반환 (CombatUIManager에서 사용)
    public KeyCode GetSlotKey()
    {
        return slotKey;
    }

    public Image GetCooldownOverlayImage()
    {
        return cooldownOverlayImage;
    }
}
