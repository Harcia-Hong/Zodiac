using UnityEngine;

/**
 * Enemy 데이터 ScriptableObject
 * 각 Enemy 타입별 설정값을 에셋으로 관리
 * Inspector에서 쉽게 조정 가능하고, 런타임에 변경되지 않음
 */
[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/Enemy Data")]
public class EnemySO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("적의 이름 (디버깅용)")]
    public string enemyName = "Basic Enemy";

    [Tooltip("적 타입 (Standard/Charge/Range)")]
    public EnemyType enemyType = EnemyType.Standard;

    [Header("이동 설정")]
    [Tooltip("기본 이동 속도")]
    public float baseSpeed = 3.5f;

    [Tooltip("회전 속도 (높을수록 빠른 회전)")]
    public float baseRotationDamping = 10f;

    [Header("탐지 설정")]
    [Tooltip("플레이어 탐지 범위")]
    public float detectRadius = 10f;

    [Tooltip("플레이어 추적 범위 (탐지보다 약간 넓어야 함)")]
    public float chasingRange = 15f;

    [Header("전투 설정")]
    [Tooltip("공격 사거리")]
    public float attackRange = 2f;

    [Tooltip("공격력")]
    public int damage = 10;

    [Tooltip("공격 시 가하는 넉백 힘")]
    public float attackForce = 5f;

    [Tooltip("공격 쿨타임")]
    public float attackCooldown = 1.5f;

    [Header("체력 설정")]
    [Tooltip("최대 체력")]
    public int maxHealth = 100;

    [Header("애니메이션 타이밍")]
    [Tooltip("공격 애니메이션 중 실제 힘이 가해지는 시점 (0~1)")]
    public float forceTransitionTime = 0.3f;

    [Tooltip("데미지 판정 시작 시점 (0~1)")]
    public float dealingStartTransitionTime = 0.5f;

    [Tooltip("데미지 판정 종료 시점 (0~1)")]
    public float dealingEndTransitionTime = 0.8f;

    /// <summary>
    /// 데이터 유효성 검증
    /// </summary>
    private void OnValidate()
    {
        // 음수값 방지
        baseSpeed = Mathf.Max(0.1f, baseSpeed);
        baseRotationDamping = Mathf.Max(0.1f, baseRotationDamping);
        detectRadius = Mathf.Max(1f, detectRadius);
        chasingRange = Mathf.Max(detectRadius, chasingRange);
        attackRange = Mathf.Max(0.5f, attackRange);
        damage = Mathf.Max(1, damage);
        maxHealth = Mathf.Max(1, maxHealth);

        // 타이밍 값들을 0~1 범위로 제한
        forceTransitionTime = Mathf.Clamp01(forceTransitionTime);
        dealingStartTransitionTime = Mathf.Clamp01(dealingStartTransitionTime);
        dealingEndTransitionTime = Mathf.Clamp01(dealingEndTransitionTime);
    }
}
