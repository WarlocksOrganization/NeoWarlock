using DataSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackConfig", menuName = "Attack/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    public Constants.AttackType attackType;  // ✅ 공격 타입 (Projectile, Area 등)
    public float attackDelay;      // ✅ 공격이 나갈 때까지의 간격
    public float recoveryTime;     // ✅ 공격 후 이동 가능 시간
    public float attackDuration;   // ✅ 공격 지속시간 (공격이 유지되는 시간)
    public float attackInterval;   // ✅ 공격 주기 (공격이 반복되는 간격)
    public string animParameter;
    public Vector3 attackTrans;
    public GameObject Prefab;
    public GameObject explosionEffectPrefab; // ✅ 공격별 파티클 프리팹
    public GameObject particlePrefab;
    public GameObject particlePrefab2;
    public Constants.SkillType skillType;
    public BuffData appliedBuff;
}
