using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어와 상호작용 가능한 모든 보상 오브젝트의 기본 클래스.
/// (스킬, 스탯, 강화 등)
/// </summary>
public class InteractableRewardObject : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField, Tooltip("상호작용 키")]
    private KeyCode interactKey = KeyCode.F;

    [SerializeField, Tooltip("오브젝트 이름")]
    private string objectName = "테스트 오브젝트";

    [Header("UI 참조")]
    [SerializeField, Tooltip("상호작용 프롬프트 UI ( F키로 작동 )")]
    private GameObject interactionPromptUI; // UI 오브젝트 (Canvas 등)

    [SerializeField, Tooltip("오브젝트 이름 텍스트")]
    private TextMeshProUGUI nameTextUI; // TMP 텍스트 컴포넌트

    [Header("보상 설정")]
    [SerializeField, Tooltip("상호작용 시 드랍할 아이템 프리팹")]
    private GameObject rewardItemPrefab;

    [SerializeField, Tooltip("아이템 드랍 위치")]
    private Transform spawnPoint;

    // 상태 변수
    private bool isPlayerInRange = false;
    private bool isInteracted = false; // 이미 상호작용했는지 여부

    private void Awake()
    {
        // UI가 꺼진 상태로 시작
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);

        // 스폰 위치가 없으면 자신을 기준으로
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    /// <summary>
    /// 플레이어가 감지 범위에 들어왔을 때
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 이미 상호작용했거나 플레이어가 아니면 무시
        if (isInteracted || !other.CompareTag("Player"))
            return;

        isPlayerInRange = true;
        ShowInteractionUI(true);
    }

    /// <summary>
    /// 플레이어가 감지 범위를 벗어났을 때
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (isInteracted || !other.CompareTag("Player"))
            return;

        isPlayerInRange = false;
        ShowInteractionUI(false);
    }

    /// <summary>
    /// F키 입력 감지
    /// </summary>
    private void Update()
    {
        // 범위 안에 있고, 아직 상호작용 안 했고, F키를 눌렀을 때
        if (isPlayerInRange && !isInteracted && Input.GetKeyDown(interactKey))
        {
            OnInteract();
        }
    }

    /// <summary>
    /// 상호작용 UI 표시/숨기기
    /// </summary>
    private void ShowInteractionUI(bool show)
    {
        if (interactionPromptUI != null)
        {
            // 이름 텍스트가 있다면 업데이트
            if (nameTextUI != null && show)
            {
                nameTextUI.text = objectName;
            }

            interactionPromptUI.SetActive(show);
        }
    }

    /// <summary>
    /// 상호작용 실행 (F키 눌렀을 때)
    /// </summary>
    private void OnInteract()
    {
        isInteracted = true;
        isPlayerInRange = false; // F키 누르면 범위 밖으로 나간 것처럼 처리

        // 1. UI 숨기기
        ShowInteractionUI(false);

        // 2. 보상 드랍 (스킬 드랍 유형)
        if (rewardItemPrefab != null)
        {
            SpawnRewardItem();
        }
        // [TODO] 나중에 여기에 스탯/강화 UI를 띄우는 로직 추가

        // 3. 오브젝트 비활성화 (예: 부서지는 애니메이션 재생)
        // 지금은 간단하게 메시 렌더러를 끕니다.
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        // 콜라이더도 꺼서 다시는 상호작용 안 되게
        GetComponent<Collider>().enabled = false;

        Debug.Log($"[{objectName}] 상호작용! 보상을 드랍합니다.");
    }

    /// <summary>
    /// 보상 아이템 스폰
    /// </summary>
    private void SpawnRewardItem()
    {
        Instantiate(rewardItemPrefab, spawnPoint.position, Quaternion.identity);
    }
}
