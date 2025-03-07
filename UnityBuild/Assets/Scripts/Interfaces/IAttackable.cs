using Player.Combat;
using UnityEngine;

namespace Interfaces
{
    public interface IAttackable
    {
        public void Attack(Vector3 targetPosition);
    }
}
