using UnityEngine;

/// <summary>
/// 플레이어 검술 스타일 관리자
/// 검술 전환 및 스타일별 설정 관리 전용
/// </summary>
public class PlayerSwordStyleManager : MonoBehaviour
{
    [Header("Current Sword Style")]
    [SerializeField] private SwordStyle _currentStyle;

    [Header("References")]
    [SerializeField] private Animator playerAnimator;

    // 현재 검술 스타일 프로퍼티
    public SwordStyle currentStyle
    {
        get => _currentStyle;
        private set => _currentStyle = value;
    }

    private void Awake()
    {
        // 애니메이터 자동 참조
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // 시작 시 기본 스타일 적용
        if (_currentStyle != null)
        {
            ApplyStyle(_currentStyle);
        }
    }

    /// <summary>
    /// 새로운 검술 스타일로 전환
    /// </summary>
    /// <param name="newStyle">적용할 검술 스타일</param>
    public void ChangeStyle(SwordStyle newStyle)
    {
        if (newStyle == null)
        {
            Debug.LogWarning("[PlayerSwordStyleManager] null 스타일로 전환할 수 없습니다.");
            return;
        }

        if (newStyle == _currentStyle)
        {
            Debug.Log($"[PlayerSwordStyleManager] 이미 {newStyle.styleName} 스타일입니다.");
            return;
        }

        Debug.Log($"[PlayerSwordStyleManager] 검술 전환: {_currentStyle?.styleName ?? "없음"} → {newStyle.styleName}");

        _currentStyle = newStyle;
        ApplyStyle(newStyle);
    }

    /// <summary>
    /// 검술 스타일 적용
    /// </summary>
    /// <param name="style">적용할 스타일</param>
    private void ApplyStyle(SwordStyle style)
    {
        // 애니메이터 오버라이드 적용
        if (style.animatorOverride != null && playerAnimator != null)
        {
            playerAnimator.runtimeAnimatorController = style.animatorOverride;
            Debug.Log($"[PlayerSwordStyleManager] 애니메이터 변경: {style.animatorOverride.name}");
        }

        // UI 업데이트 (CombatUIManager를 통해)
        UpdateUI(style);

        // 스킬 쿨다운 적용 (검술 변경 후 즉시 적용)
        ApplyStyleCooldowns();
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    /// <param name="style">적용할 스타일</param>
    private void UpdateUI(SwordStyle style)
    {
        if (CombatUIManager.Instance == null) return;

        // 검술 아이콘 변경
        if (style.iconSprite != null)
        {
            CombatUIManager.Instance.SetSwordIcon(style.iconSprite);
        }

        // 스킬 아이콘들 변경 (Q, E, R 순서로 가정)
        if (style.skillSprites != null && style.skillSprites.Count >= 3)
        {
            CombatUIManager.Instance.SetSkillIcon(0, style.skillSprites[0]); // Q 스킬
            CombatUIManager.Instance.SetSkillIcon(1, style.skillSprites[1]); // E 스킬  
            CombatUIManager.Instance.SetSkillIcon(2, style.skillSprites[2]); // R 스킬
        }
    }

    /// <summary>
    /// 검술 변경 후 스킬 쿨다운 적용
    /// </summary>
    private void ApplyStyleCooldowns()
    {
        if (CombatUIManager.Instance == null) return;

        // 검술 변경 후 모든 스킬에 짧은 쿨다운 적용
        CombatUIManager.Instance.StartSkillCoolDown(0, 3f); // Q 스킬
        CombatUIManager.Instance.StartSkillCoolDown(1, 3f); // E 스킬
        CombatUIManager.Instance.StartSkillCoolDown(2, 3f); // R 스킬
    }

    /// <summary>
    /// 현재 스타일의 콤보 트리거 반환
    /// </summary>
    /// <returns>콤보 트리거 리스트</returns>
    public string[] GetComboTriggers()
    {
        if (_currentStyle == null || _currentStyle.comboTriggers == null)
            return new string[0];

        return _currentStyle.comboTriggers.ToArray();
    }

    /// <summary>
    /// 현재 스타일의 전진 거리 반환
    /// </summary>
    /// <returns>전진 거리 리스트</returns>
    public float[] GetForwardDistances()
    {
        if (_currentStyle == null || _currentStyle.forwardDistances == null)
            return new float[0];

        return _currentStyle.forwardDistances.ToArray();
    }

    /// <summary>
    /// 현재 스타일의 Q 스킬 트리거 반환
    /// </summary>
    /// <returns>Q 스킬 트리거</returns>
    public string GetSkillQTrigger()
    {
        return _currentStyle?.skillQTrigger ?? "";
    }

    /// <summary>
    /// 현재 스타일의 스킬 이펙트 반환
    /// </summary>
    /// <returns>스킬 이펙트 프리팹</returns>
    public GameObject GetSkillEffectPrefab()
    {
        return _currentStyle?.skillEffectPrefab;
    }

    /// <summary>
    /// 현재 스타일의 콤보 슬래시 이펙트 반환
    /// </summary>
    /// <param name="comboIndex">콤보 인덱스</param>
    /// <returns>해당 콤보의 슬래시 이펙트</returns>
    public GameObject GetComboSlashEffect(int comboIndex)
    {
        if (_currentStyle?.comboSlashEffects == null ||
            comboIndex < 0 ||
            comboIndex >= _currentStyle.comboSlashEffects.Length)
            return null;

        return _currentStyle.comboSlashEffects[comboIndex];
    }

    /// <summary>
    /// 현재 스타일의 슬래시 색상 반환
    /// </summary>
    /// <returns>슬래시 색상</returns>
    public Color GetSlashColor()
    {
        return _currentStyle?.slashColor ?? Color.white;
    }

    /// <summary>
    /// 현재 스타일의 이펙트 스케일 반환
    /// </summary>
    /// <returns>이펙트 스케일</returns>
    public float GetEffectScale()
    {
        return _currentStyle?.effectScale ?? 1f;
    }

    /// <summary>
    /// 검술 스타일 정보 로그 출력 (디버깅용)
    /// </summary>
    [ContextMenu("현재 검술 스타일 정보")]
    public void LogCurrentStyleInfo()
    {
        if (_currentStyle == null)
        {
            Debug.Log("[PlayerSwordStyleManager] 현재 검술 스타일: 없음");
            return;
        }

        Debug.Log("=== 현재 검술 스타일 정보 ===");
        Debug.Log($"이름: {_currentStyle.styleName}");
        Debug.Log($"콤보 수: {_currentStyle.comboTriggers?.Count ?? 0}");
        Debug.Log($"Q 스킬 트리거: {_currentStyle.skillQTrigger}");
        Debug.Log($"애니메이터: {_currentStyle.animatorOverride?.name ?? "없음"}");
        Debug.Log($"슬래시 색상: {_currentStyle.slashColor}");
        Debug.Log($"이펙트 스케일: {_currentStyle.effectScale}");
    }
}
