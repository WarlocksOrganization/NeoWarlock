using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem.Database;
using GameManagement;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
    public PlayerCardSlot[] slots; // UI 슬롯 3개
    [SerializeField] private TMP_Text timerText; // 남은 시간 표시

    private Queue<Database.PlayerCardData> selectedCardsQueue = new();
    public float maxTime = 10f;
    private float remainingTime;
    public Slider timerSlider;
    private bool isRunning = true;

    private PlayerCharacterUI playerCharacterUI;

    void Start()
    {
        LoadRandomPlayerCards();
        DisplayTopThreeCards();
        playerCharacterUI = FindFirstObjectByType<PlayerCharacterUI>();
        playerCharacterUI.GetComponent<CanvasGroup>().alpha = 0f;
        remainingTime = maxTime;
        timerSlider.maxValue = 1f;
        timerSlider.value = 1f;
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Clamp(remainingTime, 0f, maxTime);

        timerSlider.value = Mathf.Lerp(timerSlider.value, remainingTime / maxTime, Time.deltaTime * 10f);
        //fillImage.color = Color.Lerp(endColor, startColor, ratio);
        timerText.text = $"남은 시간: {Mathf.Ceil(remainingTime)}초";

        if (remainingTime <= 0f)
        {
            isRunning = false;
            ConfirmSelectedCards();
        }
    }

    private void LoadRandomPlayerCards()
    {
        HashSet<int> existingCardIds = new(PlayerSetting.PlayerCards?.Select(card => card.ID) ?? new int[0]);

        List<Database.PlayerCardData> availableCards = Database.playerCardDictionary.Values
            .Where(card =>
            {
                // 중복 카드 제외
                if (existingCardIds.Contains(card.ID))
                    return false;

                // 특수 강화 (스킬 강화 계열)인 경우만 필터링 필요
                if (IsSkillStat(card.StatType))
                {
                    return PlayerSetting.AttackSkillIDs.Contains(card.AppliedSkill);
                }

                return true; // 기본 스탯은 그대로 허용
            })
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

    private bool IsSkillStat(PlayerStatType statType)
    {
        return statType is PlayerStatType.AttackSpeed or PlayerStatType.Range
            or PlayerStatType.Radius or PlayerStatType.Damage
            or PlayerStatType.KnockbackForce or PlayerStatType.Cooldown
            or PlayerStatType.Special;
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
        while (selectedCardsQueue.Count > 0)
        {
            var candidate = selectedCardsQueue.Dequeue();

            // 현재 선택된 카드에 중복되는지 확인
            bool isDuplicate = PlayerSetting.PlayerCards.Any(card => card.ID == candidate.ID) ||
                               slots.Any(slot => slot.GetCurrentCard().ID == candidate.ID);

            if (!isDuplicate)
            {
                newCard = candidate;
                return true;
            }
        }

        newCard = null;
        return false;
    }

    // ⏳ 서버에서 호출하여 클라이언트 UI 업데이트
    public void UpdateTimer(float serverTime)
    {
        float timeDiff = Mathf.Abs(remainingTime - serverTime);

        if (timeDiff > 1f)
        {
            // 큰 차이는 바로 보정
            remainingTime = serverTime;
        }
        else
        {
            // 부드럽게 동기화 (클라이언트 기준 시간 보정)
            float smoothFactor = 0.3f;
            remainingTime = Mathf.Lerp(remainingTime, serverTime, smoothFactor);
        }
    }

    // ✅ 카드 선택 확정 및 UI 비활성화
    private void ConfirmSelectedCards()
    {
        playerCharacterUI.GetComponent<CanvasGroup>().alpha = 1f;
        PlayerSetting.PlayerCards.AddRange(slots.Select(slot => slot.GetCurrentCard()));
        gameObject.SetActive(false);
    }
}
