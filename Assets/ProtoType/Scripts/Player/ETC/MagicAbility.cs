using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마법 전용 스킬 인디케이터 시스템
/// - Q 스킬: 파이어볼 타겟팅 인디케이터
/// - E 스킬: 아이스 스파이크 범위 인디케이터
/// - 검술과 독립적인 마법 전용 인디케이터
/// </summary>
public class MagicAbility : MonoBehaviour
{
    [Header("Q 스킬 인디케이터 (파이어볼)")]
    [SerializeField] private Canvas qSkillCanvas;
    [SerializeField] private Image qSkillTargetIndicator;      // 원형 타겟 인디케이터
    [SerializeField] private float qSkillMaxRange = 12f;       // 파이어볼 최대 사거리

    [Header("E 스킬 인디케이터 (아이스 스파이크)")]
    [SerializeField] private Canvas eSkillCanvas;
    [SerializeField] private Image eSkillAreaIndicator;        // 원형 범위 인디케이터
    [SerializeField] private float eSkillRadius = 5f;          // 아이스 스파이크 범위
    [SerializeField] private float eSkillMaxRange = 10f;       // 아이스 스파이크 최대 사거리

    [Header("마법 이펙트 색상")]
    [SerializeField] private Color fireballColor = Color.red;
    [SerializeField] private Color iceSpikeColor = Color.cyan;

    // 상태 관리
    private bool isQSkillActive = false;        // Q스킬 인디케이터 활성 상태
    private bool isESkillActive = false;        // E스킬 인디케이터 활성 상태
    private bool isESkillLockOnMode = false;    // E스킬 락온 모드
    private Vector3 eSkillFixedPosition;        // 락온 시 고정 위치

    /// <summary>초기화</summary>
    private void Start()
    {
        InitializeMagicIndicators();
    }

    /// <summary>마법 인디케이터 초기 설정</summary>
    private void InitializeMagicIndicators()
    {
        // Q 스킬 인디케이터 초기화
        if (qSkillCanvas != null)
        {
            qSkillCanvas.enabled = false;
        }
        if (qSkillTargetIndicator != null)
        {
            qSkillTargetIndicator.enabled = false;
            qSkillTargetIndicator.color = fireballColor;
        }

        // E 스킬 인디케이터 초기화
        if (eSkillCanvas != null)
        {
            eSkillCanvas.enabled = false;
        }
        if (eSkillAreaIndicator != null)
        {
            eSkillAreaIndicator.enabled = false;
            eSkillAreaIndicator.color = iceSpikeColor;
        }

        Debug.Log("[MagicAbility] 마법 인디케이터 시스템 초기화 완료");
    }

    /// <summary>프레임 업데이트</summary>
    private void Update()
    {
        // Q스킬 마우스 추적
        if (isQSkillActive && !isESkillLockOnMode)
        {
            UpdateQSkillMousePosition();
        }

        // E스킬 마우스 추적
        if (isESkillActive && !isESkillLockOnMode)
        {
            UpdateESkillMousePosition();
        }
    }

    // =============================================================================
    // Q 스킬 인디케이터 (파이어볼)
    // =============================================================================

    /// <summary>
    /// Q스킬 인디케이터 표시 - PlayerMagicSkill에서 호출
    /// 파이어볼 타겟팅용 원형 인디케이터
    /// </summary>
    public void ShowQSkillIndicator()
    {
        Debug.Log("[MagicAbility] Q스킬(파이어볼) 인디케이터 표시");

        // 다른 마법 인디케이터들 숨기기
        HideAllMagicIndicators();

        // Q스킬 인디케이터 활성화
        isQSkillActive = true;

        if (qSkillCanvas != null)
            qSkillCanvas.enabled = true;
        if (qSkillTargetIndicator != null)
        {
            qSkillTargetIndicator.enabled = true;
            qSkillTargetIndicator.color = fireballColor;
        }

        // 락온 모드 확인
        CheckLockOnMode();
    }

    /// <summary>Q스킬 인디케이터 숨기기</summary>
    public void HideQSkillIndicator()
    {
        Debug.Log("[MagicAbility] Q스킬(파이어볼) 인디케이터 숨김");

        isQSkillActive = false;

        if (qSkillCanvas != null)
            qSkillCanvas.enabled = false;
        if (qSkillTargetIndicator != null)
            qSkillTargetIndicator.enabled = false;
    }

    /// <summary>Q스킬 마우스 추적 위치 업데이트</summary>
    private void UpdateQSkillMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 targetPosition = hit.point;

            // 플레이어와의 거리 제한
            Vector3 playerPosition = transform.position;
            float distance = Vector3.Distance(playerPosition, targetPosition);

            if (distance > qSkillMaxRange)
            {
                Vector3 direction = (targetPosition - playerPosition).normalized;
                targetPosition = playerPosition + direction * qSkillMaxRange;
            }

            SetQSkillPosition(targetPosition);
        }
    }

    /// <summary>Q스킬 위치 설정</summary>
    /// <param name="position">타겟 위치</param>
    private void SetQSkillPosition(Vector3 position)
    {
        if (qSkillCanvas == null) return;

        // Y축 고정 (지면에 표시)
        float fixedY = transform.position.y;
        qSkillCanvas.transform.position = new Vector3(position.x, fixedY, position.z);
    }

    // =============================================================================
    // E 스킬 인디케이터 (아이스 스파이크)
    // =============================================================================

    /// <summary>
    /// E스킬 인디케이터 표시 - PlayerMagicSkill에서 호출
    /// 아이스 스파이크 범위 표시용 원형 인디케이터
    /// </summary>
    /// <param name="targetPosition">스킬 시전 위치</param>
    public void ShowESkillIndicator(Vector3 targetPosition)
    {
        Debug.Log($"[MagicAbility] E스킬(아이스 스파이크) 인디케이터 표시: 위치={targetPosition}");

        // 다른 마법 인디케이터들 숨기기
        HideAllMagicIndicators();

        // E스킬 인디케이터 활성화
        isESkillActive = true;

        if (eSkillCanvas != null)
            eSkillCanvas.enabled = true;
        if (eSkillAreaIndicator != null)
        {
            eSkillAreaIndicator.enabled = true;
            eSkillAreaIndicator.color = iceSpikeColor;
        }

        // 락온 모드 확인
        PlayerLockOn lockOn = FindFirstObjectByType<PlayerLockOn>();
        if (lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null)
        {
            // 락온 모드: 고정 위치
            isESkillLockOnMode = true;
            eSkillFixedPosition = targetPosition;
            SetESkillPosition(targetPosition);
        }
        else
        {
            // 마우스 추적 모드
            isESkillLockOnMode = false;
        }

        Debug.Log($"[MagicAbility] E스킬 모드: {(isESkillLockOnMode ? "락온 고정" : "마우스 추적")}");
    }

    /// <summary>E스킬 인디케이터 숨기기</summary>
    public void HideESkillIndicator()
    {
        Debug.Log("[MagicAbility] E스킬(아이스 스파이크) 인디케이터 숨김");

        isESkillActive = false;
        isESkillLockOnMode = false;

        if (eSkillCanvas != null)
            eSkillCanvas.enabled = false;
        if (eSkillAreaIndicator != null)
            eSkillAreaIndicator.enabled = false;
    }

    /// <summary>E스킬 마우스 추적 위치 업데이트</summary>
    private void UpdateESkillMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 targetPosition = hit.point;

            // 플레이어와의 거리 제한
            Vector3 playerPosition = transform.position;
            float distance = Vector3.Distance(playerPosition, targetPosition);

            if (distance > eSkillMaxRange)
            {
                Vector3 direction = (targetPosition - playerPosition).normalized;
                targetPosition = playerPosition + direction * eSkillMaxRange;
            }

            SetESkillPosition(targetPosition);
        }
    }

    /// <summary>E스킬 위치 설정</summary>
    /// <param name="position">설정할 위치</param>
    private void SetESkillPosition(Vector3 position)
    {
        if (eSkillCanvas == null) return;

        // Y축 고정 (지면에 표시)
        float fixedY = transform.position.y;
        eSkillCanvas.transform.position = new Vector3(position.x, fixedY, position.z);
    }

    // =============================================================================
    // 공통 기능
    // =============================================================================

    /// <summary>모든 마법 인디케이터 숨기기</summary>
    private void HideAllMagicIndicators()
    {
        // Q스킬 인디케이터
        if (qSkillCanvas != null)
            qSkillCanvas.enabled = false;
        if (qSkillTargetIndicator != null)
            qSkillTargetIndicator.enabled = false;

        // E스킬 인디케이터
        if (eSkillCanvas != null)
            eSkillCanvas.enabled = false;
        if (eSkillAreaIndicator != null)
            eSkillAreaIndicator.enabled = false;

        // 상태 초기화
        isQSkillActive = false;
        isESkillActive = false;
        isESkillLockOnMode = false;
    }

    /// <summary>락온 모드 확인</summary>
    private void CheckLockOnMode()
    {
        PlayerLockOn lockOn = FindFirstObjectByType<PlayerLockOn>();
        if (lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null)
        {
            isESkillLockOnMode = true;
            // 락온 타겟 위치로 설정하는 로직 추가 가능
        }
        else
        {
            isESkillLockOnMode = false;
        }
    }

    /// <summary>현재 Q스킬 타겟 위치 반환</summary>
    /// <returns>현재 Q스킬이 조준하고 있는 위치</returns>
    public Vector3 GetQSkillTargetPosition()
    {
        if (qSkillCanvas != null && isQSkillActive)
        {
            return qSkillCanvas.transform.position;
        }
        else
        {
            return transform.position + transform.forward * 5f; // 기본값
        }
    }

    /// <summary>현재 E스킬 타겟 위치 반환</summary>
    /// <returns>현재 E스킬이 조준하고 있는 위치</returns>
    public Vector3 GetESkillTargetPosition()
    {
        if (isESkillLockOnMode)
        {
            return eSkillFixedPosition;
        }
        else if (eSkillCanvas != null && isESkillActive)
        {
            return eSkillCanvas.transform.position;
        }
        else
        {
            return transform.position + transform.forward * 5f; // 기본값
        }
    }

    /// <summary>디버그용: 현재 상태 출력</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCurrentState()
    {
        Debug.Log($"[MagicAbility] 상태 - Q:{isQSkillActive}, E:{isESkillActive}, LockOn:{isESkillLockOnMode}");
    }
}
