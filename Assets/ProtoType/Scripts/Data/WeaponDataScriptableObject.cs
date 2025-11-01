using UnityEngine;

/// <summary>
/// 무기 데이터 ScriptableObject
/// Create → Magic → WeaponData로 생성
/// </summary>
[CreateAssetMenu(fileName = "WeaponDataScriptableObject", menuName = "Scriptable Objects/WeaponDataScriptableObject")]
public class WeaponDataScriptableObject : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public WeaponType weaponType;

    [Header("UI Icons")]
    public Sprite weaponIcon;
    [Tooltip("Index 0: Q스킬 아이콘, Index 1: E스킬 아이콘")]
    public Sprite[] skillIcons = new Sprite[2];

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;

    [Header("Stats")]
    public float baseDamage = 10f;
    public float baseSpeed = 1f;
    public float baseRange = 2f;

    [Header("Special Settings")]
    [Tooltip("이 무기가 현재 사용 가능한지")]
    public bool isUnlocked = true;

    [Tooltip("무기 전환 시 추가 딜레이")]
    public float switchDelay = 0f;

    /// <summary>WeaponData로 변환</summary>
    public WeaponData ToWeaponData()
    {
        return new WeaponData
        {
            weaponName = this.weaponName,
            weaponType = this.weaponType,
            weaponIcon = this.weaponIcon,
            skillIcons = this.skillIcons,
            animatorController = this.animatorController,
            baseDamage = this.baseDamage,
            baseSpeed = this.baseSpeed,
            baseRange = this.baseRange,
            isUnlocked = this.isUnlocked,
            switchDelay = this.switchDelay
        };
    }
}
