using DataSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackConfig", menuName = "Attack/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    public Constants.AttackType attackType;  // ✅ 공격 타입 (Projectile, Area 등)
    public Sprite attackIcon;      // ✅ 아이콘
    public float attackDelay;      // ✅ 공격이 나갈 때까지의 간격
    public float recoveryTime;     // ✅ 공격 후 이동 가능 시간
    public string animParameter;
    public GameObject Prefab;
    public GameObject explosionEffectPrefab; // ✅ 공격별 파티클 프리팹
    public Constants.SkillType skillType;
    public BuffData appliedBuff;
}
