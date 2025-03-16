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
    

    public virtual Vector3 GetTargetPosition(Vector3 playerPosition, Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - playerPosition).normalized;
        float distanceToTarget = Vector3.Distance(playerPosition, targetPosition);
        float moveDistance = Mathf.Min(distanceToTarget, maxDistance);
        return playerPosition + direction * moveDistance;
    }
}