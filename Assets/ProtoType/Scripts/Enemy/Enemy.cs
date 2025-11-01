using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

/**
 * Enemy 메인 컨트롤러 - FSM 기반으로 완전 리팩토링
 * - AI 로직은 모두 EnemyStateMachine에 위임
 * - 컴포넌트 관리와 데이터만 담당
 * - 기존 코드와의 호환성 유지
 */
public class Enemy : MonoBehaviour
{
    [Header("Enemy Data")]
    [SerializeField] private EnemySO enemyData;
    public EnemySO Data => enemyData; // StateMachine에서 접근용

    [Header("Enemy Type (기존 호환성)")]
    public EnemyType enemyType = EnemyType.Standard;
    public EnemyRank enemyRank = EnemyRank.Normal;

    [Header("Animation Data")]
    [SerializeField] private EnemyAnimationData animationData;
    public EnemyAnimationData AnimationData => animationData; // StateMachine에서 접근용

    [Header("UI References")]
    public GameObject hpUIPrefab;
    public GameObject damageTextPrefab;
    public Transform hpAnchor;

    // 컴포넌트 참조들 (public으로 StateMachine에서 접근 가능)
    public Rigidbody rigid { get; private set; }
    public BoxCollider boxCollider { get; private set; }
    public Material mat { get; private set; }
    public NavMeshAgent nav { get; private set; }
    public Animator animator { get; private set; }
    public PlayerHealth health { get; private set; }

    // UI 관련
    private GameObject hpUIInstance;
    private Slider hpSlider;

    // FSM 핵심
    private EnemyStateMachine stateMachine;

    // 기존 코드와의 호환성을 위한 상태 변수들
    public bool isHit = false;
    public bool isGroggy = false;
    public bool isDead = false;

    // 이벤트 및 콜백
    public static event System.Action OnEnemyDeath;
    private System.Action deathCallback;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.3f;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
        InitializeStateMachine();
    }

    private void Start()
    {
        InitializeUI();
        RegisterToManager();

        // FSM 시작 - Idle 상태로 시작
        stateMachine.ChangeState(stateMachine.IdleState);
    }

    private void Update()
    {
        // FSM 업데이트 (핵심!)
        stateMachine.HandleInput();
        stateMachine.Update();

        // UI 업데이트
        UpdateHPUI();

        // 기존 호환성을 위한 처리
        UpdateCompatibilityFlags();
    }

    private void FixedUpdate()
    {
        // FSM 물리 업데이트
        stateMachine.PhysicsUpdate();

        // 기존 물리 처리
        FreezeVelocity();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 필수 컴포넌트들
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        nav = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<PlayerHealth>();

        // 렌더러에서 머티리얼 가져오기
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            mat = renderer.material;
        }

        // 애니메이션 데이터 초기화
        if (animationData != null)
        {
            animationData.Initialize();
        }
        else
        {
            Debug.LogWarning($"[Enemy] {name}에 EnemyAnimationData가 설정되지 않음");
        }

        // 데이터 검증
        if (enemyData == null)
        {
            Debug.LogWarning($"[Enemy] {name}에 EnemySO 데이터가 설정되지 않음");
        }
    }

    /// <summary>
    /// StateMachine 초기화
    /// </summary>
    private void InitializeStateMachine()
    {
        stateMachine = new EnemyStateMachine(this);
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // HP UI 생성 (기존 로직 유지)
        if (hpUIPrefab != null && hpAnchor != null)
        {
            hpUIInstance = Instantiate(hpUIPrefab);
            hpSlider = hpUIInstance.GetComponentInChildren<Slider>();

            if (hpSlider != null && health != null)
            {
                hpSlider.maxValue = health.maxHealth;
                hpSlider.value = health.curHealth;
            }
        }
    }

    /// <summary>
    /// EnemyManager에 등록
    /// </summary>
    private void RegisterToManager()
    {
        EnemyManager.Register(this);
    }

    #endregion

    #region FSM Public Interface

    /// <summary>
    /// 외부에서 강제로 상태 변경 (기존 호환성)
    /// </summary>
    public void ForceSetState(EnemyState newState)
    {
        switch (newState)
        {
            case EnemyState.Idle:
                stateMachine.ChangeState(stateMachine.IdleState);
                break;
            case EnemyState.Trace:
                stateMachine.ChangeState(stateMachine.ChasingState);
                break;
            case EnemyState.Attack:
                stateMachine.ChangeState(stateMachine.AttackState);
                break;
                // Hit, Die 등은 별도 처리
        }
    }

    #endregion

    #region Health & Damage System

    /// <summary>
    /// 데미지 받기 (기존 호환성 유지)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        if (other.tag == "Melee")
        {
            Weapon weapon = other.GetComponent<Weapon>();
            int damage = weapon != null ? weapon.damage : 10;

            Vector3 reactVec = transform.position - other.transform.position;
            reactVec.y = 0f;

            TakeDamage(damage, reactVec);
            lastHitTime = Time.time;
        }
    }

    /// <summary>
    /// 데미지 처리 - EnemyHitReactionSystem과 연동
    /// </summary>
    private void TakeDamage(int damage, Vector3 reactVec)
    {
        if (health != null)
        {
            // 체력 감소
            health.TakeDamage(damage);

            if (health.isDead)
            {
                // 죽음 처리
                HandleDeath(reactVec);
            }
            else
            {
                // EnemyHitReactionSystem에 데미지 전달 (FSM 연동 피격 처리)
                var hitReactionSystem = GetComponent<EnemyHitReactionSystem>();
                if (hitReactionSystem != null)
                {
                    hitReactionSystem.AddDamage(damage);
                }
                else
                {
                    // HitReactionSystem이 없으면 기본 피격 처리
                    StartCoroutine(OnDamage(reactVec, damage));
                }
            }
        }
    }

    /// <summary>
    /// 기본 피격 반응 처리 (HitReactionSystem이 없을 때 폴백)
    /// </summary>
    private IEnumerator OnDamage(Vector3 reactVec, int damage)
    {
        isHit = true;

        // 넉백 효과
        if (rigid != null)
        {
            rigid.AddForce(reactVec.normalized * 5, ForceMode.Impulse);
        }

        // 기본 피격 시간
        yield return new WaitForSeconds(0.5f);

        isHit = false;
    }

    /// <summary>
    /// 죽음 처리
    /// </summary>
    private void HandleDeath(Vector3 reactVec)
    {
        if (isDead) return;

        isDead = true;

        // 충돌체 비활성화
        if (boxCollider != null) boxCollider.enabled = false;

        // 시각적 효과
        if (mat != null) mat.color = Color.gray;
        gameObject.layer = 10;

        // AI 비활성화
        if (nav != null) nav.enabled = false;

        // 죽음 애니메이션
        if (animator != null) animator.SetTrigger("doDie");

        // 물리 효과
        if (rigid != null)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
            rigid.isKinematic = false;
            rigid.useGravity = true;
            rigid.AddForce(reactVec.normalized * 5, ForceMode.Impulse);
        }

        // 콜백 호출
        deathCallback?.Invoke();
        OnEnemyDeath?.Invoke();

        // 정리 작업
        CleanupOnDeath();

        // 4초 후 제거
        Destroy(gameObject, 4f);
    }

    #endregion

    #region Compatibility & Helper Methods

    /// <summary>
    /// 기존 코드와의 호환성을 위한 플래그 업데이트
    /// </summary>
    private void UpdateCompatibilityFlags()
    {
        // 필요에 따라 추가 플래그들 업데이트
    }

    /// <summary>
    /// 물리 속도 고정 (기존 로직 유지)
    /// </summary>
    private void FreezeVelocity()
    {
        // 추적 중일 때만 속도 고정 (FSM에서는 불필요할 수 있지만 호환성 유지)
        /*if (rigid != null && stateMachine.CurrentState is EnemyChasingState)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }*/

        if (rigid != null && !isDead)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// HP UI 업데이트
    /// </summary>
    private void UpdateHPUI()
    {
        if (hpUIInstance != null && hpAnchor != null)
        {
            hpUIInstance.transform.position = hpAnchor.position;
            hpUIInstance.transform.rotation = Camera.main.transform.rotation;

            if (hpSlider != null && health != null)
            {
                hpSlider.value = health.curHealth;
            }
        }
    }

    /// <summary>
    /// 죽음 시 정리 작업
    /// </summary>
    private void CleanupOnDeath()
    {
        if (hpUIInstance != null)
        {
            Destroy(hpUIInstance);
        }

        EnemyManager.Unregister(this);
    }

    /// <summary>
    /// 죽음 콜백 설정 (MonsterSpawner에서 호출)
    /// </summary>
    public void SetDeathCallback(System.Action callback)
    {
        deathCallback = callback;
    }

    /// <summary>
    /// 스킬 데미지 받기
    /// </summary>
    public void ApplySkillDamage(int damage)
    {
        Vector3 reactVec = stateMachine.Target != null
            ? transform.position - stateMachine.Target.position
            : -transform.forward;
        reactVec.y = 0f;

        TakeDamage(damage, reactVec);
    }

    #endregion

    #region 기존 호환성을 위한 메서드들

    /// <summary>
    /// 일정 시간 동안 강제로 Idle 상태 유지 (기존 호환성)
    /// </summary>
    /// <param name="idleDuration">대기 시간</param>
    public void SetTemporaryIdle(float idleDuration)
    {
        StopAllCoroutines();
        StartCoroutine(IdleAfterDelay(idleDuration));
    }

    /// <summary>
    /// 지정된 시간 후 추적 상태로 복귀
    /// </summary>
    /// <param name="duration">대기 시간</param>
    private IEnumerator IdleAfterDelay(float duration)
    {
        // 강제로 Idle 상태로 변경
        stateMachine.ChangeState(stateMachine.IdleState);

        yield return new WaitForSeconds(duration);

        // 플레이어가 탐지 범위에 있으면 추적 재개
        if (stateMachine.IsPlayerInDetectRange())
        {
            stateMachine.ChangeState(stateMachine.ChasingState);
        }
    }

    /// <summary>
    /// 슬로우 모션 효과 (기존 호환성)
    /// </summary>
    /// <param name="isSlow">슬로우 모션 적용 여부</param>
    public void SetSlowMotion(bool isSlow)
    {
        if (isSlow)
        {
            if (animator != null) animator.speed = 0.2f;
            if (nav != null) nav.speed *= 0.2f;
        }
        else
        {
            if (animator != null) animator.speed = 1f;
            if (nav != null) nav.speed *= 5f;
        }
    }

    #endregion
}
