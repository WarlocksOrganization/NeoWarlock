using UnityEngine;
using UnityEngine.UI;
using DataSystem;
using DataSystem.Database;
using TMPro;
using Player;
using System.Collections;

public class PlayerStatUI : MonoBehaviour
{
    [SerializeField] private Image ClassIcon;
    [SerializeField] private TMP_Text hpStat;
    [SerializeField] private TMP_Text atkStat;
    [SerializeField] private TMP_Text defStat;
    [SerializeField] private TMP_Text spdStat;
    [SerializeField] private TMP_Text knockStat;

    [SerializeField] private Slider HpSlider;
    private PlayerCharacter playerCharacter;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);
        var pc = FindFirstObjectByType<PlayerCharacter>();
        if (pc != null && pc.isOwned)
        {
            pc.NotifyStatChanged();  // 🔥 강제 동기화 요청
        }
    }
   public void Setup(PlayerCharacter character)
   {
       if (character == null)
       {
           Debug.LogWarning("[PlayerStatUI] 전달된 character가 null입니다.");
           return;
       }

       if (playerCharacter != null)
           playerCharacter.OnStatChanged -= RefreshStatUI;

       playerCharacter = character;

       var classData = Database.GetCharacterClassData(playerCharacter.PLayerCharacterClass);
       if (classData == null)
       {
           Debug.LogWarning($"[PlayerStatUI] ClassData를 찾을 수 없습니다: {playerCharacter.PLayerCharacterClass}");
           return;
       }

       if (ClassIcon == null)
       {
           Debug.LogError("[PlayerStatUI] ClassIcon이 인스펙터에 연결되지 않았습니다.");
           return;
       }

       ClassIcon.sprite = classData.CharacterIcon;
       playerCharacter.OnStatChanged += RefreshStatUI;
       HpSlider.value = 1f;
       RefreshStatUI(); // 최초 1회
   }

    public void RefreshStatUI()
    {
        hpStat.text = $"{playerCharacter.curHp} / {playerCharacter.maxHp}";
        HpSlider.value = (float)playerCharacter.curHp / playerCharacter.maxHp;
        atkStat.text = Mathf.Round(playerCharacter.AttackPower * 100).ToString();
        defStat.text = playerCharacter.Defense.ToString();
        spdStat.text = Mathf.Round(playerCharacter.MoveSpeed * 10).ToString();
        // knockStat.text = Mathf.Round(10 / playerCharacter.KnockbackFactor).ToString();
        knockStat.text = 100 * (1 - playerCharacter.KnockbackFactor).ToString();

        if (playerCharacter.CurHp == 0)
        {
            ClassIcon.color = new Color(0.4f, 0.4f, 0.4f, 1f); // 어두운 회색
        }
        else
        {
            ClassIcon.color = Color.white; // 원래대로 복원
        }
    }

    private void OnDisable()
    {
        if (playerCharacter != null)
            playerCharacter.OnStatChanged -= RefreshStatUI;
    }
}