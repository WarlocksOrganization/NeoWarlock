using System;
using System.Collections;
using System.Linq;
using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using Player;
using Telepathy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLobbyUI : MonoBehaviour
{
    public TMP_Text RoomNameText;
    [SerializeField] protected TMP_Text PlayerInRoonText;
    [SerializeField] protected GameObject PlayerSelection;
    [SerializeField] protected Button StartGameButton; // ✅ 게임 시작 버튼
    [SerializeField] protected PlayerStatusUI playerStatusUI;

    public GameObject[] PlayerCharacters;
    protected PlayerCharacter[] foundCharacters;

    private int hostNum = 0;
    
    [SerializeField] protected KillLogUI killLogUI;
    
    [SerializeField] private TMP_Text warningText;
    private Coroutine warningCoroutine;

    private void Start()
    {
        if (NetworkClient.active)
        {
            PlayerSelection.SetActive(true);
        }
        CheckIfHost();
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_Lobby);
    }

    public void OpenPlayerSelection()
    {
        PlayerSelection.SetActive(true);
    }

    public virtual void UpdatePlayerInRoon()
    {
        // ✅ 현재 씬에서 모든 PlayerCharacter 찾기
        foundCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .OrderBy(player => player.GetComponent<NetworkIdentity>().netId)
            .ToArray();
        
        PlayerCharacters = foundCharacters
            .Select(player => player.gameObject) // GameObject만 배열에 저장
            .ToArray();


        // ✅ 본인의 플레이어 번호 찾기
        var myPlayer = foundCharacters.FirstOrDefault(p => p.isOwned);
        if (myPlayer != null)
        {
            int myIndex = Array.IndexOf(PlayerCharacters, myPlayer.gameObject);
            PlayerSetting.PlayerId = myIndex;
            //Debug.Log($"[GameLobbyUI] 내 PlayerNum: {PlayerSetting.PlayerNum}");
        }

        // ✅ 게임 방 인원 수 업데이트
        GameRoomData gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null)
        {
            int maxPlayers = gameRoomData.maxPlayerCount; // ✅ 최대 인원 가져오기
            PlayerInRoonText.text = $"현재 인원 {PlayerCharacters.Length} / {maxPlayers}";
        }
        
        playerStatusUI.Setup(foundCharacters, PlayerSetting.PlayerId);

        // ✅ 방장인지 확인 후 버튼 활성화
        CheckIfHost(PlayerSetting.PlayerId);
    }
    
    // ✅ 방장인지 확인 후 버튼 활성화
    private void CheckIfHost(int playerNum = -1)
    {   
        if (NetworkServer.active || playerNum == hostNum) // ✅ 방장인지 확인
        {
            StartGameButton.gameObject.SetActive(true);
            StartGameButton.onClick.AddListener(StartGame); // ✅ 버튼 클릭 이벤트 추가
        }
        else
        {
            StartGameButton.gameObject.SetActive(false);
        }
    }

    // ✅ 방장이 게임 시작 버튼을 클릭하면 실행
    private void StartGame()
    {
        if (NetworkServer.active)
        {
            // ✅ 게임 시작 전에 플레이어 정보로 Stats 초기화
            var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Init(allPlayers);
            }
            
            var players = FindObjectsOfType<PlayerCharacter>();
            bool allReady = players.All(p => p.State == Constants.PlayerState.Start);

            if (!allReady)
            {
                ShowWarningMessage("아직 준비되지 않은 플레이어가 있습니다.");
                return;
            }

            (NetworkManager.singleton as RoomManager).StartGame();
        }
        else
        {
            PlayerCharacters[0].GetComponent<PlayerCharacter>().CmdStartGame();
        }
    }

    public void UpdateKillLog(int deadId, int skillid, int killerId, bool isFall)
    {
        if (killerId < 0)
        {
            killerId = deadId;
        }
        killLogUI?.AddKillLog(foundCharacters[killerId], foundCharacters[deadId], skillid, isFall);

        UpdatePlayerInRoon();
    }
    
    private void ShowWarningMessage(string message)
    {
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(FadeOutWarning(message));
    }
    
    private IEnumerator FadeOutWarning(string message)
    {
        warningText.text = message;
        Color originalColor = warningText.color;
        originalColor.a = 1f;
        warningText.color = originalColor;

        float duration = 2.5f; // 사라지는 데 걸리는 시간
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            warningText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        warningText.text = "";
    }
}
