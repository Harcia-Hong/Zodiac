using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 플레이어 무기 시스템 통합 관리자
/// - 무기 전환 처리
/// - 각 무기별 스킬 스크립트 활성화/비활성화
/// - UI 업데이트 및 애니메이션 연동
/// - 쿨타임 관리
/// </summary>
public class PlayerWeaponManager : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponType currentWeaponType = WeaponType.Sword;

    [Header("Weapon Skill Scripts")]
    [SerializeField] private PlayerSwordSkill swordSkill;
    [SerializeField] private PlayerMagicSkill magicSkill;
    [SerializeField] private PlayerGunSkill gunSkill;
    [SerializeField] private PlayerBowSkill bowSkill;

    [Header("Weapon GameObjects")]
    [SerializeField] private GameObject swordObject;
    [SerializeField] private GameObject staffObject;
    [SerializeField] private GameObject gunObject;
    [SerializeField] private GameObject bowObject;

    [Header("Weapon Data")]
    [SerializeField] private WeaponData[] availableWeapons;

    [Header("Switch Settings")]
    [SerializeField] private float weaponSwitchDelay = 0.3f;
    [SerializeField] private bool allowSwitchDuringCombat = false;

    [Header("Weapon Switch Effects")]
    [SerializeField] private GameObject weaponSwitchVFX;
    [SerializeField] private AudioClip weaponSwitchSound;
    [SerializeField] private Transform handTransform;

    // 컴포넌트 참조
    private PlayerController playerController;
    private Animator anim;
    private AudioSource audioSource;

    // 상태 관리
    private bool isSwitching = false;
    private Dictionary<WeaponType, IWeaponSkill> weaponSkills;
    private Dictionary<WeaponType, GameObject> weaponObjects;

    /// <summary>초기화</summary>
    private void Awake()
    {
        InitializeComponents();
        SetupWeaponDictionaries();
    }

    /// <summary>시작 시 설정</summary>
    private void Start()
    {
        // 시작 무기 설정
        SwitchWeapon(currentWeaponType, true);
    }

    /// <summary>컴포넌트 참조 초기화</summary>
    private void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        anim = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // 스킬 스크립트 자동 참조
        if (swordSkill == null) swordSkill = GetComponent<PlayerSwordSkill>();
        if (magicSkill == null) magicSkill = GetComponent<PlayerMagicSkill>();
        if (gunSkill == null) gunSkill = GetComponent<PlayerGunSkill>();
        if (bowSkill == null) bowSkill = GetComponent<PlayerBowSkill>();

        // 핸드 트랜스폼 자동 찾기
        if (handTransform == null)
        {
            handTransform = FindDeepChild(transform, "Hand") ?? transform;
        }
    }

    /// <summary>무기 딕셔너리 설정</summary>
    private void SetupWeaponDictionaries()
    {
        // 스킬 스크립트 딕셔너리 (null 체크 포함)
        weaponSkills = new Dictionary<WeaponType, IWeaponSkill>();

        if (swordSkill != null) weaponSkills.Add(WeaponType.Sword, swordSkill);
        if (magicSkill != null) weaponSkills.Add(WeaponType.Magic, magicSkill);
        if (gunSkill != null) weaponSkills.Add(WeaponType.Gun, gunSkill);
        if (bowSkill != null) weaponSkills.Add(WeaponType.Bow, bowSkill);

        // 무기 오브젝트 딕셔너리 (null 체크 포함)
        weaponObjects = new Dictionary<WeaponType, GameObject>();

        if (swordObject != null) weaponObjects.Add(WeaponType.Sword, swordObject);
        if (staffObject != null) weaponObjects.Add(WeaponType.Magic, staffObject);
        if (gunObject != null) weaponObjects.Add(WeaponType.Gun, gunObject);
        if (bowObject != null) weaponObjects.Add(WeaponType.Bow, bowObject);

        Debug.Log($"[WeaponManager] 무기 시스템 초기화 완료 - 스킬: {weaponSkills.Count}, 오브젝트: {weaponObjects.Count}");
    }

    /// <summary>매 프레임 업데이트</summary>
    private void Update()
    {
        HandleWeaponSwitchInput();
        HandleSkillInput();
    }

    /// <summary>무기 전환 입력 처리</summary>
    private void HandleWeaponSwitchInput()
    {
        // 숫자 키로 무기 전환
        if (Input.GetKeyDown(KeyCode.Alpha1)) TrySwitchWeapon(WeaponType.Sword);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TrySwitchWeapon(WeaponType.Magic);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TrySwitchWeapon(WeaponType.Gun);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TrySwitchWeapon(WeaponType.Bow);

        // 마우스 휠로 순차 전환
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.1f) SwitchToNextWeapon();
        if (scroll < -0.1f) SwitchToPreviousWeapon();
    }

    /// <summary>스킬 입력을 현재 무기로 전달</summary>
    private void HandleSkillInput()
    {
        if (isSwitching || !CanUseSkills()) return;

        var currentSkill = GetCurrentWeaponSkill();
        if (currentSkill == null) return;

        // Q스킬 (기본 공격 스킬)
        if (Input.GetButtonDown("Skill_Q"))
        {
            currentSkill.UseQSkill();
        }

        // E스킬 (특수 공격 스킬)
        if (Input.GetKeyDown(KeyCode.E))
        {
            currentSkill.UseESkill();
        }
    }

    // =============================================================================
    // 무기 전환 시스템
    // =============================================================================

    /// <summary>무기 전환 시도</summary>
    public void TrySwitchWeapon(WeaponType targetWeapon)
    {
        if (!CanSwitchWeapon(targetWeapon))
        {
            Debug.Log($"[WeaponManager] 무기 전환 불가: {targetWeapon}");
            return;
        }

        SwitchWeapon(targetWeapon);
    }

    /// <summary>무기 전환 실행</summary>
    public void SwitchWeapon(WeaponType newWeaponType, bool immediate = false)
    {
        if (newWeaponType == currentWeaponType && !immediate)
        {
            Debug.Log($"[WeaponManager] 이미 {newWeaponType} 무기입니다.");
            return;
        }

        Debug.Log($"[WeaponManager] 무기 전환: {currentWeaponType} → {newWeaponType}");

        if (immediate)
        {
            ExecuteWeaponSwitch(newWeaponType);
        }
        else
        {
            StartCoroutine(WeaponSwitchWithAnimation(newWeaponType));
        }
    }

    /// <summary>무기 전환 애니메이션 포함</summary>
    private IEnumerator WeaponSwitchWithAnimation(WeaponType newWeaponType)
    {
        isSwitching = true;

        // 1. 전환 시작 알림
        Debug.Log($"[WeaponManager] 무기 전환 시작: {newWeaponType}");

        // 2. 전환 이펙트 재생
        PlayWeaponSwitchEffect();

        // 3. 현재 무기 페이드아웃 + 비활성화
        yield return StartCoroutine(FadeOutCurrentWeapon());

        // 4. 무기 전환
        WeaponType oldWeapon = currentWeaponType;
        currentWeaponType = newWeaponType;

        // 5. 새 무기 활성화 + 페이드인
        yield return StartCoroutine(FadeInNewWeapon());

        // 6. UI 및 애니메이터 업데이트
        UpdateWeaponUI();
        UpdateAnimatorController();

        // 7. 전환 완료
        isSwitching = false;
        Debug.Log($"[WeaponManager] 무기 전환 완료: {oldWeapon} → {newWeaponType}");
    }

    /// <summary>현재 무기 페이드아웃</summary>
    private IEnumerator FadeOutCurrentWeapon()
    {
        GameObject currentWeaponObj = GetCurrentWeaponObject();
        if (currentWeaponObj != null)
        {
            // 페이드아웃 효과
            yield return StartCoroutine(FadeWeapon(currentWeaponObj, 1f, 0f, weaponSwitchDelay * 0.5f));
        }

        // 현재 무기 비활성화
        DeactivateCurrentWeapon();
    }

    /// <summary>새 무기 페이드인</summary>
    private IEnumerator FadeInNewWeapon()
    {
        // 새 무기 활성화
        ActivateCurrentWeapon();

        GameObject newWeaponObj = GetCurrentWeaponObject();
        if (newWeaponObj != null)
        {
            // 페이드인 효과
            yield return StartCoroutine(FadeWeapon(newWeaponObj, 0f, 1f, weaponSwitchDelay * 0.5f));
        }
    }

    /// <summary>무기 페이드 효과</summary>
    private IEnumerator FadeWeapon(GameObject weapon, float startAlpha, float endAlpha, float duration)
    {
        if (weapon == null) yield break;

        Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) yield break;

        float elapsed = 0f;

        // 시작 알파값 설정
        SetWeaponAlpha(renderers, startAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);

            SetWeaponAlpha(renderers, alpha);
            yield return null;
        }

        // 최종 알파값 설정
        SetWeaponAlpha(renderers, endAlpha);
    }

    /// <summary>무기 알파값 설정</summary>
    private void SetWeaponAlpha(Renderer[] renderers, float alpha)
    {
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            foreach (var material in renderer.materials)
            {
                if (material == null) continue;

                // 메테리얼의 렌더링 모드를 Transparent로 설정 필요
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }
    }

    /// <summary>무기 전환 이펙트 재생</summary>
    private void PlayWeaponSwitchEffect()
    {
        // VFX 이펙트 재생
        if (weaponSwitchVFX != null && handTransform != null)
        {
            GameObject effect = Instantiate(weaponSwitchVFX, handTransform.position, handTransform.rotation);
            Destroy(effect, 2f); // 2초 후 자동 삭제
        }

        // 효과음 재생
        if (weaponSwitchSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(weaponSwitchSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(weaponSwitchSound, transform.position);
            }
        }
    }

    /// <summary>다음 무기로 전환</summary>
    private void SwitchToNextWeapon()
    {
        WeaponType[] weaponTypes = System.Enum.GetValues(typeof(WeaponType)) as WeaponType[];
        int currentIndex = System.Array.IndexOf(weaponTypes, currentWeaponType);

        // 사용 가능한 다음 무기 찾기
        for (int i = 1; i < weaponTypes.Length; i++)
        {
            int nextIndex = (currentIndex + i) % weaponTypes.Length;
            WeaponType nextWeapon = weaponTypes[nextIndex];

            if (IsWeaponAvailable(nextWeapon))
            {
                TrySwitchWeapon(nextWeapon);
                break;
            }
        }
    }

    /// <summary>이전 무기로 전환</summary>
    private void SwitchToPreviousWeapon()
    {
        WeaponType[] weaponTypes = System.Enum.GetValues(typeof(WeaponType)) as WeaponType[];
        int currentIndex = System.Array.IndexOf(weaponTypes, currentWeaponType);

        // 사용 가능한 이전 무기 찾기
        for (int i = 1; i < weaponTypes.Length; i++)
        {
            int prevIndex = (currentIndex - i + weaponTypes.Length) % weaponTypes.Length;
            WeaponType prevWeapon = weaponTypes[prevIndex];

            if (IsWeaponAvailable(prevWeapon))
            {
                TrySwitchWeapon(prevWeapon);
                break;
            }
        }
    }

    /// <summary>즉시 무기 전환 (애니메이션 없음)</summary>
    private void ExecuteWeaponSwitch(WeaponType newWeaponType)
    {
        // 현재 무기 비활성화
        DeactivateCurrentWeapon();

        // 새 무기 설정
        currentWeaponType = newWeaponType;

        // 새 무기 활성화
        ActivateCurrentWeapon();

        // UI 및 애니메이터 업데이트
        UpdateWeaponUI();
        UpdateAnimatorController();

        Debug.Log($"[WeaponManager] 즉시 무기 전환 완료: {newWeaponType}");
    }

    /// <summary>현재 무기 비활성화</summary>
    private void DeactivateCurrentWeapon()
    {
        // 현재 무기 스킬 비활성화
        var currentSkill = GetCurrentWeaponSkill();
        currentSkill?.DeactivateSkills();

        // 현재 무기 오브젝트 비활성화
        var currentWeaponObj = GetCurrentWeaponObject();
        if (currentWeaponObj != null)
        {
            currentWeaponObj.SetActive(false);
        }
    }

    /// <summary>현재 무기 활성화</summary>
    private void ActivateCurrentWeapon()
    {
        // 현재 무기 오브젝트 활성화
        var currentWeaponObj = GetCurrentWeaponObject();
        if (currentWeaponObj != null)
        {
            currentWeaponObj.SetActive(true);
        }

        // 현재 무기 스킬 활성화
        var currentSkill = GetCurrentWeaponSkill();
        currentSkill?.ActivateSkills();
    }

    /// <summary>무기 UI 업데이트</summary>
    private void UpdateWeaponUI()
    {
        WeaponData weaponData = GetWeaponData(currentWeaponType);
        if (weaponData == null) return;

        if (CombatUIManager.Instance != null)
        {
            // 무기 아이콘 변경
            if (weaponData.weaponIcon != null)
            {
                CombatUIManager.Instance.SetSwordIcon(weaponData.weaponIcon);
            }

            // 스킬 아이콘들 변경
            if (weaponData.skillIcons != null && weaponData.skillIcons.Length >= 2)
            {
                CombatUIManager.Instance.SetSkillIcon(0, weaponData.skillIcons[0]); // Q스킬
                CombatUIManager.Instance.SetSkillIcon(1, weaponData.skillIcons[1]); // E스킬
            }

            // 무기 전환 쿨다운 적용 (전환 중임을 표시)
            float adjustedDelay = weaponSwitchDelay + (weaponData.switchDelay);
            CombatUIManager.Instance.StartSkillCoolDown(0, adjustedDelay);
            CombatUIManager.Instance.StartSkillCoolDown(1, adjustedDelay);
        }
    }

    /// <summary>애니메이터 컨트롤러 업데이트</summary>
    private void UpdateAnimatorController()
    {
        WeaponData weaponData = GetWeaponData(currentWeaponType);
        if (weaponData?.animatorController != null && anim != null)
        {
            anim.runtimeAnimatorController = weaponData.animatorController;
            Debug.Log($"[WeaponManager] 애니메이터 변경: {weaponData.animatorController.name}");
        }
    }

    // =============================================================================
    // 조건 체크 메서드들
    // =============================================================================

    /// <summary>무기 전환 가능 여부 확인</summary>
    private bool CanSwitchWeapon(WeaponType targetWeapon)
    {
        // 이미 전환 중
        if (isSwitching)
        {
            Debug.Log("[WeaponManager] 전환 중이므로 무기 변경 불가");
            return false;
        }

        // 같은 무기
        if (targetWeapon == currentWeaponType)
        {
            Debug.Log("[WeaponManager] 이미 같은 무기");
            return false;
        }

        // 무기 데이터 및 사용 가능 여부 확인
        if (!IsWeaponAvailable(targetWeapon))
        {
            Debug.Log($"[WeaponManager] {targetWeapon} 무기를 사용할 수 없음");
            return false;
        }

        // 전투 중 전환 불가 설정
        if (!allowSwitchDuringCombat && IsInCombat())
        {
            Debug.Log("[WeaponManager] 전투 중 무기 전환 불가");
            return false;
        }

        // 스킬 시전 중
        if (playerController?.isSkillCasting == true)
        {
            Debug.Log("[WeaponManager] 스킬 시전 중 무기 전환 불가");
            return false;
        }

        // 회피 중
        if (playerController?.isDodging == true)
        {
            Debug.Log("[WeaponManager] 회피 중 무기 전환 불가");
            return false;
        }

        return true;
    }

    /// <summary>스킬 사용 가능 여부 확인</summary>
    private bool CanUseSkills()
    {
        if (isSwitching) return false;
        if (playerController?.isSkillCasting == true) return false;
        if (playerController?.isDodging == true) return false;

        return true;
    }

    /// <summary>전투 상태 확인</summary>
    private bool IsInCombat()
    {
        // 주변에 적이 있는지 확인
        var enemies = EnemyManager.GetAllEnemies();
        if (enemies == null || enemies.Count == 0) return false;

        float combatRange = 15f;
        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= combatRange)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // =============================================================================
    // 유틸리티 메서드들
    // =============================================================================

    /// <summary>현재 무기 스킬 스크립트 반환</summary>
    private IWeaponSkill GetCurrentWeaponSkill()
    {
        if (weaponSkills.ContainsKey(currentWeaponType))
        {
            return weaponSkills[currentWeaponType];
        }
        return null;
    }

    /// <summary>현재 무기 오브젝트 반환</summary>
    private GameObject GetCurrentWeaponObject()
    {
        if (weaponObjects.ContainsKey(currentWeaponType))
        {
            return weaponObjects[currentWeaponType];
        }
        return null;
    }

    /// <summary>특정 무기의 데이터 반환</summary>
    private WeaponData GetWeaponData(WeaponType weaponType)
    {
        if (availableWeapons == null) return null;

        foreach (var weapon in availableWeapons)
        {
            if (weapon?.weaponType == weaponType)
            {
                return weapon;
            }
        }
        return null;
    }

    /// <summary>깊은 자식 오브젝트 찾기</summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;

            Transform found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // =============================================================================
    // 공개 인터페이스
    // =============================================================================

    /// <summary>현재 무기 타입 반환</summary>
    public WeaponType GetCurrentWeaponType() => currentWeaponType;

    /// <summary>현재 무기가 특정 타입인지 확인</summary>
    public bool IsCurrentWeapon(WeaponType weaponType) => currentWeaponType == weaponType;

    /// <summary>특정 무기 사용 가능 여부 확인</summary>
    public bool IsWeaponAvailable(WeaponType weaponType)
    {
        WeaponData data = GetWeaponData(weaponType);
        return data != null && data.isUnlocked;
    }

    /// <summary>무기 잠금/해제</summary>
    public void SetWeaponUnlocked(WeaponType weaponType, bool unlocked)
    {
        WeaponData data = GetWeaponData(weaponType);
        if (data != null)
        {
            data.isUnlocked = unlocked;
            Debug.Log($"[WeaponManager] {weaponType} 무기 {(unlocked ? "해제" : "잠금")}");
        }
    }

    /// <summary>모든 무기 강제 정지</summary>
    public void ForceStopAllWeapons()
    {
        foreach (var skill in weaponSkills.Values)
        {
            skill?.DeactivateSkills();
        }

        if (playerController != null)
        {
            playerController.isSkillCasting = false;
        }

        isSwitching = false;
        Debug.Log("[WeaponManager] 모든 무기 강제 정지");
    }

    /// <summary>현재 무기의 쿨다운 상태 반환</summary>
    public object GetCurrentWeaponCooldowns()
    {
        var currentSkill = GetCurrentWeaponSkill();
        return currentSkill?.GetCooldownStatus();
    }

    /// <summary>전환 중인지 여부</summary>
    public bool IsSwitching() => isSwitching;

    // =============================================================================
    // 디버그 및 테스트
    // =============================================================================

    /// <summary>현재 상태 로그 출력</summary>
    [ContextMenu("현재 무기 상태 출력")]
    public void LogCurrentWeaponStatus()
    {
        Debug.Log("=== 무기 시스템 현황 ===");
        Debug.Log($"현재 무기: {currentWeaponType}");
        Debug.Log($"전환 중: {isSwitching}");
        Debug.Log($"전투 중: {IsInCombat()}");
        Debug.Log($"스킬 사용 가능: {CanUseSkills()}");

        var currentSkill = GetCurrentWeaponSkill();
        if (currentSkill != null)
        {
            Debug.Log($"현재 무기 스킬: {currentSkill.GetType().Name}");
            Debug.Log($"쿨다운 상태: {currentSkill.GetCooldownStatus()}");
        }

        Debug.Log("사용 가능한 무기:");
        foreach (WeaponType weapon in System.Enum.GetValues(typeof(WeaponType)))
        {
            Debug.Log($"- {weapon}: {(IsWeaponAvailable(weapon) ? "사용가능" : "잠금")}");
        }
    }

    /// <summary>테스트용: 강제 무기 전환</summary>
    [ContextMenu("테스트: 마법 무기로 전환")]
    public void TestSwitchToMagic()
    {
        SwitchWeapon(WeaponType.Magic, true);
    }

    /// <summary>테스트용: 강제 무기 전환</summary>
    [ContextMenu("테스트: 검 무기로 전환")]
    public void TestSwitchToSword()
    {
        SwitchWeapon(WeaponType.Sword, true);
    }
}

// =============================================================================
// 무기 시스템 관련 데이터 클래스들
// =============================================================================

/// <summary>
/// 무기 타입 열거형
/// </summary>
public enum WeaponType
{
    Sword,   // 검
    Magic,   // 마법 스태프
    Gun,     // 총기
    Bow      // 활
}

/// <summary>
/// 무기별 스킬 인터페이스
/// 모든 무기 스킬 스크립트가 구현해야 하는 공통 인터페이스
/// </summary>
public interface IWeaponSkill
{
    /// <summary>Q스킬 사용</summary>
    void UseQSkill();

    /// <summary>E스킬 사용</summary>
    void UseESkill();

    /// <summary>스킬 활성화</summary>
    void ActivateSkills();

    /// <summary>스킬 비활성화</summary>
    void DeactivateSkills();

    /// <summary>쿨다운 상태 반환</summary>
    object GetCooldownStatus();
}

/// <summary>
/// 무기 데이터 ScriptableObject
/// Inspector에서 무기별 설정을 관리
/// </summary>
[System.Serializable]
public class WeaponData
{
    [Header("Basic Info")]
    public string weaponName;
    public WeaponType weaponType;

    [Header("UI")]
    public Sprite weaponIcon;
    public Sprite[] skillIcons; // [0] = Q스킬, [1] = E스킬

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;

    [Header("Stats")]
    public float baseDamage = 10f;
    public float baseSpeed = 1f;
    public float baseRange = 2f;

    [Header("Special Settings")]
    [Tooltip("이 무기가 현재 사용 가능한지")]
    public bool isUnlocked = true;

    [Tooltip("무기 전환 시 추가 딜레이")]
    public float switchDelay = 0f;
}
