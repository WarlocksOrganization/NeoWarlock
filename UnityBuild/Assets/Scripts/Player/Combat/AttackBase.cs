using DataSystem.Database;
using UnityEngine;

namespace Player.Combat
{
    public class AttackBase : MonoBehaviour, IAttack
    {
        public GameObject projectilePrefab; // ✅ 모든 공격에서 사용할 공통 변수
        protected Database.AttackData attackData; // ✅ 공격 데이터 저장

        public float CooldownTime => attackData?.Cooldown ?? 1f;
        public float LastUsedTime { get; set; }

        public void Initialize(Database.AttackData data)
        {
            attackData = data;
            LastUsedTime = -9999;
        }

        public virtual void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        {
            Debug.Log($"{attackData.Name} 공격 실행!");
        }

        public Database.AttackData GetAttackData()
        {
            return attackData;
        }

        public bool IsReady()
        {
            return Time.time >= LastUsedTime + CooldownTime;
        }
    }
}