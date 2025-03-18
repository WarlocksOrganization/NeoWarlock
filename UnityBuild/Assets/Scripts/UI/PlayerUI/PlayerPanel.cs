using DataSystem;
using DataSystem.Database;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanel : MonoBehaviour
{
    [SerializeField] private Image playerImage;
    [SerializeField] private Image isDeadImage;
    [SerializeField] private TMP_Text playerName;
    public void Setup(PlayerCharacter playerCharacter)
    {
        if (playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None)
        {
            playerImage.sprite = Database.GetCharacterClassData(playerCharacter.PLayerCharacterClass).CharacterIcon;
        }
        playerName.text = playerCharacter.nickname;
        if (playerCharacter.isDead)
        {
            //playerImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            if (isDeadImage != null) isDeadImage.gameObject.SetActive(true);
        }
        else
        {
            //playerImage.color = Color.white;
            if (isDeadImage != null) isDeadImage.gameObject.SetActive(false);
        }
    }
}

