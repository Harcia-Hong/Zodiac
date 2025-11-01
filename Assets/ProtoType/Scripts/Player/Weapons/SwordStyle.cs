using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordStyle", menuName = "Combat/SwordStyle")]
public class SwordStyle : ScriptableObject
{
    public string styleName;

    [Header("Combo Settings")]
    public List<string> comboTriggers;
    public List<float> forwardDistances;

    [Header("Skill Settings")]
    public string skillQTrigger;
    public GameObject skillEffectPrefab;

    [Header("Etc")]
    public RuntimeAnimatorController animatorOverride;

    [Header("UI")]
    public Sprite iconSprite;
    public List<Sprite> skillSprites;

    [Header("VFX Settings")]
    public GameObject[] comboSlashEffects;
    public Color slashColor = Color.white;
    public float effectScale = 1f;

}
