using UnityEngine;
using UnityEngine.VFX;

public class Weapon : MonoBehaviour
{
    public enum WeaponType { Melee, Range };

    [Header("Weapon Setting")]
    public WeaponType weaponType = WeaponType.Melee;
    public int damage;
    public float attackRate;

    [Header("HitBox")]
    public BoxCollider hitBox;

    // ──────────────────────────────────────────────────────────────
    // 기본기: 메시 트레일(폴리곤) 사용
    [Header("Blade Trail (Basic Attacks)")]
    [SerializeField] private BladeTrailMesh bladeTrail;

    // 특수기(러시/이동형/스킬): VFX Graph 버스트 사용
    [Header("Slash VFX (Special Attacks)")]
    [SerializeField] private SlashVFXController slashVFX;
    [SerializeField] private SlashVFXController.Mode defaultMode = SlashVFXController.Mode.SwingTracked;
    // ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (hitBox != null) hitBox.enabled = false;

        if (bladeTrail == null) bladeTrail = GetComponentInChildren<BladeTrailMesh>(true);
        bladeTrail?.Disarm();     // 안전 정리

        if (slashVFX == null) slashVFX = GetComponentInChildren<SlashVFXController>(true);
        slashVFX?.Disarm();       // 안전 정리
    }

    /// <summary>
    /// 기본기(지상/공중/제자리) 시작: 메시 트레일을 켠다.
    /// </summary>
    public void EnableHitBox()
    {
        if (hitBox != null) hitBox.enabled = true;

        // 기본기는 폴리곤 트레일만 사용
        bladeTrail?.Arm();
        // 혹시 특수 VFX가 남아있으면 끈다
        slashVFX?.Disarm();
    }

    /// <summary>
    /// 특수기(러시/이동형 스핀/스킬) 시작: DirectionalBurst 등 VFX 사용.
    /// </summary>
    public void EnableHitBox(SlashVFXController.Mode mode, Vector3? fixedDirection = null)
    {
        if (hitBox != null) hitBox.enabled = true;

        // 특수기 동안엔 메시 트레일은 사용하지 않음
        bladeTrail?.Disarm();
        slashVFX?.Arm(mode, fixedDirection);
    }

    /// <summary>
    /// 공격 종료/캔슬: 전부 끈다.
    /// </summary>
    public void DisableHitBox()
    {
        if (hitBox != null) hitBox.enabled = false;

        bladeTrail?.Disarm();
        slashVFX?.Disarm();
    }

    public int GetDamage() => damage;
}
