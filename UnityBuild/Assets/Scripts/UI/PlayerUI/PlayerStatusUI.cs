using System.Collections.Generic;
using Player;
using UnityEngine;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private GameObject playerPanelPrefab; // 🔹 프리팹 참조
    [SerializeField] private Transform panelParent; // 🔹 부모 트랜스폼 (Vertical/Horizontal Layout Group 등)

    private readonly List<PlayerPanel> playerPanels = new();

    public void Setup(PlayerCharacter[] players, int playerid)
    {
        // 기존 UI 정리
        foreach (Transform child in panelParent)
        {
            Destroy(child.gameObject);
        }
        playerPanels.Clear();

        // 플레이어 수만큼 동적 생성
        foreach (var player in players)
        {
            var panelGO = Instantiate(playerPanelPrefab, panelParent);
            var panel = panelGO.GetComponent<PlayerPanel>();
            if (panel != null)
            {
                panel.Setup(player, playerid);
                playerPanels.Add(panel);
            }
        }
    }
    
    public void OpenPanels()
    {
        foreach (var panel in playerPanels)
        {
            panel.gameObject.SetActive(true);
        }
    }

    public void ClosePanels()
    {
        foreach (var panel in playerPanels)
        {
            panel.gameObject.SetActive(false);
        }
    }
}
