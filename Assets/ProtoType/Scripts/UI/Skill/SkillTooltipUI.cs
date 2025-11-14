using UnityEngine;
using TMPro; // TextMeshPro 사용

/// <summary>
/// 스킬 툴팁 UI를 관리합니다. 씬에 이 스크립트를 가진 UI 패널이 하나 있어야 합니다.
/// </summary>
public class SkillTooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TextMeshProUGUI skillInfoText; // 쿨타임, 등급 등
    [SerializeField] private TextMeshProUGUI unequipText; // "좌클릭 시 장착 해제"

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        gameObject.SetActive(false); // 처음엔 숨김
    }

    /// <summary>
    /// 툴팁을 표시합니다.
    /// </summary>
    public void Show(SkillData data)
    {
        if (data == null) return;

        skillNameText.text = data.skillName;
        skillDescriptionText.text = data.description;
        skillInfoText.text = $"[ {data.grade} | 쿨타임: {data.cooldown}초 ]";

        // T(고유스킬) 슬롯 등은 장착 해제 문구를 숨길 수 있음 (추후 SkillSlotUI에서 키를 받아 분기)
        unequipText.text = "좌클릭 시 장착 해제";

        gameObject.SetActive(true);

        // 툴팁 위치를 마우스 커서 옆으로 이동
        UpdatePosition();
    }

    /// <summary>
    /// 툴팁을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        // 툴팁이 활성화되어 있으면 마우스 위치를 따라다님
        if (gameObject.activeSelf)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 툴팁 위치를 마우스 커서 기준으로 조정 (화면 밖으로 나가지 않게)
    /// </summary>
    private void UpdatePosition()
    {
        Vector2 mousePos = Input.mousePosition;

        // 캔버스 크기
        float canvasWidth = rootCanvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = rootCanvas.GetComponent<RectTransform>().rect.height;

        // 툴팁 크기
        float tooltipWidth = rectTransform.rect.width;
        float tooltipHeight = rectTransform.rect.height;

        // 기본 위치 (마우스 우측 상단)
        Vector2 newPos = mousePos + new Vector2(10, 10);

        // 화면 오른쪽 경계 체크
        if (newPos.x + tooltipWidth > canvasWidth)
        {
            newPos.x = mousePos.x - tooltipWidth - 10;
        }

        // 화면 위쪽 경계 체크
        if (newPos.y + tooltipHeight > canvasHeight)
        {
            newPos.y = mousePos.y - tooltipHeight - 10;
        }

        transform.position = newPos;
    }
}
