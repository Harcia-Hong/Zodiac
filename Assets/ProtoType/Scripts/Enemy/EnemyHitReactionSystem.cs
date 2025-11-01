using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Normal 몬스터 전용 피격 시스템
/// - 매 피격마다 짧은 경직
/// - 피격 애니메이션 재생
/// - FSM 행동 중단
/// </summary>
public class EnemyHitReactionSystem : MonoBehaviour
{
    [Header("피격 설정")]
    [SerializeField, Tooltip("피격 경직 시간")]
    private float hitStunDuration = 0.3f;

    [Header("시각 효과")]
    [SerializeField, Tooltip("피격 시 색상")]
    private Color hitColor = Color.red;

    // 컴포넌트 참조
    private Enemy enemyScript;
    private Renderer enemyRenderer;
    private Material originalMaterial;
    private NavMeshAgent navAgent;
    private Animator anim;
    private Color originalColor;

    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>
    /// 컴포넌트 참조 초기화
    /// </summary>
    private void InitializeComponents()
    {
        enemyScript = GetComponent<Enemy>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        navAgent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
            originalColor = originalMaterial.color;
        }
    }

    /// <summary>
    /// 피격 처리 메인 메서드
    /// Enemy.cs의 TakeDamage에서 호출
    /// </summary>
    /// <param name="damage">받은 데미지 (현재는 미사용)</param>
    public void AddDamage(float damage)
    {
        // 모든 행동 즉시 중단
        ForceStopCurrentAction();

        // 피격 애니메이션 재생
        PlayHitAnimation();

        // 시각적 피드백
        StartCoroutine(HitColorFeedback());

        // 경직 시간 동안 Idle 상태 유지
        ApplyHitStun(hitStunDuration);

        Debug.Log($"[{gameObject.name}] 피격! {hitStunDuration}초 경직");
    }

    /// <summary>
    /// 현재 진행 중인 모든 행동 강제 중단
    /// </summary>
    private void ForceStopCurrentAction()
    {
        // 1. NavMeshAgent 이동 중단
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
            Debug.Log($"[{gameObject.name}] NavMeshAgent 이동 중단");
        }

        // 2. 모든 공격 컴포넌트 중단 (IEnemyAttack 인터페이스 활용)
        IEnemyAttack[] attackComponents = GetComponents<IEnemyAttack>();

        if (attackComponents.Length > 0)
        {
            foreach (var attackComponent in attackComponents)
            {
                if (attackComponent != null && attackComponent.IsAttacking())
                {
                    attackComponent.StopAttack();
                    Debug.Log($"[{gameObject.name}] 공격 중단: {attackComponent.GetType().Name}");
                }
            }
        }
    }

    /// <summary>
    /// 피격 애니메이션 재생
    /// </summary>
    private void PlayHitAnimation()
    {
        if (anim != null)
        {
            anim.SetTrigger("doHit");
        }
    }

    /// <summary>
    /// 피격 시 색상 변화 효과
    /// </summary>
    private IEnumerator HitColorFeedback()
    {
        if (enemyRenderer != null && originalMaterial != null)
        {
            // 빨간색으로 변경
            originalMaterial.color = hitColor;

            yield return new WaitForSeconds(0.1f);

            // 원래 색으로 복귀
            originalMaterial.color = originalColor;
        }
    }

    /// <summary>
    /// 경직 적용 - FSM을 Idle 상태로 전환
    /// </summary>
    /// <param name="duration">경직 시간</param>
    private void ApplyHitStun(float duration)
    {
        if (enemyScript != null)
        {
            enemyScript.SetTemporaryIdle(duration);
        }
    }

    // =============================================================================
    // 공개 API (디버그/테스트용)
    // =============================================================================

    /// <summary>
    /// 에디터용: 피격 테스트
    /// </summary>
    [ContextMenu("테스트: 피격 효과")]
    public void TestHitReaction()
    {
        AddDamage(10);
    }
}
