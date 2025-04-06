using DataSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Buff System/Buff Data")]
public class BuffData : ScriptableObject
{
    public string buffName;
    public Constants.BuffType BuffType;

    public float duration;
    public float moveSpeedModifier;
    public float attackDamageModifier;
    public float defenseModifier;
    public float knonkbackModifier;
    public int tickDamage;
    public Vector3 moveDirection;

    // ✅ UI 표시용
    public Sprite buffIcon;
    public string displayName;
    [TextArea]
    public string description;
}
