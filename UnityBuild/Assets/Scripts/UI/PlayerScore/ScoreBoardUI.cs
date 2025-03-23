using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private PlayerScorePanel[] scorePanels;
    [SerializeField] private RectTransform[] panelPositions; // 1등 ~ 6등 위치

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button returnToLobbyButton;

    private void Awake()
    {
        returnToLobbyButton.gameObject.SetActive(false);
        returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
    }

    public void ShowReturnToLobbyButton()
    {
        returnToLobbyButton.gameObject.SetActive(true);
    }

    public void ShowScoreBoard()
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowRankingFlow());
    }

    private IEnumerator ShowRankingFlow()
    {
        int roundIndex = GameManager.Instance.CurrentRound - 1;

        // 1. 현재 라운드 결과 표시
        roundText.text = $"{roundIndex + 1} 라운드 결과";
        var roundSorted = GameManager.Instance.GetRoundOnlySortedRecords(roundIndex);
        for (int i = 0; i < roundSorted.Count; i++)
        {
            var record = roundSorted[i];
            var stats = record.roundStatsList[roundIndex];

            var panel = scorePanels.First(p => p.id == record.playerId); // ✅ id로 매칭
            panel.gameObject.SetActive(true); // ✅ 무조건 켜줌

            panel.SetupWithScore(record, GameManager.Instance.GetScoreAtRound(record, roundIndex));
            panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        yield return new WaitForSeconds(4f);

        // 2. 누적 점수 (반영 전)
        roundText.text = "최종 순위";
        var preSorted = GameManager.Instance.GetSortedRecords(roundIndex - 1);
        for (int i = 0; i < preSorted.Count; i++)
        {
            var panel = scorePanels.First(p => p.id == preSorted[i].playerId);
            int preScore = preSorted[i].GetTotalScoreUpToRound(roundIndex - 1);
            panel.SetupWithScore(preSorted[i], preScore);
            panel.MoveTo(panelPositions[i].anchoredPosition);
        }

        yield return new WaitForSeconds(2f);

        // 3. 최종 누적 점수
        roundText.text = "최종 순위";
        var finalSorted = GameManager.Instance.GetSortedRecords(roundIndex);
        for (int i = 0; i < finalSorted.Count; i++)
        {
            var panel = scorePanels.First(p => p.id == finalSorted[i].playerId);
            int before = preSorted.First(p => p.playerId == finalSorted[i].playerId).GetTotalScoreUpToRound(roundIndex - 1);
            int after = finalSorted[i].GetTotalScoreUpToRound(roundIndex);
            panel.AnimateScore(before, after);
            panel.MoveTo(panelPositions[i].anchoredPosition);
        }
    }

    private void OnClickReturnToLobby()
    {
        var player = NetworkClient.connection.identity.GetComponent<GamePlayer>();
        player.CmdRequestReturnToLobby();
    }
}