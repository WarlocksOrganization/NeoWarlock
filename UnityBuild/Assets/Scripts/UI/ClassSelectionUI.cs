using UnityEngine;
using DataSystem;
using GameManagement;
using Player;

public class ClassSelectionUI : MonoBehaviour
{
    [SerializeField] private PlayerSelectArea playerSelectArea;
    private PlayerCharacter playerCharacter;
    private Constants.CharacterClass characterClass = Constants.CharacterClass.Mage;

    private void OnEnable()
    {
        playerCharacter.SetIsDead(true);
        FindPlayerCharacter();
        ChangeClass(0);
    }

    private void FindPlayerCharacter()
    {
        if (playerCharacter == null)
        {
            PlayerCharacter[] playerCharacters = FindObjectsByType<PlayerCharacter>(sortMode: FindObjectsSortMode.None);
            foreach (var player in playerCharacters)
            {
                if (player.isOwned) // ✅ 현재 플레이어의 캐릭터만 선택
                {
                    playerCharacter = player;
                    break;
                }
            }
        }
    }

    public void ChangeClass(int classIndex)
    {
        characterClass = (Constants.CharacterClass)classIndex;
        
        if (playerSelectArea != null)
        {
            playerSelectArea.SelectCharacter(characterClass);
        }
    }
    
    public void SelectClass()
    {
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
        }

        if (playerCharacter != null)
        {
            Constants.CharacterClass selectedClass = characterClass; // ✅ int → CharacterClass 변환
            playerCharacter.SetCharacterClass(selectedClass);
            PlayerSetting.PlayerCharacterClass = selectedClass;
            playerCharacter.SetIsDead(false);
        }
        else
        {
            Debug.LogWarning("플레이어 캐릭터를 찾을 수 없습니다.");
        }
        gameObject.SetActive(false);
    }
}