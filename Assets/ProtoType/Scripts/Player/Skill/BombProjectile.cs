using UnityEngine;

/// <summary>
/// 실제로 날아가서 터지는 폭탄 오브젝트의 로직
/// </summary>
public class BombProjectile : MonoBehaviour
{
    private Vector3 targetPosition;
    private float speed;
    private int damage;
    private float explosionRadius;
    private HitEffectType hitEffectType;

    [Tooltip("폭발 시 생성될 이펙트 프리팹")]
    public GameObject explosionVFX;

    private bool isInitialized = false;

    /// <summary>
    /// BombThrowLogic에서 호출되어 폭탄의 정보를 설정
    /// </summary>
    public void Initialize(Vector3 target, float moveSpeed, int dmg, float radius, HitEffectType effectType)
    {
        targetPosition = target;
        speed = moveSpeed;
        damage = dmg;
        explosionRadius = radius;
        hitEffectType = effectType;
        isInitialized = true;

        // Y값을 무시하고 XZ 평면상의 거리만 계산하여 이동 시간 추정
        // (포물선 로직은 복잡하므로, 여기서는 직선 이동으로 단순화)
        float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(target.x, 0, target.z));
        float estimatedTime = distance / speed;

        // 일정 시간 후 폭발하도록 예약
        // (DOTween이나 포물선 계산을 쓰면 더 좋지만, 지금은 Invoke로 단순화)
        Invoke(nameof(Explode), estimatedTime);
    }

    void Update()
    {
        if (!isInitialized) return;

        // 목표 지점을 향해 직선으로 이동 (단순화된 로직)
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    /// <summary>
    /// 목표 지점에 도달했거나 시간이 다 되면 폭발
    /// </summary>
    private void Explode()
    {
        // 폭발 이펙트 생성
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // 폭발 범위 내의 모든 적 찾기
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            // Enemy.cs가 있는지 확인
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && !enemy.isDead)
            {
                // 적에게 범위 데미지와 피격 효과 전달
                enemy.ApplySkillDamage(damage, hitEffectType);
            }
        }

        // 폭탄 오브젝트 파괴
        Destroy(gameObject);
    }

    // 디버그용: 폭발 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
