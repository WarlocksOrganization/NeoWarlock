using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using Networking;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLobbyUI : MonoBehaviour
{
    public TMP_Text RoomNameText;
    [SerializeField] protected TMP_Text PlayerInRoonText;
    [SerializeField] protected GameObject PlayerSelection;
    [SerializeField] protected PlayerStatusUI playerStatusUI;

    [Header("Map")] [SerializeField] protected Button StartGameButton;
    [SerializeField] protected Button ChangeMapNextButton;
    [SerializeField] protected Button ChangeMapBeforeButton;
    [SerializeField] protected Image MapImage;
    [SerializeField] protected TMP_Text MapName;

    public GameObject[] PlayerCharacters;
    protected PlayerCharacter[] foundCharacters;

    private readonly int hostNum = 0;

    [SerializeField] protected KillLogUI killLogUI;

    [SerializeField] private TMP_Text warningText;
    private Coroutine warningCoroutine;

    [Header("Team")] [SerializeField] protected Button ChangeTeamButton;

    private void Start()
    {
        if (NetworkClient.active) PlayerSelection.SetActive(true);
        CheckIfHost();
        
        if (NetworkServer.active)
        {
            AudioManager.Instance.SetBGMVolume(0f); // BGM 볼륨 0
            AudioManager.Instance.SetSFXVolume(0f); // SFX 볼륨 0
        }
        
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_Lobby);

        ChangeTeamButton.onClick.AddListener(OnClickChangeTeam);
    }

    public void OpenPlayerSelection()
    {
        PlayerSelection.SetActive(true);
    }

    public virtual void UpdatePlayerInRoon()
    {
        Debug.Log("UpdatePlayerInRoon 실행");
        // ✅ 모든 PlayerCharacter 가져오기
        foundCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => p.playerId >= 0)
            .OrderBy(p => p.GetComponent<NetworkIdentity>().netId)
            .ToArray();

        // ✅ PlayerCharacters 배열 재구성
        var maxPlayerId = foundCharacters.Length > 0 ? foundCharacters.Max(p => p.playerId) : 0;
        PlayerCharacters = new GameObject[maxPlayerId + 1];

        foreach (var player in foundCharacters)
            if (player.playerId >= 0 && player.playerId < PlayerCharacters.Length)
                PlayerCharacters[player.playerId] = player.gameObject;

        // ✅ 내 플레이어 찾기
        var myPlayer = foundCharacters.FirstOrDefault(p => p.isOwned);
        if (myPlayer != null) PlayerSetting.PlayerId = myPlayer.playerId;

        // ✅ GameRoomData 가져와서 최대 인원, 팀 타입 확인
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null)
        {
            PlayerInRoonText.text = $"현재 인원 {foundCharacters.Length} / {gameRoomData.maxPlayerCount}";

            if (gameRoomData.roomType == Constants.RoomType.Team && myPlayer != null)
            {
                if (myPlayer.team == Constants.TeamType.None)
                {
                    var teamACount = foundCharacters.Count(p => p.team == Constants.TeamType.TeamA);
                    var teamBCount = foundCharacters.Count(p => p.team == Constants.TeamType.TeamB);

                    var assignedTeam = teamACount > teamBCount ? Constants.TeamType.TeamB : Constants.TeamType.TeamA;
                    myPlayer.CmdSetTeam(assignedTeam);
                    PlayerSetting.TeamType = assignedTeam;
                }
            }
            else
            {
                PlayerSetting.TeamType = Constants.TeamType.None;
            }

            // 팀 변경 버튼 숨기기 (solo 방인 경우)
            if (gameRoomData.roomType == Constants.RoomType.Solo) ChangeTeamButton.gameObject.SetActive(false);
        }

        // ✅ 상태창 UI 초기화
        playerStatusUI.Setup(foundCharacters, PlayerSetting.PlayerId);

        // ✅ 방장 여부 확인하여 버튼 활성화
        CheckIfHost(PlayerSetting.PlayerId);

        if (myPlayer)
        {
            Debug.Log("UpdatePlayerInRoon : " + myPlayer.playerId + ", " + myPlayer.nickname + ", " + foundCharacters.Length);
        }
    }


    public void OnServerPlayerListUpdated(string netIdListStr)
    {
        var parts = netIdListStr.Split(',');
        Dictionary<uint, int> netIdsDict = new Dictionary<uint, int>();
        List<uint> netIds = new List<uint>(parts.Length);
        int idx = 0;
        foreach (var part in parts)
        {
            var subParts = part.Split(':');
            if (subParts.Length == 2 && uint.TryParse(subParts[0], out var netId) && int.TryParse(subParts[1], out var playerId))
            {
                netIdsDict[netId] = playerId;
                netIds[idx++] = netId;
            }
        }

        netIds.Sort();
        var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
        var orderedPlayers = netIds
            .Select(id => allPlayers.FirstOrDefault(p => p.GetComponent<NetworkIdentity>().netId == id))
            .Where(p => p != null)
            .OrderBy(p => p.GetComponent<NetworkIdentity>().netId)
            .ToArray();
            
        PlayerCharacters = orderedPlayers.Select(p => p.gameObject)
            .OrderBy(p => p.GetComponent<NetworkIdentity>().netId)
            .ToArray();
        foundCharacters = orderedPlayers;

        // 내 플레이어 식별
        var myPlayer = orderedPlayers.FirstOrDefault(p => p.isOwned);
        if (myPlayer != null)
        {
            // var myIndex = Array.IndexOf(PlayerCharacters, myPlayer.gameObject);
            int myIndex = netIdsDict[myPlayer.GetComponent<NetworkIdentity>().netId];
            PlayerSetting.PlayerId = myIndex;
        }

        // UI 표시
        var roomData = FindFirstObjectByType<GameRoomData>();
        PlayerInRoonText.text = $"현재 인원 {PlayerCharacters.Length} / {roomData.maxPlayerCount}";

        playerStatusUI.Setup(orderedPlayers, PlayerSetting.PlayerId);
        CheckIfHost(PlayerSetting.PlayerId);
    }

    // ✅ 방장인지 확인 후 버튼 활성화
    private void CheckIfHost(int playerNum = -1)
    {
        if (StartGameButton == null || ChangeMapNextButton == null || ChangeMapBeforeButton == null)
        {
            Debug.LogWarning("[CheckIfHost] UI 요소가 아직 초기화되지 않음");
            return;
        }

        if (NetworkServer.active || playerNum == hostNum)
        {
            StartGameButton.gameObject.SetActive(true);
            ChangeMapNextButton.gameObject.SetActive(true);
            ChangeMapBeforeButton.gameObject.SetActive(true);

            StartGameButton.onClick.AddListener(StartGame);
        }
        else
        {
            StartGameButton.gameObject.SetActive(false);
            ChangeMapBeforeButton.gameObject.SetActive(false);
            ChangeMapNextButton.gameObject.SetActive(false);
        }
    }

    // ✅ 방장이 게임 시작 버튼을 클릭하면 실행
    private void StartGame()
    {
        if (NetworkServer.active)
        {
            // ✅ 게임 시작 전에 플레이어 정보로 Stats 초기화
            var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            if (GameManager.Instance != null) GameManager.Instance.Init(allPlayers);

            var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            var allReady = players.All(p => p.State == Constants.PlayerState.Start);

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
        if (killerId < 0) killerId = deadId;

        if (foundCharacters == null ||
            deadId < 0 || deadId >= foundCharacters.Length ||
            killerId < 0 || killerId >= foundCharacters.Length)
        {
            Debug.LogWarning(
                $"[UpdateKillLog] 잘못된 인덱스 접근: deadId={deadId}, killerId={killerId}, foundCharacters.Length={foundCharacters?.Length}");
            return;
        }

        killLogUI?.AddKillLog(foundCharacters[killerId], foundCharacters[deadId], skillid, isFall);
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
        var originalColor = warningText.color;
        originalColor.a = 1f;
        warningText.color = originalColor;

        var duration = 2.5f; // 사라지는 데 걸리는 시간
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            warningText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        warningText.text = "";
    }

    public void OnClickChangeMap(bool next)
    {
        if (!NetworkClient.active) return;

        var player = NetworkClient.connection.identity.GetComponent<RoomPlayer>();
        player.CmdChangeMap(next);
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
    }

    public virtual void UpdateMapUI(Constants.RoomMapType type)
    {
        var config = Database.GetMapConfig(type);

        if (config == null || MapImage == null || MapName == null)
        {
            Debug.LogWarning("[UpdateMapUI] UI 요소가 아직 준비되지 않았습니다.");
            return;
        }

        Debug.Log(config?.mapName);
        MapImage.sprite = config?.mapSprite; // 또는 따로 image 설정
        MapName.text = config?.mapName;
    }

    private void OnClickChangeTeam()
    {
        var localPlayer = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isOwned);

        if (localPlayer != null)
        {
            // 현재 팀 반대로 전환
            var newTeam = localPlayer.team == Constants.TeamType.TeamA
                ? Constants.TeamType.TeamB
                : Constants.TeamType.TeamA;
            localPlayer.CmdSetTeam(newTeam);
            PlayerSetting.TeamType = newTeam;
        }
    }
}