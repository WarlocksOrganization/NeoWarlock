using UnityEngine;

[CreateAssetMenu(fileName = "DragonAttackData", menuName = "Boss/DragonAttackData", order = 0)]
public class DragonAttackConfig : ScriptableObject
{
    public string attackName;         // 공격 이름 (디버그용)
    public string animTrigger;        // 애니메이션 트리거
    public float attackDuration = 1f; // 전체 애니메이션 시간
    public float cooldown = 2f;       // 공격 쿨타임
    public float range = 2f;          // 공격 거리
    public float radius = 2f;
    public float speed = 10f;
    public int damage = 10;
    public float knockback = 1f;
    public AttackConfig config;
}