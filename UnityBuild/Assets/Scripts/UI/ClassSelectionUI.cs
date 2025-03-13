using UnityEngine;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Player;

public class ClassSelectionUI : MonoBehaviour
{
    [SerializeField] private PlayerSelectArea playerSelectArea;
    private PlayerCharacter playerCharacter;
    private Constants.CharacterClass characterClass = Constants.CharacterClass.Mage;

    private void Start()
    {
        FindPlayerCharacter();
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
            Constants.CharacterClass selectedClass = characterClass;
            PlayerSetting.PlayerCharacterClass = selectedClass;

            switch (selectedClass)
            {
                case Constants.CharacterClass.Mage:
                    PlayerSetting.MoveSkill = Constants.SkillType.TelePort;
                    PlayerSetting.AttackSkillIDs = new int[] { 0, 1, 2, 3 };
                    break;

                case Constants.CharacterClass.Archer:
                    PlayerSetting.MoveSkill = Constants.SkillType.Roll;
                    PlayerSetting.AttackSkillIDs = new int[] { 0, 11, 12, 13 };
                    break;

                case Constants.CharacterClass.Warrior:
                    PlayerSetting.MoveSkill = Constants.SkillType.Roll;
                    PlayerSetting.AttackSkillIDs = new int[] { 0, 21, 22, 23 };
                    break;

                case Constants.CharacterClass.Necromancer:
                    PlayerSetting.MoveSkill = Constants.SkillType.PhantomStep;
                    PlayerSetting.AttackSkillIDs = new int[] { 0, 31, 32, 33 };
                    break;

                case Constants.CharacterClass.Priest:
                    // 특정한 설정이 필요할 경우 추가
                    break;
            }

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