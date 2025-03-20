using System.Collections;
using DataSystem.Database;
using GameManagement;
using Player;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KillLogItem : MonoBehaviour
{
    [SerializeField] private TMP_Text killerNameText;
    [SerializeField] private TMP_Text victimNameText;
    [SerializeField] private Image killerIcon;
    [SerializeField] private Image victimIcon;
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image killIcon;
    
    [SerializeField] private Sprite killSprite;
    [SerializeField] private Sprite fallSprite;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // 🔹 킬 로그 설정 (닉네임, 아이콘, 스킬)
    public void SetKillLog(PlayerCharacter killer, PlayerCharacter victim, int skillId, bool isFall)
    {
        killerNameText.text = killer.nickname;
        killerNameText.color = killer.playerId == PlayerSetting.PlayerId ? Color.yellow : Color.white;
        victimNameText.text = victim.nickname;
        victimNameText.color = victim.playerId == PlayerSetting.PlayerId ? Color.yellow : Color.white;
        killerIcon.sprite = Database.GetCharacterClassData(killer.PLayerCharacterClass).CharacterIcon;
        victimIcon.sprite = Database.GetCharacterClassData(victim.PLayerCharacterClass).CharacterIcon;
        
        if (killer.playerId == victim.playerId)
        {
            killerIcon.color = Color.clear;
            killerNameText.text = "";
        }
        
        if (skillId > 0)
        {
            skillIcon.sprite = Database.GetAttackData(skillId).Icon;
            skillIcon.color = Color.white;
        }
        else
        {
            skillIcon.color = Color.clear;
        }

        if (isFall)
        {
            killIcon.sprite = fallSprite;
        }
        else
        {
            killIcon.sprite = killSprite;
        }

        StartCoroutine(FadeInAndOut());
    }

    // 🔹 페이드 인 & 아웃
    private IEnumerator FadeInAndOut()
    {
        canvasGroup.alpha = 0;
        float fadeInDuration = 0.5f;
        float fadeOutDuration = 0.5f;
        float displayDuration = 2.5f; // 유지 시간

        // ✅ 페이드 인
        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = t / fadeInDuration;
            yield return null;
        }
        canvasGroup.alpha = 1;

        // ✅ 일정 시간 유지
        yield return new WaitForSeconds(displayDuration);

        // ✅ 페이드 아웃
        for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = 1 - (t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0;

        // ✅ KillLogUI에 반환 요청 (자동 관리)
        KillLogUI.Instance.ReturnLogToPool(this);
    }
}