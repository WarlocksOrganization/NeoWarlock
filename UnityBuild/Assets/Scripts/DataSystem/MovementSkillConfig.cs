using UnityEngine;
using DataSystem;

[CreateAssetMenu(fileName = "NewMovementSkill", menuName = "Skill/MovementSkill")]
public class MovementSkillConfig : ScriptableObject
{
    public Constants.SkillType skillType;
    public float cooldown;
    public float castTime;
    public float moveDuration;
    public float endTime;
    public float maxDistance;
    public Sprite skillIcon;
    public string skillName;
    public string Description;
}