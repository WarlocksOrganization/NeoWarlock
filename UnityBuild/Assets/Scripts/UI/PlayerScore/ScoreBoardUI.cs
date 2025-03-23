using System.Collections;
using System.Linq;
using DataSystem;
using UnityEngine;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private PlayerScorePanel[] scorePanels;
    [SerializeField] private RectTransform[] panelPositions; // 1등 ~ 6등 위치들

    public void ShowScoreBoard(Constants.PlayerStats[] statsArray)
    {
        gameObject.SetActive(true);

        // 초기 패널 세팅 (원래 순서)
        for (int i = 0; i < statsArray.Length; i++)
        {
            var panel = scorePanels[i];
            var stats = statsArray[i];

            panel.gameObject.SetActive(true);
            panel.Setup(stats);
            panel.SetRoundRanks(stats.roundRanks); // 초기에는 순위 비표시 or 임시값
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        // 1초 뒤에 정렬 후 이동
        StartCoroutine(DelayedSortAndMove(statsArray));
    }

    private IEnumerator DelayedSortAndMove(Constants.PlayerStats[] statsArray)
    {
        yield return new WaitForSeconds(1f);

        // 점수 내림차순 정렬
        var orderedStats = statsArray.OrderByDescending(s => s.totalScore).ToArray();

        for (int i = 0; i < orderedStats.Length; i++)
        {
            var targetStats = orderedStats[i];

            // 해당 stats에 맞는 panel 찾기 (nickname이나 ID로 매칭)
            var panel = scorePanels.FirstOrDefault(p => p.id == targetStats.playerId);
            if (panel != null)
            {
                statsArray[i].roundRanks.Add(i + 1);
                panel.SetRoundRanks(statsArray[i].roundRanks); // 순위 업데이트
                panel.MoveTo(panelPositions[i].anchoredPosition); // 이동
            }
        }
    }
}