using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 시간 표시 UI 시스템
/// 남은 시간, 위기 상황, 유예 시간 표시
/// </summary>
public class TimeDisplayUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI timeText;           // 시간 텍스트 (MM:SS)
    [SerializeField] private Image timeBar;                       // 시간 게이지 바 (옵션)
    [SerializeField] private GameObject criticalWarning;          // 위기 경고 오브젝트
    [SerializeField] private TextMeshProUGUI graceTimeText;      // 유예 시간 텍스트

    [Header("색상 설정")]
    [SerializeField] private Color normalColor = Color.white;     // 정상 색상
    [SerializeField] private Color criticalColor = Color.red;     // 위기 색상
    [SerializeField] private Color graceColor = new Color(1f, 0.5f, 0f); // 유예 색상 (주황)

    [Header("애니메이션 설정")]
    [SerializeField] private float pulseSpeed = 1f;               // 위기 시 깜빡임 속도
    [SerializeField] private bool enablePulseEffect = true;       // 깜빡임 효과 사용 여부

    // 컴포넌트 참조
    private TimeResource timeResource;

    // 애니메이션 상태
    private Tween pulseTween;
    private bool isPulsing = false;

    /// <summary>초기화</summary>
    private void Awake()
    {
        // TimeResource 찾기
        timeResource = FindObjectOfType<TimeResource>();

        if (timeResource == null)
        {
            Debug.LogError("[TimeDisplayUI] TimeResource를 찾을 수 없습니다!");
            return;
        }

        // 이벤트 구독
        timeResource.OnTimeChanged += UpdateTimeDisplay;
        timeResource.OnCriticalTime += OnCriticalTime;
        timeResource.OnGracePeriodStart += OnGracePeriodStart;
        timeResource.OnDeath += OnDeath;
    }

    /// <summary>시작 시 초기화</summary>
    private void Start()
    {
        // 초기 UI 설정
        if (criticalWarning != null)
            criticalWarning.SetActive(false);

        if (graceTimeText != null)
            graceTimeText.gameObject.SetActive(false);

        UpdateTimeDisplay();
    }

    /// <summary>매 프레임 업데이트 (유예 시간 표시)</summary>
    private void Update()
    {
        if (timeResource != null && timeResource.IsInGracePeriod)
        {
            UpdateGraceTimeDisplay();
        }
    }

    /// <summary>정리</summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (timeResource != null)
        {
            timeResource.OnTimeChanged -= UpdateTimeDisplay;
            timeResource.OnCriticalTime -= OnCriticalTime;
            timeResource.OnGracePeriodStart -= OnGracePeriodStart;
            timeResource.OnDeath -= OnDeath;
        }

        // Tween 정리
        pulseTween?.Kill();
    }

    // =============================================================================
    // 시간 표시 업데이트
    // =============================================================================

    /// <summary>
    /// 시간 표시 업데이트
    /// MM:SS 형식으로 표시
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (timeResource == null) return;

        float currentTime = timeResource.CurrentTime;

        // MM:SS 형식으로 변환
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        // 텍스트 업데이트
        if (timeText != null)
        {
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // 위기 상황이 아니면 정상 색상
            if (!timeResource.IsCritical && !timeResource.IsInGracePeriod)
            {
                timeText.color = normalColor;
            }
        }

        // 게이지 바 업데이트 (있다면)
        if (timeBar != null)
        {
            timeBar.fillAmount = timeResource.TimePercent;

            // 위기 상황에 따라 색상 변경
            if (timeResource.IsInGracePeriod)
                timeBar.color = graceColor;
            else if (timeResource.IsCritical)
                timeBar.color = criticalColor;
            else
                timeBar.color = normalColor;
        }
    }

    // =============================================================================
    // 위기 상황 UI
    // =============================================================================

    /// <summary>
    /// 위기 상황 진입 (10% 이하)
    /// </summary>
    private void OnCriticalTime()
    {
        Debug.Log("[TimeDisplayUI] 위기 상황 UI 활성화");

        // 경고 오브젝트 활성화
        if (criticalWarning != null)
            criticalWarning.SetActive(true);

        // 텍스트 색상 변경
        if (timeText != null)
            timeText.color = criticalColor;

        // 깜빡임 효과 시작
        if (enablePulseEffect)
            StartPulseEffect();
    }

    /// <summary>
    /// 깜빡임 효과 시작
    /// </summary>
    private void StartPulseEffect()
    {
        if (isPulsing || timeText == null) return;

        isPulsing = true;

        // DOTween을 이용한 깜빡임
        pulseTween = timeText.DOFade(0.3f, pulseSpeed)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// 깜빡임 효과 중지
    /// </summary>
    private void StopPulseEffect()
    {
        if (!isPulsing) return;

        isPulsing = false;
        pulseTween?.Kill();

        if (timeText != null)
            timeText.alpha = 1f;
    }

    // =============================================================================
    // 유예 시간 UI
    // =============================================================================

    /// <summary>
    /// 유예 시간 시작 (0초 도달)
    /// </summary>
    private void OnGracePeriodStart()
    {
        Debug.Log("[TimeDisplayUI] 유예 시간 UI 활성화");

        // 유예 시간 텍스트 활성화
        if (graceTimeText != null)
        {
            graceTimeText.gameObject.SetActive(true);
            graceTimeText.color = graceColor;
        }

        // 기존 깜빡임 중지
        StopPulseEffect();

        // 위기 경고 비활성화
        if (criticalWarning != null)
            criticalWarning.SetActive(false);

        // 메인 시간 텍스트 숨기기 또는 변경
        if (timeText != null)
        {
            timeText.text = "00:00";
            timeText.color = graceColor;
        }
    }

    /// <summary>
    /// 유예 시간 표시 업데이트
    /// </summary>
    private void UpdateGraceTimeDisplay()
    {
        if (graceTimeText == null || timeResource == null) return;

        // 유예 시간을 계산 (TimeResource에서 직접 가져올 수 없으므로 역계산)
        float graceRemaining = timeResource.CurrentTime; // 임시, TimeResource에 프로퍼티 추가 필요

        graceTimeText.text = $"유예 시간: {graceRemaining:F1}초";

        // 깜빡임 효과
        if (enablePulseEffect && !isPulsing)
        {
            pulseTween = graceTimeText.DOFade(0.3f, pulseSpeed * 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
            isPulsing = true;
        }
    }

    // =============================================================================
    // 죽음 처리
    // =============================================================================

    /// <summary>
    /// 플레이어 사망 시
    /// </summary>
    private void OnDeath()
    {
        Debug.Log("[TimeDisplayUI] 사망 UI 표시");

        // 모든 효과 중지
        StopPulseEffect();

        // UI 비활성화 또는 사망 표시
        if (timeText != null)
        {
            timeText.text = "TIME OUT";
            timeText.color = Color.black;
        }

        if (graceTimeText != null)
            graceTimeText.gameObject.SetActive(false);
    }

    // =============================================================================
    // 유틸리티
    // =============================================================================

    /// <summary>
    /// 강제 UI 갱신 (리스폰 시 등)
    /// </summary>
    public void RefreshUI()
    {
        StopPulseEffect();

        if (criticalWarning != null)
            criticalWarning.SetActive(false);

        if (graceTimeText != null)
            graceTimeText.gameObject.SetActive(false);

        UpdateTimeDisplay();
    }
}
