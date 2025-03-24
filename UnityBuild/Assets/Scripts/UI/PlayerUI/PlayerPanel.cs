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
    [SerializeField] private Image isDeadImage;
    [SerializeField] private TMP_Text playerName;
    public void Setup(PlayerCharacter playerCharacter, int playerid)
    {
        if (playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None)
        {
            playerImage.sprite = Database.GetCharacterClassData(playerCharacter.PLayerCharacterClass).CharacterIcon;
        }
    
        playerName.text = playerCharacter.nickname;
        playerName.color = playerCharacter.playerId == playerid ? Color.yellow : Color.white;
    
        // ✅ 강제 UI 업데이트
        UpdateIsDeadImage(playerCharacter.isDead);

        //Debug.Log($"[PlayerPanel] {PlayerSetting.PlayerId} 플레이어 {playerCharacter.playerId} 체력: {playerCharacter.curHp}, isDead: {playerCharacter.isDead}");
    }

    private void UpdateIsDeadImage(bool isDead)
    {
        if (isDead)
        {
            if (isDeadImage != null) isDeadImage.gameObject.SetActive(true);
        }
        else
        {
            if (isDeadImage != null) isDeadImage.gameObject.SetActive(false);
        }
    }

}

