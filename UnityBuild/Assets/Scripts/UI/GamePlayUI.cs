using System;
using System.Linq;
using DataSystem;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GamePlayUI : GameLobbyUI
{
    [SerializeField] private GameObject alamGameObject;
    [SerializeField] private TextMeshProUGUI alamText;
    [SerializeField] private TextMeshProUGUI countDownText;
    
    private int survivePlayers = -1;
    private Constants.GameState gameState = Constants.GameState.NotStarted;

    private int maxTime = 10;
    
    [SerializeField] private GameObject StartCube; // 발판
    
    [SerializeField] private ScoreBoardUI scoreBoardUI;

    
    private void Start()
    {
        alamGameObject.SetActive(false);
        playerStatusUI.ClosePanels();
    }
    public override void UpdatePlayerInRoon()
    {
        // ✅ 현재 씬에서 모든 PlayerCharacter 찾기
        foundCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .OrderBy(player => player.playerId)
            .ToArray();

        // ✅ 최대 playerId 값을 기준으로 배열 크기 결정
        int maxPlayerId = foundCharacters.Length > 0 ? foundCharacters.Max(p => p.playerId) : 0;
        PlayerCharacters = new GameObject[maxPlayerId + 1]; // ✅ playerId가 배열 인덱스가 되도록 크기 지정

        // ✅ 각 playerId 위치에 해당하는 PlayerCharacter 저장
        foreach (var player in foundCharacters)
        {
            if (player.playerId >= 0 && player.playerId < PlayerCharacters.Length)
            {
                PlayerCharacters[player.playerId] = player.gameObject; // ✅ playerId 위치에 저장
            }
        }

        survivePlayers = 0;

        foreach (PlayerCharacter playerCharacter in foundCharacters)
        {
            //Debug.Log(playerCharacter.playerId + ",  " + playerCharacter.isDead + ", " + playerCharacter.curHp);
            if (!playerCharacter.isDead)
            {
                survivePlayers += 1;
            }

            if (playerCharacter.State == Constants.PlayerState.Ready && gameState == Constants.GameState.NotStarted)
            {
                gameState = Constants.GameState.Counting;
                GameStart();
            }
        }

        PlayerInRoonText.text = $"남은 인원 : {survivePlayers} 명";

        playerStatusUI.Setup(foundCharacters, PlayerSetting.PlayerId);
    }


    private void GameStart()
    {
        playerStatusUI.OpenPanels();
        alamGameObject.SetActive(true);
        StartCube.SetActive(false);

        NetworkTimer networkTimer = FindFirstObjectByType<NetworkTimer>();

        networkTimer.StartCountdown(3);
    }

    public void UpdateCountdownUI(int time)
    {
        if (gameState == Constants.GameState.Start)
        {
            alamText.color = Color.white;
            if (time <= 5)
            {
                alamText.color = new Color(0.8f, 0, 0);
            }
            alamText.text = $"{time}초";
            if (time == 0)
            {
                NetworkTimer networkTimer = FindFirstObjectByType<NetworkTimer>();
                networkTimer.StartCountdown(maxTime);
            }
        }
        
        else if (gameState == Constants.GameState.Counting)
        {
            countDownText.text = time.ToString();
            
            if (time == 0)
            {
                foreach (var player in foundCharacters)
                {
                    player.SetState(Constants.PlayerState.Start);
                }
                countDownText.gameObject.SetActive(false);
                gameState = Constants.GameState.Start;
                UpdatePlayerInRoon();
            }
        }
    }
    
    public void ShowFinalScoreBoard(Constants.PlayerStats[] stats)
    {
        scoreBoardUI.ShowScoreBoard(); // 모든 클라이언트에서 실행
    }
}
