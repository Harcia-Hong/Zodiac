using UnityEngine;
using System;

/// <summary>
/// 시간 자원 관리 시스템
/// HP 대신 시간을 생명 자원으로 사용
/// </summary>
public class TimeResource : MonoBehaviour
{
    [Header("시간 설정")]
    [SerializeField] private float maxTime = 120f;           // 최대 시간 (초)
    [SerializeField] private float currentTime = 120f;       // 현재 시간 (초)

    [Header("유예 시간 설정")]
    [SerializeField] private float graceTime = 5f;           // 0초 도달 시 유예 시간
    [SerializeField] private bool isInGracePeriod = false;   // 유예 시간 중인지
    [SerializeField] private float graceTimeRemaining = 0f;  // 남은 유예 시간

    [Header("위기 상황 설정")]
    [SerializeField, Range(0f, 1f)] private float criticalThreshold = 0.1f; // 위기 임계값 (10%)

    // 이벤트
    public event Action OnTimeChanged;              // 시간 변경 시
    public event Action OnTimeRecovered;            // 시간 회복 시
    public event Action OnTimeConsumed;             // 시간 소모 시
    public event Action OnCriticalTime;             // 위기 상황 진입 시
    public event Action OnGracePeriodStart;         // 유예 시간 시작 시
    public event Action OnDeath;                    // 죽음 시

    // 상태 플래그
    private bool isDead = false;
    private bool isCritical = false;
    private bool hasUsedGrace = false;              // 유예 시간 이미 사용했는지

    // 프로퍼티
    public float MaxTime => maxTime;
    public float CurrentTime => currentTime;
    public float TimePercent => maxTime > 0 ? currentTime / maxTime : 0f;
    public bool IsCritical => isCritical;
    public bool IsInGracePeriod => isInGracePeriod;
    public bool IsDead => isDead;

    /// <summary>초기화</summary>
    private void Awake()
    {
        currentTime = maxTime;
        isDead = false;
        isCritical = false;
        hasUsedGrace = false;
    }

    /// <summary>매 프레임 업데이트</summary>
    private void Update()
    {
        // 유예 시간 카운트다운
        if (isInGracePeriod)
        {
            UpdateGracePeriod();
        }

        // 위기 상황 체크
        CheckCriticalState();
    }

    // =============================================================================
    // 시간 소모
    // =============================================================================

    /// <summary>
    /// 시간 소모
    /// </summary>
    /// <param name="amount">소모할 시간 (양수)</param>
    /// <returns>실제 소모된 시간</returns>
    public float ConsumeTime(float amount)
    {
        if (isDead || amount <= 0) return 0f;

        float actualConsumed = Mathf.Min(amount, currentTime);
        currentTime -= actualConsumed;
        currentTime = Mathf.Max(0f, currentTime);

        OnTimeConsumed?.Invoke();
        OnTimeChanged?.Invoke();

        // 0초 도달 시 유예 시간 시작
        if (currentTime <= 0f && !hasUsedGrace)
        {
            StartGracePeriod();
        }

        return actualConsumed;
    }

    // =============================================================================
    // 시간 회복
    // =============================================================================

    /// <summary>
    /// 시간 복구 (현재 시간만 회복, 최대치 초과 불가)
    /// </summary>
    /// <param name="amount">회복할 시간 (양수)</param>
    /// <returns>실제 회복된 시간</returns>
    public float RecoverTime(float amount)
    {
        if (isDead || amount <= 0) return 0f;

        float beforeRecover = currentTime;
        currentTime += amount;
        currentTime = Mathf.Min(currentTime, maxTime); // 최대치 제한

        float actualRecovered = currentTime - beforeRecover;

        OnTimeRecovered?.Invoke();
        OnTimeChanged?.Invoke();

        // 유예 시간 중 회복 시 유예 해제
        if (isInGracePeriod && currentTime > 0f)
        {
            ExitGracePeriod();
        }

        return actualRecovered;
    }

    /// <summary>
    /// 영구 증가 (최대 시간 증가 + 현재 시간도 증가분만큼 회복)
    /// </summary>
    /// <param name="amount">증가할 시간 (양수)</param>
    public void IncreaseMaxTime(float amount)
    {
        if (amount <= 0) return;

        maxTime += amount;
        currentTime += amount; // 증가분만큼 현재 시간도 회복

        OnTimeRecovered?.Invoke();
        OnTimeChanged?.Invoke();

        Debug.Log($"[TimeResource] 최대 시간 영구 증가: +{amount}초 (최대: {maxTime}초, 현재: {currentTime}초)");
    }

    // =============================================================================
    // 유예 시간 시스템
    // =============================================================================

    /// <summary>유예 시간 시작 (1회성)</summary>
    private void StartGracePeriod()
    {
        if (hasUsedGrace) return;

        isInGracePeriod = true;
        hasUsedGrace = true;
        graceTimeRemaining = graceTime;

        OnGracePeriodStart?.Invoke();

        Debug.Log($"[TimeResource] 유예 시간 시작! {graceTime}초 안에 시간을 회복하세요!");
    }

    /// <summary>유예 시간 업데이트</summary>
    private void UpdateGracePeriod()
    {
        graceTimeRemaining -= Time.deltaTime;

        if (graceTimeRemaining <= 0f)
        {
            Die();
        }
    }

    /// <summary>유예 시간 해제 (시간 회복 시)</summary>
    private void ExitGracePeriod()
    {
        isInGracePeriod = false;
        graceTimeRemaining = 0f;

        Debug.Log($"[TimeResource] 유예 시간 탈출 성공!");
    }

    // =============================================================================
    // 위기 상황 체크
    // =============================================================================

    /// <summary>위기 상황 체크 (시간이 10% 이하)</summary>
    private void CheckCriticalState()
    {
        bool shouldBeCritical = TimePercent <= criticalThreshold && !isDead;

        // 위기 상황 진입
        if (shouldBeCritical && !isCritical)
        {
            isCritical = true;
            OnCriticalTime?.Invoke();
            Debug.Log($"[TimeResource] 위기! 시간이 {criticalThreshold * 100}% 이하입니다!");
        }
        // 위기 상황 해제
        else if (!shouldBeCritical && isCritical)
        {
            isCritical = false;
        }
    }

    // =============================================================================
    // 죽음 처리
    // =============================================================================

    /// <summary>죽음 처리</summary>
    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentTime = 0f;
        isInGracePeriod = false;

        OnDeath?.Invoke();

        Debug.Log($"[TimeResource] 시간 고갈! 플레이어 사망");
    }

    // =============================================================================
    // 유틸리티
    // =============================================================================

    /// <summary>시간 초기화 (리스폰 시 사용)</summary>
    public void ResetTime()
    {
        currentTime = maxTime;
        isDead = false;
        isCritical = false;
        isInGracePeriod = false;
        hasUsedGrace = false;
        graceTimeRemaining = 0f;

        OnTimeChanged?.Invoke();

        Debug.Log($"[TimeResource] 시간 초기화: {maxTime}초");
    }

    /// <summary>디버그용: 강제 시간 설정</summary>
    [ContextMenu("테스트: 시간 10초로 설정")]
    private void DebugSetTime10()
    {
        currentTime = 10f;
        OnTimeChanged?.Invoke();
    }

    /// <summary>디버그용: 시간 소모 테스트</summary>
    [ContextMenu("테스트: 시간 20초 소모")]
    private void DebugConsumeTime()
    {
        ConsumeTime(20f);
    }

    /// <summary>디버그용: 시간 회복 테스트</summary>
    [ContextMenu("테스트: 시간 30초 회복")]
    private void DebugRecoverTime()
    {
        RecoverTime(30f);
    }
}
