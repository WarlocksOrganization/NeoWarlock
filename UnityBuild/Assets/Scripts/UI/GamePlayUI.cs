using System;
using System.Collections;
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

        networkTimer.StartCountdown(Constants.CountTime);
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
                networkTimer.StartCountdown(Constants.MaxGameEventTime);
            }
        }
        
        else if (gameState == Constants.GameState.Counting)
        {
            foreach (var player in foundCharacters)
            {
                player.SetState(Constants.PlayerState.Counting);
            }
            
            countDownText.text = time.ToString();
            StartCoroutine(PunchScale(countDownText.transform));
            
            if (time == 0)
            {   
                countDownText.text = "SMASH!";
                foreach (var player in foundCharacters)
                {
                    player.SetState(Constants.PlayerState.Start);
                }

                StartCoroutine(HideAfterDelay(countDownText.gameObject, 0.8f));
                gameState = Constants.GameState.Start;
                UpdatePlayerInRoon();
            }
        }
    }
    
    IEnumerator PunchScale(Transform target, float upScale = 1.1f, float downScale = 0.5f, float totalDuration = 0.3f)
    {
        Vector3 original = Vector3.one;
        Vector3 overshoot = original * upScale;
        Vector3 undershoot = original * downScale;

        float halfDuration = totalDuration / 2f;
        float t = 0f;

        // 1️⃣ 커짐: original → overshoot
        while (t < halfDuration)
        {
            float ratio = t / halfDuration;
            target.localScale = Vector3.Lerp(original, overshoot, Mathf.SmoothStep(0f, 1f, ratio));
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;

        // 2️⃣ 작아짐: overshoot → undershoot
        while (t < halfDuration)
        {
            float ratio = t / halfDuration;
            target.localScale = Vector3.Lerp(overshoot, undershoot, Mathf.SmoothStep(0f, 1f, ratio));
            t += Time.deltaTime;
            yield return null;
        }

        // 끝난 후 undershoot 상태 유지
        target.localScale = undershoot;
    }

    IEnumerator HideAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        target.SetActive(false);
    }
    public void ShowFinalScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        scoreBoardUI.ShowScoreBoard(records, roundIndex);
    }
    
    public void ShowGameOverTextAndScore(Constants.PlayerRecord[] records, int roundIndex)
    {
        StartCoroutine(GameOverSequence(records, roundIndex));
        StartCoroutine(GameOverSequence(records, roundIndex));
    }
    
    private IEnumerator GameOverSequence(Constants.PlayerRecord[] records, int roundIndex)
    {
        countDownText.gameObject.SetActive(true);
        countDownText.color = Color.green;
        countDownText.text = "Game Over";
        StartCoroutine(PunchScale(countDownText.transform));
        
        yield return new WaitForSeconds(3f);
        
        ShowFinalScoreBoard(records, roundIndex);
    }
}
