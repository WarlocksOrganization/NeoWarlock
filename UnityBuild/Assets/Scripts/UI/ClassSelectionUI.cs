using UnityEngine;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Player;
using Mirror;

public class ClassSelectionUI : MonoBehaviour
{
    [SerializeField] private PlayerSelectArea playerSelectArea;
    private PlayerCharacter playerCharacter;
    private Constants.CharacterClass characterClass = Constants.CharacterClass.Mage;

    private void Start()
    {
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

    public void RollClass(int changeAmount)
    {
        ChangeClass(((int)characterClass + changeAmount+5)%5);
    }
    
    public void SelectClass()
    {
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
        }

        if (playerCharacter != null)
        {
            Constants.CharacterClass selectedClass = characterClass;
            PlayerSetting.PlayerCharacterClass = selectedClass;
            
            PlayerSetting.AttackSkillIDs = new int[] { 0, 
                Database.GetCharacterClassData(selectedClass).AttackSkillIds[0], 
                Database.GetCharacterClassData(selectedClass).AttackSkillIds[1], 
                Database.GetCharacterClassData(selectedClass).AttackSkillIds[2] };
            PlayerSetting.MoveSkill = Database.GetCharacterClassData(selectedClass).MovementSkillType;

            // 서버에 데이터 전송하여 동기화
            playerCharacter.CmdSetCharacterData(
                PlayerSetting.PlayerCharacterClass,
                PlayerSetting.MoveSkill,
                PlayerSetting.AttackSkillIDs
            );
            playerCharacter.SetIsDead(false);

            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("플레이어 캐릭터를 찾을 수 없습니다.");
        }
    }
}