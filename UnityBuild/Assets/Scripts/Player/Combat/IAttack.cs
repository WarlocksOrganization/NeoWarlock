using DataSystem.Database;
using UnityEngine;

namespace Player.Combat
{
    public interface IAttack
    {
        float CooldownTime { get; }
        float LastUsedTime { get; set; }
        bool IsReady();
        Database.AttackData GetAttackData();
        void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner);
    }
}