using System.Collections;
using DataSystem;
using Player;
using UnityEngine;

public class RollSkill : MovementSkillBase
{
    private float maxDistance = 5f;

    public override  float Cooldown => 5f;
    public override  float CastTime => 0f;
    public override  float MoveDuration => 0.5f;
    public override  float EndTime => 0.5f;

    public override  Constants.SkillType SkillType => Constants.SkillType.Roll;

    public override  Vector3 GetTargetPosition(PlayerCharacter player, Vector3 target)
    {
        Vector3 direction = (target - player.transform.position).normalized;
        float distanceToTarget = Vector3.Distance(player.transform.position, target);
        float moveDistance = Mathf.Min(distanceToTarget, maxDistance);
        return player.transform.position + direction * moveDistance;
    }
}
