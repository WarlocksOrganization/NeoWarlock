using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private GameObject scorePanelPrefab;
    [SerializeField] private Transform scorePanelParent;
    [SerializeField] private GameObject rankPanelPrefab;
    [SerializeField] private Transform rankPanelParent;
    [SerializeField] private GameObject teamResult;
    [SerializeField] private TMP_Text teamResultText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button resultToggleButton;
    [SerializeField] private TMP_Text toggleButtonText;
    [SerializeField] private TMP_Text bestPlayerName;
    [SerializeField] private TMP_Text bestPlayerStat;
    [SerializeField] private Image bestPlayerIcon;
    [SerializeField] private TMP_Text bestKillName;
    [SerializeField] private TMP_Text bestKillStat;
    [SerializeField] private Image bestKillIcon;
    [SerializeField] private TMP_Text bestDamageName;
    [SerializeField] private TMP_Text bestDamageStat;
    [SerializeField] private Image bestDamageIcon;
    [SerializeField] private GameObject playerCharacterPrefab;
    [SerializeField] private Transform bestSpawnPoint;
    [SerializeField] private Camera bestPlayerCamera;
    [SerializeField] private Vector3 cameraOffset = new(-0.8f, 0.9f, -2.4f);
    [SerializeField] private Vector3 cameraLookOffset = new(0f, 1.8f, 0f);

    private List<PlayerScorePanel> scorePanels = new();
    private List<RectTransform> panelPositions = new();
    private PlayableGraph? currentGraph;
    private bool isBestPlayerSpawned;

    private readonly Dictionary<Constants.CharacterClass, (string folder, string prefix)> classAnimMap = new()
    {
        { Constants.CharacterClass.Necromancer, ("Necro", "N") },
        { Constants.CharacterClass.Warrior, ("Warrior", "W") },
        { Constants.CharacterClass.Mage, ("Magician", "M") },
        { Constants.CharacterClass.Archer, ("Archer", "A") },
        { Constants.CharacterClass.Priest, ("Priest", "P") }
    };

    private void Awake()
    {
        returnToLobbyButton.gameObject.SetActive(false);
        resultToggleButton.gameObject.SetActive(false);
        returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
    }

    private void Start()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ShowReturnToLobbyButton()
    {
        returnToLobbyButton.gameObject.SetActive(true);
        resultToggleButton.gameObject.SetActive(true);
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(20f);
        if (returnToLobbyButton.gameObject.activeSelf)
            OnClickReturnToLobby();
    }

    public void ShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        foreach (var r in records)
            Debug.Log($"[ScoreBoardUI] {r.nickname} score@{roundIndex} = {r.GetScoreAtRound(roundIndex)}");

        teamResult.gameObject.SetActive(false);
        gameObject.SetActive(true);

        CreateRankPanels(records.Length);
        CreateScorePanels(records);

        scorePanelParent.gameObject.SetActive(false);
        StartCoroutine(DelayedRankingFlow(records, roundIndex));
    }

    private void CreateRankPanels(int count)
    {
        foreach (Transform child in rankPanelParent)
            Destroy(child.gameObject);
        panelPositions.Clear();

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(rankPanelPrefab, rankPanelParent);
            panelPositions.Add(go.GetComponent<RectTransform>());
            go.GetComponent<RankPanel>()?.Init(i + 1);
        }
    }

    private void CreateScorePanels(Constants.PlayerRecord[] records)
    {
        foreach (Transform child in scorePanelParent)
            Destroy(child.gameObject);

        scorePanels.Clear();
        foreach (var record in records.OrderBy(r => r.playerId))
        {
            var panel = Instantiate(scorePanelPrefab, scorePanelParent).GetComponent<PlayerScorePanel>();
            panel.id = record.playerId;
            scorePanels.Add(panel);
        }
    }

    private IEnumerator DelayedRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        scorePanelParent.gameObject.SetActive(true);
        StartCoroutine(ShowRankingFlow(records, roundIndex));
    }

    private IEnumerator ShowRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        roundText.text = $"{roundIndex + 1} 라운드 결과";

        var roundSorted = records
            .OrderByDescending(r => r.GetScoreAtRound(roundIndex))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < roundSorted.Count; i++)
        {
            var record = roundSorted[i];
            var panel = scorePanels[record.playerId];
            panel.gameObject.SetActive(true);

            panel.SetupWithScore(record, record.GetScoreAtRound(roundIndex), roundIndex, false, PlayerSetting.PlayerId);
            panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        yield return new WaitForSeconds(3f);
        yield return FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f);

        roundText.text = $"{roundIndex + 1} 라운드 통계";
        var preSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex - 1))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < preSorted.Count; i++)
        {
            var record = preSorted[i];
            var panel = scorePanels[record.playerId];
            var score = record.GetTotalScoreUpToRound(roundIndex - 1);
            panel.SetupWithScore(record, score, roundIndex - 1, true, PlayerSetting.PlayerId);
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        yield return FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f);
        yield return new WaitForSeconds(1f);

        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_ScoreCount);

        var finalSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < finalSorted.Count; i++)
        {
            var record = finalSorted[i];
            var panel = scorePanels.First(p => p.id == record.playerId);
            int before = preSorted.FirstOrDefault(p => p.playerId == record.playerId)?.GetTotalScoreUpToRound(roundIndex - 1) ?? 0;
            int after = record.GetTotalScoreUpToRound(roundIndex);

            panel.SetupWithScore(record, after, roundIndex, true, PlayerSetting.PlayerId);
            panel.AnimateScore(before, after);
            panel.MoveTo(panelPositions[i].anchoredPosition);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        var time = 0f;
        cg.alpha = from;
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }

    private void OnClickReturnToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }

    private void OnDestroy()
    {
        teamResult.SetActive(false);
        currentGraph?.Destroy();
    }
}