using DataSystem;
using Player;
using UnityEngine;

[System.Serializable]
public abstract class MovementSkillBase
{
    public abstract Constants.SkillType SkillType { get; } // ✅ 네트워크에서 사용 가능하도록 추가
    public abstract float Cooldown { get; }
    public abstract float CastTime { get; }
    public abstract float MoveDuration { get; }
    public abstract float EndTime { get; }

    public abstract Vector3 GetTargetPosition(PlayerCharacter player, Vector3 target);
    
    public virtual Sprite SkillIcon => MovementSkillFactory.GetSkillIcon(SkillType);
}