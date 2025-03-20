using Player;
using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private PlayerPanel[] playerPanel;

    public void Setup(PlayerCharacter[] players, int playerid)
    {
        for (int i = 0; i < playerPanel.Length; i++)
        {
            if (i >= players.Length)
            {
                playerPanel[i].GetComponent<CanvasGroup>().alpha = 0;
                continue;
            }
            playerPanel[i].GetComponent<CanvasGroup>().alpha = 1;
            playerPanel[i].Setup(players[i], playerid);
        }
    }
    
    public void OpenPanels()
    {
        for (int i = 0; i < playerPanel.Length; i++)
        {
            playerPanel[i].gameObject.SetActive(true);
        }
    }

    public void ClosePanels()
    {
        for (int i = 0; i < playerPanel.Length; i++)
        {
            playerPanel[i].gameObject.SetActive(false);
        }
    }
}
