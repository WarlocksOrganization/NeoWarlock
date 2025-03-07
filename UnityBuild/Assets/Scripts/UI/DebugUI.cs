using UnityEngine;
using DataSystem;
using Player;

public class ClassSelectionUI : MonoBehaviour
{
    private PlayerCharacter playerCharacter;

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
        FindPlayerCharacter(); // ✅ 플레이어가 변경되었을 수도 있으므로 다시 찾기

        if (playerCharacter != null)
        {
            Constants.CharacterClass selectedClass = (Constants.CharacterClass)classIndex; // ✅ int → CharacterClass 변환
            playerCharacter.SetCharacterClass(selectedClass);
        }
        else
        {
            Debug.LogWarning("플레이어 캐릭터를 찾을 수 없습니다.");
        }
    }
}