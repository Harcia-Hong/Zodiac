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

    private void Awake()
    {
        if (hitBox != null) hitBox.enabled = false;
    }

    /// <summary>
    /// 기본기(지상/공중/제자리) 시작: 메시 트레일을 켠다.
    /// </summary>
    public void EnableHitBox()
    {
        if (hitBox != null) hitBox.enabled = true;
    }

    /// <summary>
    /// 공격 종료/캔슬: 전부 끈다.
    /// </summary>
    public void DisableHitBox()
    {
        if (hitBox != null) hitBox.enabled = false;
    }

    public int GetDamage() => damage;
}
