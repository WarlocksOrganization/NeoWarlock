using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CharacterDetailLoader : MonoBehaviour
{
    private Dictionary<string, CharacterDetail> characterDetails = new Dictionary<string, CharacterDetail>();

    void Start()
    {
        LoadCharacterDetails();
    }

    private void LoadCharacterDetails()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("Data/CharacterDetails");
        if (csvFile == null)
        {
            Debug.LogError("CSV 파일을 찾을 수 없습니다! Assets/Resources/Data/CharacterDetails.csv를 확인하세요.");
            return;
        }

        string csvText = System.Text.Encoding.UTF8.GetString(csvFile.bytes);
    string[] lines = csvText.Split('\n');
    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue;
        string[] values = lines[i].Split(',');

        if (values.Length < 5) continue;
        string characterClass = values[0].Trim();
        string characterName = values[1].Replace("\"", "");
        string characterLine = values[2].Replace("\"", "");
        string characterAtk = values[3].Trim();
        string characterHp = values[4].Trim();
        string characterSpeed = values[5].Trim();
        string characterKnock = values[6].Trim();

        CharacterDetail newCharacter = new CharacterDetail
        {
            characterName = characterName,
            characterLine = characterLine,
            characterAtk = characterAtk,
            characterHp = characterHp,
            characterSpeed = characterSpeed,
            characterKnock = characterKnock
        };

        if (!characterDetails.ContainsKey(characterClass))
        {
            characterDetails.Add(characterClass, newCharacter);
        }
    }
    } 

    public CharacterDetail GetCharacterDetail(string characterClass)
    {
        if (characterDetails.TryGetValue(characterClass, out CharacterDetail detail))
        {
            return detail;
        }
        return null;
    }
}

[System.Serializable]
public class CharacterDetail
{
    public string characterName;
    public string characterLine;
    public string characterAtk;
    public string characterHp;
    public string characterSpeed;
    public string characterKnock;
}
