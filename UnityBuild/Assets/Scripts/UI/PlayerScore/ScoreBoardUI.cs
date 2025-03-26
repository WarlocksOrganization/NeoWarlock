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

    public void ShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
{
    gameObject.SetActive(true);
    StartCoroutine(ShowRankingFlow(records, roundIndex)); // ✅ records 사용
}

private IEnumerator ShowRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
{
    // 1. 현재 라운드 결과 표시
    roundText.text = $"{roundIndex + 1} 라운드 결과";

    var roundSorted = records
        .OrderByDescending(r => GameManager.Instance.GetScoreAtRound(r, roundIndex))
        .ToList();

    for (int i = 0; i < roundSorted.Count; i++)
    {
        var record = roundSorted[i];
        var stats = record.roundStatsList[roundIndex];

        var panel = scorePanels[record.playerId];
        panel.gameObject.SetActive(true);

        panel.SetupWithScore(record, GameManager.Instance.GetScoreAtRound(record, roundIndex), roundIndex, includeCurrentRound: false);
        panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
        panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
    }

    yield return new WaitForSeconds(3f);
    yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f));

    // 2. 누적 점수 (반영 전)
    roundText.text = $"{roundIndex + 1} 라운드 통계";

    var preSorted = records
        .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex - 1))
        .ToList();

    for (int i = 0; i < preSorted.Count; i++)
    {
        var panel = scorePanels[preSorted[i].playerId];
        int preScore = preSorted[i].GetTotalScoreUpToRound(roundIndex - 1);
        panel.SetupWithScore(preSorted[i], preScore, roundIndex - 1, includeCurrentRound: true);
        panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
    }

    yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f));
    yield return new WaitForSeconds(1f);

    var finalSorted = records
        .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex))
        .ToList();

    for (int i = 0; i < finalSorted.Count; i++)
    {
        var panel = scorePanels.First(p => p.id == finalSorted[i].playerId);
        int before = preSorted.First(p => p.playerId == finalSorted[i].playerId).GetTotalScoreUpToRound(roundIndex - 1);
        int after = finalSorted[i].GetTotalScoreUpToRound(roundIndex);
        panel.SetupWithScore(finalSorted[i], after, roundIndex, includeCurrentRound: true);
        panel.AnimateScore(before, after);
        panel.MoveTo(panelPositions[i].anchoredPosition);
        
        // ✅ 데미지 애니메이션 추가
        int prevDamage = 0;
        for (int r = 0; r <= roundIndex - 1; r++)
        {
            if (r < finalSorted[i].roundStatsList.Count)
                prevDamage += finalSorted[i].roundStatsList[r].damageDone;
        }

        int finalDamage = prevDamage;
        if (roundIndex < finalSorted[i].roundStatsList.Count)
            finalDamage += finalSorted[i].roundStatsList[roundIndex].damageDone;

        panel.AnimateDamage(prevDamage, finalDamage);
    }
}


    private void OnClickReturnToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost(); // 서버와 클라이언트 모두 종료
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient(); // 클라이언트만 종료
        }
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float time = 0f;
        cg.alpha = from;
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        cg.alpha = to;
    }
}