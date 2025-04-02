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
        if (killer == null || victim == null)
        {
            Debug.LogWarning($"[KillLogItem] SetKillLog 실패 - killer 또는 victim이 null입니다. skillId: {skillId}, isFall: {isFall}");
            return;
        }

        killerNameText.text = killer.nickname ?? "???";
        killerNameText.color = killer.playerId == PlayerSetting.PlayerId ? Color.yellow : Color.white;

        victimNameText.text = victim.nickname ?? "???";
        victimNameText.color = victim.playerId == PlayerSetting.PlayerId ? Color.yellow : Color.white;

        var killerClassData = Database.GetCharacterClassData(killer.PLayerCharacterClass);
        var victimClassData = Database.GetCharacterClassData(victim.PLayerCharacterClass);

        if (killerClassData != null)
        {
            killerIcon.sprite = killerClassData.CharacterIcon;
            killerIcon.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[KillLogItem] killerClassData가 null입니다. Class: {killer.PLayerCharacterClass}");
            killerIcon.color = Color.clear;
        }

        if (victimClassData != null)
        {
            victimIcon.sprite = victimClassData.CharacterIcon;
            victimIcon.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[KillLogItem] victimClassData가 null입니다. Class: {victim.PLayerCharacterClass}");
            victimIcon.color = Color.clear;
        }

        // 자살한 경우: killer와 victim이 동일
        if (killer.playerId == victim.playerId)
        {
            killerIcon.color = Color.clear;
            killerNameText.text = "";
        }

        if (skillId > 0)
        {
            var attackData = Database.GetAttackData(skillId);
            if (attackData != null && attackData.Icon != null)
            {
                skillIcon.sprite = attackData.Icon;
                skillIcon.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[KillLogItem] skillId {skillId}에 대한 attackData가 없거나 아이콘이 null입니다.");
                skillIcon.color = Color.clear;
            }
        }
        else
        {
            skillIcon.color = Color.clear;
        }

        killIcon.sprite = isFall ? fallSprite : killSprite;

        StartCoroutine(FadeInAndOut());
    }

    // 🔹 페이드 인 & 아웃
    private IEnumerator FadeInAndOut()
    {
        canvasGroup.alpha = 0;
        float fadeInDuration = 0.5f;
        float fadeOutDuration = 0.5f;
        float displayDuration = 2.5f;

        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = t / fadeInDuration;
            yield return null;
        }
        canvasGroup.alpha = 1;

        yield return new WaitForSeconds(displayDuration);

        for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
        {
            canvasGroup.alpha = 1 - (t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0;

        KillLogUI.Instance.ReturnLogToPool(this);
    }
}
