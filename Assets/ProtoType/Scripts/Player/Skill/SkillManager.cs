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

    [Header("슬롯 키 설정")]
    // (나중에 UI와 연동할 때 이 부분은 더 복잡해질 수 있음)
    public KeyCode slotQKey = KeyCode.Q;
    public KeyCode slotEKey = KeyCode.E;
    public KeyCode slotRKey = KeyCode.R;
    public KeyCode slotTKey = KeyCode.T;
    
    // 현재 교체 중인 스킬 아이템 (임시 저장)
    private DroppedSkillItem pendingSkillToSwap;

    // --- (임시: 테스트용) ---
    private void Start()
    {
        // 딕셔너리 초기화
        equippedSkills[slotQKey] = null;
        equippedSkills[slotEKey] = null;
        equippedSkills[slotRKey] = null;
        equippedSkills[slotTKey] = null;
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
                // StartCooldown(key, skillToUse.cooldown);
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
    
    // -------------------------------------------------------------------
    // -- 2/3단계: 스킬 교체 로직
    // -------------------------------------------------------------------

    /// <summary>
    /// (1) DroppedSkillItem이 F키를 누르면 호출할 함수
    /// </summary>
    public void StartSwapProcess(DroppedSkillItem itemToSwap)
    {
        pendingSkillToSwap = itemToSwap;
        
        // TODO: "Q, E, R, T 중 교체할 슬롯을 선택하세요" UI 활성화
        Debug.Log($"[SkillManager] 교체 시작: '{itemToSwap.GetSkillData().skillName}'. Q,E,R,T 중 선택...");
    }

    /// <summary>
    /// (2) 교체 UI가 뜬 상태에서 Q,E,R,T 입력을 감지
    /// </summary>
    private void HandleSwapInput()
    {
        if (Input.GetKeyDown(slotQKey)) FinalizeSwap(slotQKey);
        if (Input.GetKeyDown(slotEKey)) FinalizeSwap(slotEKey);
        if (Input.GetKeyDown(slotRKey)) FinalizeSwap(slotRKey);
        if (Input.GetKeyDown(slotTKey)) FinalizeSwap(slotTKey);

        // TODO: 교체 취소 (Esc 등) 로직 추가
    }

    /// <summary>
    /// (3) 특정 슬롯을 선택했을 때, 실제 교체 실행
    /// </summary>
    private void FinalizeSwap(KeyCode selectedKey)
    {
        if (pendingSkillToSwap == null) return;

        SkillData newSkill = pendingSkillToSwap.GetSkillData();
        SkillData oldSkill = equippedSkills[selectedKey]; // 기존에 장착되어 있던 스킬 (null일 수도 있음)

        // 1. 새 스킬을 슬롯에 장착
        equippedSkills[selectedKey] = newSkill;
        // TODO: UI 갱신 - selectedKey 슬롯의 아이콘을 newSkill.skillIcon으로 변경

        Debug.Log($"[SkillManager] {selectedKey} 슬롯에 '{newSkill.skillName}' 장착!");

        // 2. 기존 스킬이 있었다면 바닥에 드랍
        if (oldSkill != null)
        {
            Debug.Log($"[SkillManager] 기존 스킬 '{oldSkill.skillName}'을(를) 바닥에 드랍합니다.");
            
            // TODO: 'DroppedSkill_Prefab'을 바닥에 생성하고,
            // 그 프리팹의 Initialize(oldSkill)을 호출하는 로직 필요.
            // (이 로직은 DroppedSkillItem.cs의 Altar가 하던 것과 유사)
        }
        
        // 3. 교체가 완료되었으므로, 바닥에 있던 아이템 파괴
        Destroy(pendingSkillToSwap.gameObject);
        pendingSkillToSwap = null;

        // TODO: 교체 선택 UI 닫기
    }
}
