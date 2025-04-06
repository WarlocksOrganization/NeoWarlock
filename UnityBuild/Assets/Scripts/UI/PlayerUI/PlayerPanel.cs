using System.Collections.Generic;
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
    [SerializeField] private Image backGround;
    
    [SerializeField] private Transform cardSlotParent; // HorizontalLayoutGroup 붙은 곳
    [SerializeField] private CardSlot cardSlotPrefab;
    private readonly List<CardSlot> activeCardSlots = new();

    
    public void Setup(PlayerCharacter playerCharacter, int playerid)
    {
        if (playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None)
        {
            playerImage.sprite = Database.GetCharacterClassData(playerCharacter.PLayerCharacterClass).CharacterIcon;
        }
    
        playerName.text = playerCharacter.nickname;
        playerName.color = playerCharacter.playerId == playerid ? Color.yellow : Color.white;
        
        if (playerCharacter.team == Constants.TeamType.TeamA)
        {
            backGround.color = new Color(1,0.3f,0.3f,0.8f);
        }
        else if (playerCharacter.team == Constants.TeamType.TeamB)
        {
            backGround.color = new Color(0.3f,0.3f,1,0.8f);
        }
    
        // ✅ 강제 UI 업데이트
        UpdateIsDeadImage(playerCharacter.isDead);
        
        ShowCards(PlayerSetting.PlayerCards);

        //Debug.Log($"[PlayerPanel] {PlayerSetting.PlayerId} 플레이어 {playerCharacter.playerId} 체력: {playerCharacter.curHp}, isDead: {playerCharacter.isDead}");
    }
    
    public void ShowCards(List<Database.PlayerCardData> cards)
    {
        // 기존 카드 제거
        foreach (var slot in activeCardSlots)
            Destroy(slot.gameObject);
        activeCardSlots.Clear();

        foreach (var card in cards)
        {
            var slot = Instantiate(cardSlotPrefab, cardSlotParent);
            slot.Init(card);
            activeCardSlots.Add(slot);
        }
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

