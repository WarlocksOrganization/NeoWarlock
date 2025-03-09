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
        skillIcons[Constants.SkillType.Roll] = Resources.Load<Sprite>("Sprites/MoveIcons/roll_icon");
    }

    public static MovementSkillBase GetMovementSkill(Constants.SkillType skillType)
    {
        switch (skillType)
        {
            case Constants.SkillType.TelePort:
                return new TeleportSkill();
            case Constants.SkillType.Roll:
                return new RollSkill();
            default:
                return null;
        }
    }

    public static Sprite GetSkillIcon(Constants.SkillType skillType)
    {
        return skillIcons.ContainsKey(skillType) ? skillIcons[skillType] : null;
    }
}