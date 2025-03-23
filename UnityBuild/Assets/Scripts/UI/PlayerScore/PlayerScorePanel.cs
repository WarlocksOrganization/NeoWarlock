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
    [SerializeField] private TMP_Text totalScoreText;

    public void Setup(Constants.PlayerStats stats)
    {
        id = stats.playerId;
        classIcon.sprite = Database.GetCharacterClassData(stats.characterClass).CharacterIcon;
        nicknameText.text = stats.nickname;
        killText.text = stats.kills.ToString();
        outKillText.text = stats.outKills.ToString();
        damageText.text = stats.damageDone.ToString();
        totalScoreText.text = stats.totalScore.ToString();

        SetRoundRanks(stats.roundRanks);
    }

    public void SetRoundRanks(List<int> roundRanks)
    {
        if (roundRanks == null || roundRanks.Count == 0)
        {
            rankText.text = "-";
            return;
        }

        // 예: [1, 2, 1] → "1-2-1"
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
}