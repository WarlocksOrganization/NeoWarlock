using TMPro;
using UnityEngine;

public class RankPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;

    public void Init(int num)
    {
        rankText.text = "" + num;

        if (num == 1)
        {
            // 금색 느낌
            rankText.color = new Color(213f / 255f, 161f / 255f, 30f / 255f);
        }
        else if (num == 2)
        {
            // 은색 느낌
            rankText.color = new Color(163f / 255f, 163f / 255f, 163f / 255f);
        }
        else if (num == 3)
        {
            // 동색 느낌
            rankText.color = new Color(205f / 255f, 127f / 255f, 50f / 255f);
        }
    }
}
