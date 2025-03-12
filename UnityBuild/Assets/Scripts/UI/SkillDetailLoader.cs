using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SkillDetailLoader : MonoBehaviour
{
    private Dictionary<string, List<SkillData>> characterSkills = new Dictionary<string, List<SkillData>>();

    void Start()
    {
        LoadCharacterSkills();
    }

    private void LoadCharacterSkills()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("Data/SkillDetails");
        if (csvFile == null)
        {
            Debug.LogError("CSV 파일을 찾을 수 없습니다! Assets/Resources/Data/SkillDetails.csv를 확인하세요.");
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
        string skillType = values[1].Trim();
        string skillName = values[2].Trim().Replace("\"", "");
        string skillDescription = values[3].Trim().Replace("\"", "");
        string iconPath = values[4].Trim().Replace("\"", "");

        string fullPath = skillType == "Attack" ? $"Sprites/AttackIcons/{iconPath}" : $"Sprites/MoveIcons/{iconPath}";
        Sprite skillIcon = Resources.Load<Sprite>(fullPath);

        if (skillIcon == null)
        {
            Debug.LogWarning($"아이콘을 찾을 수 없습니다: {fullPath}");
        }

        SkillData newSkill = new SkillData
        {
            // skillType = skillType,
            skillName = skillName,
            skillDescription = skillDescription,
            skillIcon = skillIcon
        };

        if (!characterSkills.ContainsKey(characterClass))
        {
            characterSkills[characterClass] = new List<SkillData>();
        }

        characterSkills[characterClass].Add(newSkill);
    }
    }

    public List<SkillData> GetSkillsForCharacter(string characterClass)
    {
        if (characterSkills.TryGetValue(characterClass, out List<SkillData> skills))
        {
            return skills;
        }
        return new List<SkillData>();
    }
}

[System.Serializable]
public class SkillData
{
    // public string skillType;  // ✅ "Attack" 또는 "Move"
    public string skillName;
    public string skillDescription;
    public Sprite skillIcon;
}
