using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static RewardData;

/// <summary>
/// 스테이지 클리어 시 보상 선택 UI를 관리하는 시스템
/// 게임 일시정지, 카드 3개 표시, 선택 처리 등을 담당
/// </summary>
public class RewardUIManager : MonoBehaviour
{
    [Header("UI 루트 오브젝트")]
    [SerializeField, Tooltip("보상 선택 UI 전체 패널")]
    GameObject rewardUIRoot;

    [SerializeField, Tooltip("조금 어두운 오버레이")]
    GameObject darkOverlay;

    [Header("카드 UI 요소들")]
    [SerializeField, Tooltip("3개의 보상 카드 오브젝트")]
    RewardCard[] rewardCards = new RewardCard[3];

    [SerializeField, Tooltip("CHOOSE 텍스트")]
    TextMeshProUGUI titleText;

    [Header("시스템 참조")]
    [SerializeField, Tooltip("보상 선택 시스템")]
    private WeightedRandomSelector rewardSelector;

    [SerializeField, Tooltip("플레이어 스탯 관리자")]
    private PlayerStatsManager statsManager;

    [Header("UI 설정")]
    [SerializeField, Tooltip("카드 선택 후 대기 시간")]
    float selectionDelay = 1f;

    [SerializeField, Tooltip("UI 페이드 속도")]
    float fadeSpeed = 2f;

    // 현재 표시된 보상들
    private RewardData[] currentRewards;

    // UI 상태 관리
    private bool isUIActive = false;
    private bool isSelectionLocked = false;

    // 선택 완료 콜백 (StageManager에서 설정)
    private System.Action onRewardSelected;

    // 초기화 및 UI 비활성화
    private void Awake()
    {
        // 시작 시 UI 숨김
        if (rewardUIRoot != null)
            rewardUIRoot.SetActive(false);

        // 자동 참조 설정
        if (rewardSelector == null)
            rewardSelector = FindFirstObjectByType<WeightedRandomSelector>();

        if (statsManager == null)
            statsManager = PlayerStatsManager.Instance;

        // 카드 클릭 이벤트 연결
        SetupCardClickEvents();
    }

    /// <summary>
    /// 카드 클릭 이벤트 설정
    /// </summary>
    private void SetupCardClickEvents()
    {
        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] != null && rewardCards[i].cardButton != null)
            {
                int cardIndex = i; // 클로저 문제 해결
                rewardCards[i].cardButton.onClick.AddListener(() => OnCardSelected(cardIndex));
            }
        }
    }

    /// <summary>
    /// 보상 선택 UI 표시 - 메인 인터페이스
    /// StageManager에서 스테이지 클리어 시 호출
    /// </summary>
    /// <param name="onSelectionComplete">선택 완료 시 호출할 콜백</param>
    public void ShowRewardSelection(System.Action onSelectionComplete = null)
    {
        if (isUIActive)
        {
            Debug.LogWarning("[RewardUIManager] 이미 보상 UI가 활성화되어 있습니다!");
            return;
        }

        Debug.Log("[RewardUIManager] 보상 선택 UI 표시");

        // 콜백 저장
        onRewardSelected = onSelectionComplete;

        // 게임 일시정지
        PauseGame();

        // 보상 3개 선택
        GenerateRewards();

        // UI 표시
        StartCoroutine(ShowUIWithFade());
    }

    /// <summary>
    /// 게임 일시정지
    /// </summary>
    private void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("[RewardUIManager] 게임 일시정지");
    }

    /// <summary>
    /// 게임 재개
    /// </summary>
    private void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[RewardUIManager] 게임 재개");
    }

    /// <summary>
    /// 보상 3개 생성
    /// </summary>
    private void GenerateRewards()
    {
        if (rewardSelector == null)
        {
            Debug.LogError("[RewardUIManager] WeightedRandomSelector가 없습니다!");
            return;
        }

        // 확률 기반으로 3개 보상 선택
        currentRewards = rewardSelector.SelectRewards();

        if (currentRewards.Length < 3)
        {
            Debug.LogError($"[RewardUIManager] 보상이 3개 미만입니다! ({currentRewards.Length}개)");
        }

        Debug.Log($"[RewardUIManager] {currentRewards.Length}개 보상 생성 완료");
    }

    /// <summary>
    /// UI를 페이드 인 효과와 함께 표시
    /// </summary>
    private IEnumerator ShowUIWithFade()
    {
        isUIActive = true;
        isSelectionLocked = false;

        // UI 활성화
        if (rewardUIRoot != null)
            rewardUIRoot.SetActive(true);

        // 카드 데이터 설정
        SetupRewardCards();

        // 페이드 인 효과 (간단한 알파 조절)
        yield return StartCoroutine(FadeUI(0f, 1f));

        Debug.Log("[RewardUIManager] 보상 선택 UI 표시 완료");
    }

    /// <summary>
    /// 보상 카드들에 데이터 설정
    /// </summary>
    private void SetupRewardCards()
    {
        for (int i = 0; i < rewardCards.Length && i < currentRewards.Length; i++)
        {
            if (rewardCards[i] != null && currentRewards[i] != null)
            {
                rewardCards[i].SetupCard(currentRewards[i]);
            }
        }

        // 타이틀 설정
        if (titleText != null)
            titleText.text = "CHOOSE";
    }

    /// <summary>
    /// UI 페이드 효과
    /// </summary>
    /// <param name="startAlpha">시작 알파값</param>
    /// <param name="endAlpha">끝 알파값</param>
    private IEnumerator FadeUI(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        CanvasGroup canvasGroup = rewardUIRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = rewardUIRoot.AddComponent<CanvasGroup>();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지 중에도 동작
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// 카드 선택 처리
    /// </summary>
    /// <param name="cardIndex">선택된 카드 인덱스 (0~2)</param>
    private void OnCardSelected(int cardIndex)
    {
        if (isSelectionLocked)
        {
            Debug.Log("[RewardUIManager] 선택이 잠겨있습니다!");
            return;
        }

        if (cardIndex < 0 || cardIndex >= currentRewards.Length)
        {
            Debug.LogError($"[RewardUIManager] 잘못된 카드 인덱스: {cardIndex}");
            return;
        }

        RewardData selectedReward = currentRewards[cardIndex];
        if (selectedReward == null)
        {
            Debug.LogError("[RewardUIManager] 선택된 보상이 null입니다!");
            return;
        }

        Debug.Log($"[RewardUIManager] 카드 {cardIndex + 1} 선택: {selectedReward.rewardName}");

        // 선택 잠금
        isSelectionLocked = true;

        // 선택된 카드 하이라이트
        HighlightSelectedCard(cardIndex);

        // 보상 적용
        ApplySelectedReward(selectedReward);

        // 잠시 대기 후 UI 닫기
        StartCoroutine(CloseUIAfterDelay());
    }

    /// <summary>
    /// 선택된 카드 하이라이트 효과
    /// </summary>
    private void HighlightSelectedCard(int selectedIndex)
    {
        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] != null)
            {
                if (i == selectedIndex)
                {
                    // 선택된 카드: 밝게
                    rewardCards[i].SetCardState(CardState.Selected);
                }
                else
                {
                    // 나머지 카드: 어둡게
                    rewardCards[i].SetCardState(CardState.Dimmed);
                }
            }
        }
    }

    /// <summary>
    /// 선택된 보상을 플레이어에게 적용
    /// </summary>
    private void ApplySelectedReward(RewardData reward)
    {
        if (statsManager != null)
        {
            statsManager.ApplyReward(reward);
            Debug.Log($"[RewardUIManager] 보상 적용 완료: {reward.GetDisplayText()}");
        }
        else
        {
            Debug.LogError("[RewardUIManager] PlayerStatsManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 대기 후 UI 닫기
    /// </summary>
    private IEnumerator CloseUIAfterDelay()
    {
        // 선택 대기 시간
        yield return new WaitForSecondsRealtime(selectionDelay);

        // UI 닫기
        yield return StartCoroutine(HideUIWithFade());

        // 게임 재개
        ResumeGame();

        // 완료 콜백 호출
        onRewardSelected?.Invoke();

        Debug.Log("[RewardUIManager] 보상 선택 완료");
    }

    /// <summary>
    /// UI를 페이드 아웃 효과와 함께 숨김
    /// </summary>
    private IEnumerator HideUIWithFade()
    {
        // 페이드 아웃
        yield return StartCoroutine(FadeUI(1f, 0f));

        // UI 비활성화
        if (rewardUIRoot != null)
            rewardUIRoot.SetActive(false);

        isUIActive = false;
        isSelectionLocked = false;
        currentRewards = null;
    }

    /// <summary>
    /// 강제로 UI 닫기 (ESC 키 등)
    /// </summary>
    public void ForceCloseUI()
    {
        if (!isUIActive) return;

        Debug.Log("[RewardUIManager] 보상 UI 강제 닫기");

        StopAllCoroutines();

        if (rewardUIRoot != null)
            rewardUIRoot.SetActive(false);

        ResumeGame();

        isUIActive = false;
        isSelectionLocked = false;
        currentRewards = null;
    }

    /// <summary>
    /// ESC 키로 UI 닫기 (개발용)
    /// </summary>
    private void Update()
    {
        if (isUIActive && Input.GetKeyDown(KeyCode.Escape))
        {
            ForceCloseUI();
        }
    }

    // =============================================================================
    // 공개 인터페이스
    // =============================================================================

    /// <summary>
    /// 현재 UI가 활성화되어 있는지 확인
    /// </summary>
    public bool IsUIActive => isUIActive;

    /// <summary>
    /// 테스트용: 즉시 보상 UI 표시
    /// </summary>
    [ContextMenu("테스트 보상 UI 표시")]
    public void TestShowRewardUI()
    {
        ShowRewardSelection(() => {
            Debug.Log("테스트 보상 선택 완료!");
        });
    }

}

// =============================================================================
// RewardCard.cs - 개별 보상 카드 UI 컴포넌트
// =============================================================================

/// <summary>
/// 개별 보상 카드를 관리하는 컴포넌트
/// 카드 하나의 UI 요소들을 담당
/// </summary>
[System.Serializable]
public class RewardCard
{
    [Header("카드 UI 요소들")]
    [Tooltip("카드 전체 버튼")]
    public Button cardButton;

    [Tooltip("등급 표시 텍스트 (>Stats, >Combat 등)")]
    public TextMeshProUGUI categoryText;

    [Tooltip("보상 이름 텍스트")]
    public TextMeshProUGUI nameText;

    [Tooltip("보상 설명 텍스트")]
    public TextMeshProUGUI descriptionText;

    [Tooltip("수치 표시 텍스트 (Attack Power +5% 등)")]
    public TextMeshProUGUI valueText;

    [Tooltip("보상 아이콘 이미지")]
    public Image iconImage;

    [Tooltip("카드 배경 이미지")]
    public Image cardBackground;

    /// <summary>
    /// 카드에 보상 데이터 설정
    /// </summary>
    /// <param name="rewardData">표시할 보상 데이터</param>
    public void SetupCard(RewardData rewardData)
    {
        if (rewardData == null)
        {
            Debug.LogError("[RewardCard] 보상 데이터가 null입니다!");
            return;
        }

        // 카테고리 텍스트 설정
        if (categoryText != null)
        {
            categoryText.text = rewardData.category.GetCategoryPrefix();
            categoryText.color = rewardData.rarity.GetColor();
        }

        // 보상 이름 설정
        if (nameText != null)
        {
            nameText.text = rewardData.rewardName;
        }

        // 보상 설명 설정
        if (descriptionText != null)
        {
            descriptionText.text = rewardData.description;
        }

        // 수치 표시 설정
        if (valueText != null)
        {
            if(rewardData.rewardType == RewardType.MultiStat)
            {
                // 복합 스탯이면 상세 텍스트 표시
                valueText.text = rewardData.GetDetailedDisplayText();
            }
            else
            {
                // 단일 스탯이면 기존 방식대로
                valueText.text = rewardData.GetDisplayText();
            }
            valueText.color = rewardData.rarity.GetColor();
        }

        // 아이콘 설정
        if (iconImage != null && rewardData.iconSprite != null)
        {
            iconImage.sprite = rewardData.iconSprite;
            iconImage.color = Color.white;
        }

        // 카드 배경 색상 설정 (등급별)
        if (cardBackground != null)
        {
            cardBackground.color = rewardData.rarity.GetDarkColor();
        }

        // 카드 상태 초기화
        SetCardState(CardState.Normal);
    }

    /// <summary>
    /// 카드 상태 변경 (선택됨, 비활성화 등)
    /// </summary>
    /// <param name="state">변경할 카드 상태</param>
    public void SetCardState(CardState state)
    {
        float alpha = 1f;
        bool interactable = true;

        switch (state)
        {
            case CardState.Normal:
                alpha = 1f;
                interactable = true;
                break;
            case CardState.Selected:
                alpha = 1f;
                interactable = false;
                // 선택된 카드는 밝게 유지
                break;
            case CardState.Dimmed:
                alpha = 0.5f;
                interactable = false;
                break;
        }

        // 버튼 상호작용 설정
        if (cardButton != null)
        {
            cardButton.interactable = interactable;
        }

        // 전체 카드 알파값 조절
        SetCardAlpha(alpha);
    }

    /// <summary>
    /// 카드 알파값 설정
    /// </summary>
    private void SetCardAlpha(float alpha)
    {
        if (cardBackground != null)
        {
            Color bgColor = cardBackground.color;
            bgColor.a = alpha;
            cardBackground.color = bgColor;
        }

        // 모든 텍스트 알파값 조절
        SetTextAlpha(categoryText, alpha);
        SetTextAlpha(nameText, alpha);
        SetTextAlpha(descriptionText, alpha);
        SetTextAlpha(valueText, alpha);

        if (iconImage != null)
        {
            Color iconColor = iconImage.color;
            iconColor.a = alpha;
            iconImage.color = iconColor;
        }
    }

    /// <summary>
    /// 텍스트 알파값 설정
    /// </summary>
    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null)
        {
            Color textColor = text.color;
            textColor.a = alpha;
            text.color = textColor;
        }
    }
}

/// <summary>
/// 카드 상태 열거형
/// </summary>
public enum CardState
{
    Normal,     // 일반 상태
    Selected,   // 선택된 상태
    Dimmed      // 비활성화된 상태
}