using UnityEngine;

/// <summary>
/// 스킬 등급 (추후 확률에 사용)
/// </summary>
public enum SkillGrade { Common, Rare, Epic, Legendary }

/// <summary>
/// 스킬 1개의 모든 데이터를 정의하는 ScriptableObject 템플릿.
/// </summary>
[CreateAssetMenu(fileName = "SD_", menuName = "Skill System/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;        // 스킬 이름 (예: "검기 발사", "화염구")
    public Sprite skillIcon;        // UI 슬롯에 표시될 아이콘
    [TextArea(3, 5)]
    public string description;      // 스킬 설명

    [Header("성능")]
    public float cooldown;          // 쿨타임 (초)
    public SkillGrade grade;        // 스킬 등급

    [Header("피격 효과 (중요)")]
    // 이 스킬이 적에게 적중 시 어떤 피격 이펙트를 쓸지
    public HitEffectType hitEffect;

    [Header("실행 로직 (핵심)")]
    // 이 스킬이 발동될 때, 씬에 생성되어 실제 로직을 수행할 프리팹
    // (이 프리팹 안에는 ISkillLogic 인터페이스를 구현한 스크립트가 있어야 함)
    public GameObject logicPrefab;
}
