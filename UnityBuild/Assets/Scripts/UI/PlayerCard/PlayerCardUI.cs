// ✅ PlayerCardUI.cs (서버 타이머 기반 리팩토링 완료)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
    public PlayerCardSlot[] slots;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject LoadingImage;
    [SerializeField] private Slider timerSlider;

    private Queue<Database.PlayerCardData> selectedCardsQueue = new();
    public float maxTime = 10f;
    private float remainingTime;
    private bool isLoading = true;

    private PlayerCharacterUI playerCharacterUI;
    private GamePlayer myGamePlayer;

    void Start()
    {
        LoadingImage.SetActive(true);
        LoadRandomPlayerCards();
        DisplayTopThreeCards();

        playerCharacterUI = FindFirstObjectByType<PlayerCharacterUI>();
        playerCharacterUI.GetComponent<CanvasGroup>().alpha = 0f;
        remainingTime = maxTime;
        timerSlider.maxValue = 1f;
        timerSlider.value = 1f;

        myGamePlayer = FindObjectsOfType<GamePlayer>().FirstOrDefault(gp => gp.isOwned);
    }

    void Update()
    {
        // 서버 타이머 기준으로 동작하므로 Update에서 처리하지 않음
    }

    private void LoadRandomPlayerCards()
    {
        HashSet<int> existingCardIds = new(PlayerSetting.PlayerCards?.Select(card => card.ID) ?? new int[0]);

        List<Database.PlayerCardData> availableCards = Database.playerCardDictionary.Values
            .Where(card =>
            {
                if (existingCardIds.Contains(card.ID)) return false;
                if (IsSkillStat(card.StatType))
                    return PlayerSetting.AttackSkillIDs.Contains(card.AppliedSkill);
                return true;
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

    public void UpdateTimer(float serverTime)
    {
        if (isLoading)
        {
            isLoading = false;
            StartCoroutine(FadeOutLoadingImage());
            AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_CardSelect);
        }

        float ratio = Mathf.Clamp01(serverTime / maxTime);
        timerSlider.value = ratio;
        timerText.text = $"남은 시간: {Mathf.Ceil(serverTime)}초";

        if (serverTime <= 0f)
        {
            ApplySelectedCardsAndHide();
        }
    }

    private IEnumerator FadeOutLoadingImage()
    {
        CanvasGroup canvasGroup = LoadingImage.GetComponent<CanvasGroup>() ?? LoadingImage.AddComponent<CanvasGroup>();

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        LoadingImage.SetActive(false);
    }

    private void ApplySelectedCardsAndHide()
    {
        playerCharacterUI.GetComponent<CanvasGroup>().alpha = 1f;

        List<Database.PlayerCardData> selected = slots.Select(slot => slot.GetCurrentCard()).ToList();
        PlayerSetting.PlayerCards.AddRange(selected);

        if (!myGamePlayer)
            myGamePlayer = FindObjectsByType<GamePlayer>(sortMode: FindObjectsSortMode.None).FirstOrDefault(gp => gp.isOwned);

        myGamePlayer?.OnCardSelectionConfirmed();

        gameObject.SetActive(false);
    }
}