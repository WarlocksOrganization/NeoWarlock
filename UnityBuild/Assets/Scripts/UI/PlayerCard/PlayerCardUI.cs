using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem.Database;
using GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerCardUI : MonoBehaviour
{
    public PlayerCardSlot[] slots; // UI 슬롯 3개
    [SerializeField] private TMP_Text timerText; // 남은 시간 표시

    private Queue<Database.PlayerCardData> selectedCardsQueue = new();
    private float remainingTime = 10f; // 초기 시간 10초

    void Start()
    {
        LoadRandomPlayerCards();
        DisplayTopThreeCards();
    }

    private void LoadRandomPlayerCards()
    {
        HashSet<int> existingCardIds = new(PlayerSetting.PlayerCards?.Select(card => card.ID) ?? new int[0]);

        List<Database.PlayerCardData> availableCards = Database.playerCardDictionary.Values
            .Where(card => !existingCardIds.Contains(card.ID))
            .ToList();

        if (availableCards.Count < 6)
        {
            Debug.LogError("사용 가능한 카드가 6개 미만입니다!");
            return;
        }

        List<Database.PlayerCardData> selectedCards = availableCards
            .OrderBy(_ => Random.value)
            .Take(6)
            .ToList();

        selectedCardsQueue = new Queue<Database.PlayerCardData>(selectedCards);
    }

    private void DisplayTopThreeCards()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (selectedCardsQueue.Count > 0)
            {
                Database.PlayerCardData card = selectedCardsQueue.Dequeue();
                slots[i].SetCardData(card);
            }
        }
    }

    public bool TryGetNewCard(out Database.PlayerCardData newCard)
    {
        if (selectedCardsQueue.Count > 0)
        {
            newCard = selectedCardsQueue.Dequeue();
            return true;
        }

        newCard = null;
        return false;
    }

    // ⏳ 서버에서 호출하여 클라이언트 UI 업데이트
    public void UpdateTimer(float time)
    {
        remainingTime = time;
        timerText.text = $"남은 시간: {Mathf.Ceil(remainingTime)}초";

        if (remainingTime <= 0)
        {
            ConfirmSelectedCards();
        }
    }

    // ✅ 카드 선택 확정 및 UI 비활성화
    private void ConfirmSelectedCards()
    {
        PlayerSetting.PlayerCards.AddRange(slots.Select(slot => slot.GetCurrentCard()));
        gameObject.SetActive(false);
    }
}
