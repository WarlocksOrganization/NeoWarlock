using DataSystem;
using DataSystem.Database;
using GameManagement;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private Image playerImage;
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private Image backGround;
    public void Setup(PlayerCharacter playerCharacter)
    {
        if (playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None)
        {
            playerImage.sprite = Database.GetCharacterClassData(playerCharacter.PLayerCharacterClass).CharacterIcon;
        }
        playerName.text = playerCharacter.nickname;
        playerName.color = PlayerSetting.PlayerNum == playerCharacter.playerId ? Color.yellow : Color.white;
        playerImage.color = playerCharacter.isDead ? new Color(1,0,0,0.5f) : Color.white;
    }
}

