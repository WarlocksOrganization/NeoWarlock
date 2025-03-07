using DataSystem.Database;
using Player.Combat;
using UnityEngine;

namespace Player
{
    public class LobbyPlayerCharacter : PlayerCharacter
    {
        public override void Start()
        {
            
            base.Start();

            if (isOwned)
            {
                SetAvailableAttack(1,1);
                //SetAvailableAttack(4,2);
                SetAvailableAttack(2,3);
                SetAvailableAttack(3,4);
                SetMovementSkill(new TeleportSkill());
            }
        }
    }
}
