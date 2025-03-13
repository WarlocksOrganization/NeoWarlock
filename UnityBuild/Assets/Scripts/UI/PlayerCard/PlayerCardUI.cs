using System.Collections.Generic;
using System.Linq;
using DataSystem.Database;
using GameManagement;
using UnityEngine;

public class PlayerCardUI : MonoBehaviour
{
    [SerializeField] private PlayerCardSlot[] slots; // UI 슬롯 3개

    private Queue<Database.PlayerCardData> selectedCardsQueue = new();

    void Start()
    {
        LoadRandomPlayerCards();
        DisplayTopThreeCards();
    }

    private void LoadRandomPlayerCards()
    {
        // PlayerSetting.PlayerCardss에 없는 카드만 필터링
        HashSet<int> existingCardIds = new(PlayerSetting.PlayerCardss?.Select(card => card.ID) ?? new int[0]);

        List<Database.PlayerCardData> availableCards = Database.playerCardDictionary.Values
            .Where(card => !existingCardIds.Contains(card.ID))
            .ToList();

        if (availableCards.Count < 6)
        {
            Debug.LogError("사용 가능한 카드가 6개 미만입니다!");
            return;
        }

        // 6개 랜덤 선택
        List<Database.PlayerCardData> selectedCards = availableCards
            .OrderBy(_ => Random.value)
            .Take(6)
            .ToList();

        // Queue에 저장
        selectedCardsQueue = new Queue<Database.PlayerCardData>(selectedCards);
    }

    private void DisplayTopThreeCards()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (selectedCardsQueue.Count > 0)
            {
                Database.PlayerCardData card = selectedCardsQueue.Dequeue();
                slots[i].SetCardData(card); // 슬롯에 카드 데이터 할당
            }
        }
    }
}