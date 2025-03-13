using DataSystem.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text cardTypeText;
    [SerializeField] private Image carIconImage;
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardDetailText;
    
    [SerializeField] private Button roRollButton;

    private Database.PlayerCardData currentCard;

    public void SetCardData(Database.PlayerCardData cardData)
    {
        currentCard = cardData;
        cardNameText.text = cardData.Name; // 카드 이름 설정

        // 카드 타입에 따른 처리
        switch (cardData.StatType)
        {
            case PlayerStatType.Health:
                cardTypeText.text = "스탯 강화";
                cardDetailText.text = $"체력 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Speed:
                cardTypeText.text = "스탯 강화";
                cardDetailText.text = $"이동 속도 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.AttackSpeed:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"투사체 속도 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Range:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"최대 거리 +{cardData.BonusStat}";
                break;

            case PlayerStatType.Radius:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"타격 범위 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Damage:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"데미지 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.KnockbackForce:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"넉백 거리 +{cardData.BonusStat}%";
                break;

            case PlayerStatType.Cooldown:
                cardTypeText.text = "스킬 강화";
                cardDetailText.text = $"쿨다운 {cardData.BonusStat}% 감소";
                break;

            case PlayerStatType.Special:
                cardTypeText.text = "스킬 진화";
                cardDetailText.text = "스킬 진화 효과 적용";
                break;

            default:
                cardTypeText.text = "알 수 없는 카드";
                cardDetailText.text = "효과 없음";
                break;
        }
    }

    private void Reroll()
    {
        
    }
}