using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using Mirror;
using Player.Combat;
using UnityEngine;

namespace Player
{
    public class LobbyPlayerCharacter : PlayerCharacter
    {
        public override void Start()
        {
            base.Start();
            
            if (isServer)
            {
                SetCharacterClass(Constants.CharacterClass.Mage);
            }
        }
    }
}
