using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScorePanel : MonoBehaviour
{
    public int id;
    [SerializeField] private Image classIcon;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text outKillText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] public TMP_Text totalScoreText;

    public void SetupWithScore(Constants.PlayerRecord record, int score)
    {
        id = record.playerId;
        nicknameText.text = record.nickname;
        classIcon.sprite = Database.GetCharacterClassData(record.characterClass).CharacterIcon;
        totalScoreText.text = score.ToString();

        var ranks = record.roundStatsList.Select(r => r.rank).ToList();
        SetRoundRanks(ranks);
    }

    public void SetRoundRanks(List<int> roundRanks)
    {
        if (roundRanks == null || roundRanks.Count == 0)
        {
            rankText.text = "-";
            return;
        }
        rankText.text = string.Join("-", roundRanks.Select(r => r.ToString()));
    }

    public void MoveTo(Vector3 targetPosition, float duration = 1f)
    {
        StartCoroutine(MoveSmoothly(targetPosition, duration));
    }

    private IEnumerator MoveSmoothly(Vector3 targetAnchoredPos, float duration)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 startPos = rectTransform.anchoredPosition;
        float time = 0f;

        while (time < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPos;
    }

    public void AnimateScore(int from, int to, float duration = 1f)
    {
        StartCoroutine(AnimateScoreRoutine(from, to, duration));
    }

    private IEnumerator AnimateScoreRoutine(int from, int to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            totalScoreText.text = Mathf.Lerp(from, to, t).ToString("F0");
            time += Time.deltaTime;
            yield return null;
        }
        totalScoreText.text = to.ToString();
    }
}