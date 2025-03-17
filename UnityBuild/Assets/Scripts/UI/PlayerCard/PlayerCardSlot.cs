using System;
using DataSystem.Database;
using GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text cardTypeText;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private Image cardIconImage;
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardDetailText;
    [SerializeField] private Button reRollButton;
    
    [SerializeField] private Sprite healthIcon;
    [SerializeField] private Sprite speedIcon;

    private Database.PlayerCardData currentCard;
    private Database.AttackData[] SkillData;
    private PlayerCardUI playerCardUI;

    private void Awake()
    {
        playerCardUI = FindFirstObjectByType<PlayerCardUI>();
        
        reRollButton.gameObject.SetActive(true);
        reRollButton.onClick.RemoveAllListeners();
        reRollButton.onClick.AddListener(Reroll);
    }

    private void Initialize()
    {
        SkillData = new Database.AttackData[4];
        for (int i = 1; i < 4; i++)
        {
            SkillData[i] = Database.GetAttackData(PlayerSetting.AttackSkillIDs[i]);
        }
    }

    public void SetCardData(Database.PlayerCardData cardData)
    {
        Initialize();
        currentCard = cardData;
        
        cardIconImage.sprite = Database.GetCardIcon(cardData.StatType);

        // 카드 타입에 따른 처리
        switch (cardData.StatType)
        {
            //기본 스탯
            case PlayerStatType.Health:
                cardTypeText.text = "스탯 강화";
                skillIconImage.sprite = healthIcon;
                cardNameText.text = "최대 체력";
                cardDetailText.text = $"체력 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Speed:
                cardTypeText.text = "스탯 강화";
                skillIconImage.sprite = speedIcon;
                cardNameText.text = "이동 속도";
                cardDetailText.text = $"이동 속도 +{cardData.BonusStat}%";
                break;
            
            //특정 스킬 강화
            case PlayerStatType.AttackSpeed:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"투사체 속도 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Range:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"최대 거리 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Radius:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"타격 범위 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Damage:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"데미지 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.KnockbackForce:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"넉백 거리 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Cooldown:
                cardTypeText.text = "스킬 강화";
                skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"쿨다운 -{cardData.BonusStat}%";
                break;

            case PlayerStatType.Special:
                cardTypeText.text = "스킬 승급";
                skillIconImage.sprite =  Database.GetAttackData(currentCard.AppliedSkillIndex+(int)PlayerSetting.PlayerCharacterClass*10+100).Icon;
                cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                cardDetailText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName} \n->  " +
                                      $"{Database.GetAttackData(currentCard.AppliedSkillIndex+(int)PlayerSetting.PlayerCharacterClass*10+100).DisplayName}";
                break;

            default:
                cardTypeText.text = "알 수 없는 카드";
                cardDetailText.text = "효과 없음";
                break;
        }
    }
    
    // ✅ 현재 카드 데이터를 반환
    public Database.PlayerCardData GetCurrentCard()
    {
        return currentCard;
    }

    private void Reroll()
    {
        if (playerCardUI.TryGetNewCard(out Database.PlayerCardData newCard))
        {
            SetCardData(newCard);
            reRollButton.gameObject.SetActive(false); // ✅ 버튼 비활성화
        }
    }
}