using Player;
using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private PlayerPanel[] playerPanel;

    public void Setup(PlayerCharacter[] players)
    {
        for (int i = 0; i < playerPanel.Length; i++)
        {
            if (i >= players.Length)
            {
                playerPanel[i].GetComponent<CanvasGroup>().alpha = 0;
                continue;
            }
            playerPanel[i].GetComponent<CanvasGroup>().alpha = 1;
            playerPanel[i].Setup(players[i]);
        }
    }
}
