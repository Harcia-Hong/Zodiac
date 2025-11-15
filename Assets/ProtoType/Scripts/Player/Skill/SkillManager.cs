using UnityEngine;
using System.Collections.Generic; // Dictionary 사용

/// <summary>
/// 플레이어의 스킬 슬롯(Q, E, R, T)을 관리하고,
/// 스킬 발동, 교체, 분해 프로세스를 총괄하는 사령탑.
/// 플레이어 오브젝트에 단 하나만 존재합니다.
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("스킬 슬롯")]
    // 현재 장착된 스킬 데이터를 관리합니다.
    private Dictionary<KeyCode, SkillData> equippedSkills = new Dictionary<KeyCode, SkillData>();
    private Dictionary<KeyCode, float> skillCooldowns = new Dictionary<KeyCode, float>();

    [Header("슬롯 키 설정")]
    // (나중에 UI와 연동할 때 이 부분은 더 복잡해질 수 있음)
    public KeyCode slotQKey = KeyCode.Q;
    public KeyCode slotEKey = KeyCode.E;
    public KeyCode slotRKey = KeyCode.R;
    public KeyCode slotTKey = KeyCode.T;
    public KeyCode slotLMBKey = KeyCode.Mouse0;
    public KeyCode slotRMBKey = KeyCode.Mouse1;

    private KeyCode[] manageableSlots;

    [Header("아이템 드랍 설정")]
    [Tooltip("바닥에 드랍할 스킬 아이템 프리펩 (DroppedSkillItem.cs 포함이에욘ㅇㅇ)")]
    [SerializeField] private GameObject droppedSkillItemPrefab;

    // 현재 교체 중인 스킬 아이템 (임시 저장)
    private DroppedSkillItem pendingSkillToSwap;

    private CombatUIManager uiManager;

    // --- (임시: 테스트용) ---
    private void Start()
    {
        InitializeSlots();

        manageableSlots = new KeyCode[] { slotQKey, slotEKey, slotRKey };
        uiManager = CombatUIManager.Instance;
        if (uiManager == null) Debug.LogError("[SkillManager] 씬에 CombatUIManager.Instance가 없습니다!");
    }

    private void InitializeSlots()
    {
        manageableSlots = new KeyCode[] { slotQKey, slotEKey, slotRKey };

        // 딕셔너리 초기화
        equippedSkills[slotQKey] = null;
        equippedSkills[slotEKey] = null;
        equippedSkills[slotRKey] = null;
        equippedSkills[slotTKey] = null;
        equippedSkills[slotLMBKey] = null;
        equippedSkills[slotRMBKey] = null;

        // 쿨다운 딕셔너리 초기화 (모든 키를 0 (즉시 사용 가능)으로 설정)
        skillCooldowns[slotQKey] = 0f;
        skillCooldowns[slotEKey] = 0f;
        skillCooldowns[slotRKey] = 0f;
        skillCooldowns[slotTKey] = 0f;
        skillCooldowns[slotLMBKey] = 0f;
        skillCooldowns[slotRMBKey] = 0f;
    }

    void Update()
    {
        // 1. 스킬 발동 입력 감지
        HandleSkillInput();

        // 2. 교체 UI가 활성화된 상태라면, 교체 입력 감지
        if (pendingSkillToSwap != null)
        {
            HandleSwapInput();
        }
    }

    /// <summary>
    /// Q, E, R, T 키 입력을 감지하여 스킬 발동
    /// </summary>
    private void HandleSkillInput()
    {
        if (Input.GetKeyDown(slotQKey)) TryActivateSkill(slotQKey);
        if (Input.GetKeyDown(slotEKey)) TryActivateSkill(slotEKey);
        if (Input.GetKeyDown(slotRKey)) TryActivateSkill(slotRKey);
        if (Input.GetKeyDown(slotTKey)) TryActivateSkill(slotTKey);
    }

    /// <summary>
    /// 특정 키에 할당된 스킬 발동 시도
    /// </summary>
    private void TryActivateSkill(KeyCode key)
    {
        if (IsSkillOnCooldown(key)) return;

        if (equippedSkills.TryGetValue(key, out SkillData skillToUse) && skillToUse != null)
        {
            // TODO: 쿨다운 체크 로직 추가

            Debug.Log($"[SkillManager] {key}키 스킬 '{skillToUse.skillName}' 발동!");

            // 1. 스킬 데이터에서 '로직 프리팹' 정보를 가져옴
            GameObject logicPrefab = skillToUse.logicPrefab;
            if (logicPrefab == null)
            {
                Debug.LogError($"'{skillToUse.skillName}'의 Logic Prefab이 비어있습니다!");
                return;
            }

            // 2. 로직 프리팹을 씬에 생성
            GameObject logicInstance = Instantiate(logicPrefab);
            // 3. 생성된 프리팹에서 ISkillLogic 인터페이스를 찾음
            ISkillLogic skillLogic = logicInstance.GetComponent<ISkillLogic>();

            if (skillLogic != null)
            {
                // 4. 스킬 발동! (owner로 플레이어 게임오브젝트 전달)
                skillLogic.Activate(this.gameObject);

                // TODO: 쿨다운 시작 로직 추가
                StartCooldown(key, skillToUse.cooldown);
            }
            else
            {
                Debug.LogError($"'{logicPrefab.name}'에 ISkillLogic.cs를 구현한 스크립트가 없습니다!");
                Destroy(logicInstance); // 로직이 없으므로 파괴
            }

            // (참고: 로직 프리팹(logicInstance)은 자신의 임무가 끝나면 스스로를 파괴해야 함)
        }
        else
        {
            Debug.Log($"[SkillManager] {key}키 슬롯이 비어있습니다.");
        }
    }

    private bool IsSkillOnCooldown(KeyCode key)
    {
        return skillCooldowns.TryGetValue(key, out float cooldownEndTime) && Time.time < cooldownEndTime;
    }

    private void StartCooldown(KeyCode key, float duration)
    {
        skillCooldowns[key] = Time.time + duration;
        uiManager?.StartSkillCoolDown(key, duration);
    }

    private void ResetCooldown(KeyCode key)
    {
        skillCooldowns[key] = 0f;
        // TODO
    }

    // -------------------------------------------------------------------
    // -- 1단계 : 빈 슬롯에 스킬 자동 장착 -> F키로 습득 시 ㅇㅇ
    // -------------------------------------------------------------------
    public bool AutoEquipSkill(SkillData skillData)
    {
        foreach (KeyCode key in manageableSlots)
        {
            if (equippedSkills.TryGetValue(key, out SkillData currentSkill) && currentSkill == null)
            {
                equippedSkills[key] = skillData;
                ResetCooldown(key);
                // TODO : UI 갱신
                uiManager?.UpdateSkillSlot(key, skillData);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 지정된 슬롯(Q, E, R)의 스킬을 장착 해제, 바닥에 버려잇 (Ctrl+클릭용)
    /// </summary>
    public void UnequipSkill(KeyCode slotKey)
    {
        if (!IsManageableSlot(slotKey))
            return;

        if (equippedSkills.TryGetValue(slotKey, out SkillData oldSkill) && oldSkill != null)
        {
            equippedSkills[slotKey] = null; 
            DropSkill(oldSkill); // 바닥에 기존 스킬 아이템 생성

            // TODO : UI 갱신
            ResetCooldown(slotKey); // 플레이 해보고 악용의 요지가 있을 경우 개편
            uiManager?.UpdateSkillSlot(slotKey, null);
        }
    }

    /// <summary>
    /// 두 슬롯(Q, E, R)의 스킬을 서로 변경!!. (Ctrl+드래그 스왑용)
    /// </summary>
    public void SwapSkills(KeyCode fromKey, KeyCode toKey)
    {
        if (!IsManageableSlot(fromKey) || !IsManageableSlot(toKey))
            return;

        if (fromKey == toKey) return;

        // 딕셔너리에서 데이터 임시 추출
        equippedSkills.TryGetValue(fromKey, out SkillData fromSkill);
        equippedSkills.TryGetValue(toKey, out SkillData toSkill);

        // 딕셔너리에 데이터 스왑
        equippedSkills[fromKey] = toSkill;
        equippedSkills[toKey] = fromSkill;

        // 쿨타임이 돌고 있었다면, 그 쿨타임도 함께 따라갑니다.
        skillCooldowns.TryGetValue(fromKey, out float fromCooldownEndTime);
        skillCooldowns.TryGetValue(toKey, out float toCooldownEndTime);

        skillCooldowns[fromKey] = toCooldownEndTime;
        skillCooldowns[toKey] = fromCooldownEndTime;

        // TODO : UI 갱신 ( 변경된 양쪽 슬롯 모두 )
        uiManager?.UpdateSkillSlot(fromKey, toSkill);
        uiManager?.UpdateSkillSlot(toKey, fromSkill);
    }

    /// <summary>
    /// 스킬 데이터를 받아 바닥에 아이템을 생성
    /// </summary>
    private void DropSkill(SkillData skillToDrop)
    {
        if (skillToDrop == null || droppedSkillItemPrefab == null) return;

        // 플레이어 코 앞에 생성
        Vector3 spawnPos = transform.position + transform.forward * 1f;

        GameObject droppedItem = Instantiate(droppedSkillItemPrefab, spawnPos, Quaternion.identity);

        // 드랍된 아이템에 스킬 데이터도 전달해야죠 ㅇㅇ
        DroppedSkillItem itemLogic = droppedItem.GetComponent<DroppedSkillItem>();

        if (itemLogic != null)
            itemLogic.Initialize(skillToDrop);
        else
            Debug.LogError($"[SkillManager] {droppedSkillItemPrefab.name}에 DroppedSkillItem 스크립트가 없습니다!");
    }

    /// <summary>
    /// 해당 키가 Q, E, R 중 하나인지 확인하는 헬퍼 함수
    /// </summary>
    private bool IsManageableSlot(KeyCode key)
    {
        return key == slotQKey || key == slotEKey || key == slotRKey;
    }

    // -------------------------------------------------------------------
    // -- 2/3단계: 스킬 교체 로직
    // -------------------------------------------------------------------

    /// <summary>
    /// (1) DroppedSkillItem이 F키를 누르면 호출할 함수
    /// </summary>
    public void StartSwapProcess(DroppedSkillItem itemToSwap)
    {
        pendingSkillToSwap = itemToSwap;
        
        // TODO: "Q, E, R 중 교체할 슬롯을 선택하세요" UI 활성화
        Debug.Log($"[SkillManager] 교체 시작: '{itemToSwap.GetSkillData().skillName}'. Q,E,R 중 선택...");

        // [수정] 교체 시에도 Ctrl UI 확장 (임시)
        uiManager?.ExpandUI(true);
    }

    /// <summary>
    /// (2) 교체 UI가 뜬 상태에서 Q,E,R,T 입력을 감지
    /// </summary>
    private void HandleSwapInput()
    {
        if (Input.GetKeyDown(slotQKey)) FinalizeSwap(slotQKey);
        if (Input.GetKeyDown(slotEKey)) FinalizeSwap(slotEKey);
        if (Input.GetKeyDown(slotRKey)) FinalizeSwap(slotRKey);

        // TODO: 교체 취소 (Esc 등) 로직 추가
        if (Input.GetKeyDown(KeyCode.Escape)) CancelSwapProcess();
    }

    private void CancelSwapProcess()
    {
        Debug.Log("[SkillManager] 스킬 교체를 취소했습니다.");
        pendingSkillToSwap = null;

        // Ctrl 키가 안 눌려있다면 UI 원상복구
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            uiManager?.ExpandUI(false);
        }
    }

    /// <summary>
    /// (3) 특정 슬롯을 선택했을 때, 실제 교체 실행
    /// </summary>
    private void FinalizeSwap(KeyCode selectedKey)
    {
        if (pendingSkillToSwap == null) return;

        if (!IsManageableSlot(selectedKey))
            return;

        SkillData newSkill = pendingSkillToSwap.GetSkillData();
        SkillData oldSkill = equippedSkills[selectedKey]; // 기존에 장착되어 있던 스킬 (null일 수도 있음)

        // 1. 새 스킬을 슬롯에 장착
        equippedSkills[selectedKey] = newSkill;
        ResetCooldown(selectedKey);
        // TODO: UI 갱신 - selectedKey 슬롯의 아이콘을 newSkill.skillIcon으로 변경
        uiManager?.UpdateSkillSlot(selectedKey, newSkill);

        Debug.Log($"[SkillManager] {selectedKey} 슬롯에 '{newSkill.skillName}' 장착!");

        // 2. 기존 스킬이 있었다면 바닥에 드랍
        if (oldSkill != null)
        {
            Debug.Log($"[SkillManager] 기존 스킬 '{oldSkill.skillName}'을(를) 바닥에 드랍합니다.");

            // TODO: 'DroppedSkill_Prefab'을 바닥에 생성하고,
            DropSkill(oldSkill);
            // 그 프리팹의 Initialize(oldSkill)을 호출하는 로직 필요.
            // (이 로직은 DroppedSkillItem.cs의 Altar가 하던 것과 유사)
        }
        
        // 3. 교체가 완료되었으므로, 바닥에 있던 아이템 파괴
        Destroy(pendingSkillToSwap.gameObject);
        pendingSkillToSwap = null;

        // TODO: 교체 선택 UI 닫기
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
            uiManager?.ExpandUI(false);
    }
}
