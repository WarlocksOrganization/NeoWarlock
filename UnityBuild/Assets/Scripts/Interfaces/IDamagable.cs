using UnityEngine;

namespace Interfaces
{
    public interface IDamagable
    {
        public void takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid);
    }
}
