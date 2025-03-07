using UnityEngine;
using DataSystem;
using System.Collections.Generic;

public static class MovementSkillFactory
{
    private static Dictionary<Constants.SkillType, Sprite> skillIcons = new Dictionary<Constants.SkillType, Sprite>();

    static MovementSkillFactory()
    {
        LoadSkillIcons();
    }

    private static void LoadSkillIcons()
    {
        skillIcons[Constants.SkillType.TelePort] = Resources.Load<Sprite>("Sprites/MoveIcons/teleport_icon");
    }

    public static MovementSkillBase GetMovementSkill(Constants.SkillType skillType)
    {
        switch (skillType)
        {
            case Constants.SkillType.TelePort:
                return new TeleportSkill();
            /*case Constants.SkillType.Fire:
                return new FireSkill();
            case Constants.SkillType.Thunder:
                return new ThunderSkill();
            case Constants.SkillType.Ice:
                return new IceSkill();
            case Constants.SkillType.Meteor:
                return new MeteorSkill();*/
            default:
                return null;
        }
    }

    public static Sprite GetSkillIcon(Constants.SkillType skillType)
    {
        return skillIcons.ContainsKey(skillType) ? skillIcons[skillType] : null;
    }
}