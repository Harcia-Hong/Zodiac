using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Enemy 피격 이펙트 관리 클래스
/// 공격 타입에 따라 다른 피격 이펙트를 재생
/// VFX와 Prefab 모두 지원
/// </summary>
public class EnemyHitEffectManager : MonoBehaviour
{
    [Header("Normal Attack Hit Effect")]
    [Tooltip("기본 공격 피격 VFX")]
    public VisualEffect normalAttackHitVFX;

    [Tooltip("기본 공격 피격 Prefab")]
    public GameObject normalAttackHitPrefab;

    [Header("Flash Attack Hit Effect")]
    [Tooltip("FlashAttack 피격 VFX")]
    public VisualEffect flashAttackHitVFX;

    [Tooltip("FlashAttack 피격 Prefab")]
    public GameObject flashAttackHitPrefab;

    [Header("Effect Settings")]
    [Tooltip("Prefab 자동 삭제 시간 (ParticleSystem이 없을 경우)")]
    public float defaultDestroyTime = 3f;

    /// <summary>피격 이펙트 재생</summary>
    /// <param name="attackType">공격 타입</param>
    /// <param name="hitPosition">피격 위치</param>
    /// <param name="hitNormal">피격 표면의 법선 벡터 (이펙트 방향 조정용, 옵션)</param>
    public void PlayHitEffect(AttackType attackType, Vector3 hitPosition, Vector3 hitNormal = default)
    {
        switch (attackType)
        {
            case AttackType.NormalAttack:
                PlayEffect(normalAttackHitVFX, normalAttackHitPrefab, hitPosition, hitNormal, "일반 공격");
                break;

            case AttackType.FlashAttack:
                PlayEffect(flashAttackHitVFX, flashAttackHitPrefab, hitPosition, hitNormal, "FlashAttack");
                break;

            case AttackType.QSkill:
            case AttackType.ESkill:
            case AttackType.RSkill:
                Debug.LogWarning($"[EnemyHitEffectManager] {attackType} 피격 이펙트는 아직 구현되지 않았습니다.");
                break;

            default:
                Debug.LogWarning($"[EnemyHitEffectManager] 알 수 없는 공격 타입: {attackType}");
                break;
        }
    }

    /// <summary>VFX 또는 Prefab 이펙트 재생 (내부 로직)</summary>
    /// <param name="vfx">재생할 VFX (null 가능)</param>
    /// <param name="prefab">생성할 Prefab (null 가능)</param>
    /// <param name="position">이펙트 위치</param>
    /// <param name="normal">표면 법선 벡터</param>
    /// <param name="effectName">디버그용 이펙트 이름</param>
    private void PlayEffect(VisualEffect vfx, GameObject prefab, Vector3 position, Vector3 normal, string effectName)
    {
        // VFX가 할당되어 있으면 VFX 재생
        if (vfx != null)
        {
            PlayVFXEffect(vfx, position, effectName);
        }
        // Prefab이 할당되어 있으면 Prefab 생성
        else if (prefab != null)
        {
            SpawnPrefabEffect(prefab, position, normal, effectName);
        }
        else
        {
            Debug.LogWarning($"[EnemyHitEffectManager] {effectName} 피격 이펙트가 할당되지 않았습니다.");
        }
    }

    /// <summary>VFX 이펙트 재생</summary>
    /// <param name="vfx">재생할 VFX</param>
    /// <param name="position">재생 위치</param>
    /// <param name="effectName">디버그용 이펙트 이름</param>
    private void PlayVFXEffect(VisualEffect vfx, Vector3 position, string effectName)
    {
        // VFX 위치 설정 (VFX가 월드 좌표계를 사용하는 경우)
        vfx.transform.position = position;
        vfx.SendEvent("OnPlay");

        Debug.Log($"[EnemyHitEffectManager] {effectName} VFX 재생 at {position}");
    }

    /// <summary>Prefab 이펙트 생성 및 자동 삭제</summary>
    /// <param name="prefab">생성할 Prefab</param>
    /// <param name="position">생성 위치</param>
    /// <param name="normal">표면 법선 벡터 (회전 계산용)</param>
    /// <param name="effectName">디버그용 이펙트 이름</param>
    private void SpawnPrefabEffect(GameObject prefab, Vector3 position, Vector3 normal, string effectName)
    {
        // 이펙트 회전 계산 (법선 벡터가 제공된 경우)
        Quaternion rotation = Quaternion.identity;
        if (normal != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(normal);
        }

        // Prefab 인스턴스화
        GameObject effectInstance = Instantiate(prefab, position, rotation);

        Debug.Log($"[EnemyHitEffectManager] {effectName} Prefab 생성 at {position}");

        // 파티클 시스템 자동 삭제 처리
        DestroyEffectAfterDuration(effectInstance);
    }

    /// <summary>이펙트 오브젝트 자동 삭제 처리</summary>
    /// <param name="effectInstance">삭제할 이펙트 인스턴스</param>
    private void DestroyEffectAfterDuration(GameObject effectInstance)
    {
        ParticleSystem particleSystem = effectInstance.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            // 파티클 시스템의 duration + lifetime에 맞춰 자동 삭제
            float destroyTime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            Destroy(effectInstance, destroyTime);
        }
        else
        {
            // 파티클 시스템이 없으면 기본 시간 후 삭제
            Destroy(effectInstance, defaultDestroyTime);
            Debug.LogWarning("[EnemyHitEffectManager] Prefab에 ParticleSystem이 없습니다. 기본 시간 후 자동 삭제됩니다.");
        }
    }
}
