using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// 검술 전환 UI 컨트롤러
/// PlayerSwordStyleManager와 연동
/// </summary>
public class SwordStyleUIController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiRoot; // SwordStyleUI 루트 obj
    public TMP_Text toolTipText; // 툴팁 텍스트 ( ~~ 검술 ) 이런식

    [Header("검술 버튼")]
    public GameObject buttonPrefab;
    public Transform buttonParent;

    [Header("검술 종류")]
    public List<SwordStyle> avaliableStyles;

    [Header("Player References")]
    public PlayerSwordStyleManager swordStyleManager;
    public PlayerController playerController;

    bool isActive = false;

    public float tKeyCoolDown = 5f;
    float tKeyTimer = 0f;
    bool canToggle = true;

    void Start()
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);

        if (toolTipText != null)
            toolTipText.gameObject.SetActive(false);

        // 자동 참조 설정
        if (swordStyleManager == null)
            swordStyleManager = FindFirstObjectByType<PlayerSwordStyleManager>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        GenerateButtons();
    }

    void Update()
    {
        // T키 누르면 열고 닫기 작동
        if (Input.GetKeyDown(KeyCode.T) && canToggle)
            ToggleUI();

        if (!canToggle)
        {
            tKeyTimer += Time.unscaledDeltaTime;
            if (tKeyTimer > tKeyCoolDown)
            {
                canToggle = true;
                tKeyTimer = 0f;
            }
        }

        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSwordByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSwordByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSwordByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSwordByIndex(3);
    }

    void SelectSwordByIndex(int index)
    {
        if (index >= 0 && index < avaliableStyles.Count)
        {
            SelectSwordStyle(avaliableStyles[index]);
            ToggleUI();
        }
    }

    public void ToggleUI()
    {
        isActive = !isActive;

        if (uiRoot != null)
            uiRoot.SetActive(isActive);

        if (isActive)
        {
            Time.timeScale = 0.2f;
            Time.fixedDeltaTime = 0.02f * Time.deltaTime;

            playerController?.EnterSwitchMode();

            CombatUIManager.Instance?.SetSwordShineEffect(true);
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            playerController?.ExitSwitchMode();

            CombatUIManager.Instance?.SetSwordShineEffect(false);

            // 쿨타임 시작
            canToggle = false;
            tKeyTimer = 0f;
        }

        // UI 닫을 때 툴팁도 자동으로 끄기
        if (!isActive && toolTipText != null)
            toolTipText.gameObject.SetActive(false);
    }

    void GenerateButtons()
    {
        Debug.Log($"검술 개수: {avaliableStyles.Count}");

        foreach (Transform child in buttonParent)
            Destroy(child.gameObject);

        foreach (SwordStyle style in avaliableStyles)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonParent);
            SwordButton sb = btn.GetComponent<SwordButton>();
            if (sb != null)
            {
                sb.style = style;
                sb.controller = this;
            }

            Image icon = btn.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && style.iconSprite != null)
            {
                icon.sprite = style.iconSprite;
            }
        }
    }

    public void ShowToolTip(string text)
    {
        if (toolTipText != null)
        {
            toolTipText.text = text;
            toolTipText.gameObject.SetActive(true);
        }
    }

    public void HideToolTip()
    {
        if (toolTipText != null)
            toolTipText.gameObject.SetActive(false);
    }

    public void SelectSwordStyle(SwordStyle style)
    {
        if (swordStyleManager != null)
        {
            // PlayerSwordStyleManager의 ChangeStyle 메서드 호출
            // 이 메서드가 애니메이터, UI 업데이트, 쿨다운 등을 모두 처리함
            swordStyleManager.ChangeStyle(style);

            Debug.Log($"[SwordStyleUI] 검술 변경: {style.styleName}");
        }
        else
        {
            Debug.LogError("[SwordStyleUI] PlayerSwordStyleManager를 찾을 수 없습니다!");
        }
    }
}
