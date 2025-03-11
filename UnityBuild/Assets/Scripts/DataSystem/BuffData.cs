using DataSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuff", menuName = "Buff System/Buff Data")]
public class BuffData : ScriptableObject
{
    public float duration;            // 버프 지속 시간
    public float moveSpeedModifier;   // 이동 속도 변경 값
    public float attackDamageModifier;// 공격력 변경 값
    public float defenseModifier;     // 방어력 변경 값
    public int tickDamage;          // ✅ 0.5초마다 입히는 지속 데미지
    public Vector3 moveDirection;     // 강제 이동 방향

    public Constants.BuffType BuffType;   // 적용되는 파티클 효과
}