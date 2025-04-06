using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
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
    [SerializeField] public Image BackGround;

    public void SetupWithScore(Constants.PlayerRecord record, int score, int upToRoundIndex, bool includeCurrentRound, int localPlayerId )
    {
        id = record.playerId;
        nicknameText.text = record.nickname;
        nicknameText.color = record.playerId == localPlayerId ? Color.yellow : Color.white;
        classIcon.sprite = Database.GetCharacterClassData(record.characterClass).CharacterIcon;
        totalScoreText.text = score.ToString();

        int kills = 0;
        int outKills = 0;
        int damage = 0;

        if (includeCurrentRound)
        {
            int endIndex = Mathf.Min(upToRoundIndex + 1, record.roundStatsList.Count);
            for (int i = 0; i < endIndex; i++)
            {
                var r = record.roundStatsList[i];
                kills += r.kills;
                outKills += r.outKills;
                damage += r.damageDone;
            }
        }
        else
        {
            // 현재 라운드만 보여줌
            if (upToRoundIndex >= 0 && upToRoundIndex < record.roundStatsList.Count)
            {
                var r = record.roundStatsList[upToRoundIndex];
                kills = r.kills;
                outKills = r.outKills;
                damage = r.damageDone;
            }
        }

        killText.text = kills.ToString();
        outKillText.text = outKills.ToString();
        damageText.text = damage.ToString();
        
        if (record.team == Constants.TeamType.TeamA)
            BackGround.color = new Color(1, 0.3f, 0.3f, 0.8f); // 빨간색
        else if (record.team == Constants.TeamType.TeamB)
            BackGround.color = new Color(0.3f, 0.3f, 1, 0.8f); // 파란색

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
    
    public void AnimateDamage(int from, int to, float duration = 1f)
    {
        StartCoroutine(AnimateDamageRoutine(from, to, duration));
    }

    private IEnumerator AnimateDamageRoutine(int from, int to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            damageText.text = Mathf.Lerp(from, to, t).ToString("F0");
            time += Time.deltaTime;
            yield return null;
        }
        damageText.text = to.ToString();
    }
}