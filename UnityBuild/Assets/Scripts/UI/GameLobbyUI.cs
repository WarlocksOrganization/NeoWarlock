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

    protected Dictionary<int, PlayerCharacter> foundCharactersDict = new();

    private readonly int hostNum = 0;

    [SerializeField] protected KillLogUI killLogUI;

    [SerializeField] private TMP_Text warningText;
    private Coroutine warningCoroutine;

    [Header("Team")] [SerializeField] protected Button ChangeTeamButton;

    private void Start()
    {
        if (NetworkClient.active) PlayerSelection.SetActive(true);

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

        // 1. playerId가 유효한 캐릭터 수집
        var characters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => p.playerId >= 0)
            .ToList();

        // 2. 딕셔너리 생성
        foundCharactersDict = characters.ToDictionary(p => p.playerId, p => p);

        // 3. 배열 초기화 (playerId 순서 유지)
        var maxPlayerId = foundCharactersDict.Keys.Count > 0 ? foundCharactersDict.Keys.Max() : 0;

        // 4. 내 플레이어 찾기
        var myPlayer = foundCharactersDict.Values.FirstOrDefault(p => p.isOwned);

        // 5. UI 표시
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null)
        {
            PlayerInRoonText.text = $"현재 인원 {foundCharactersDict.Count} / {gameRoomData.maxPlayerCount}";

            if (gameRoomData.roomType == Constants.RoomType.Team && myPlayer != null &&
                myPlayer.team == Constants.TeamType.None)
            {
                var teamACount = foundCharactersDict.Values.Count(p => p.team == Constants.TeamType.TeamA);
                var teamBCount = foundCharactersDict.Values.Count(p => p.team == Constants.TeamType.TeamB);

                var assignedTeam = teamACount > teamBCount ? Constants.TeamType.TeamB : Constants.TeamType.TeamA;

                myPlayer.CmdSetTeam(assignedTeam);
                PlayerSetting.TeamType = assignedTeam;
            }

            if (gameRoomData.roomType == Constants.RoomType.Solo)
            {
                ChangeTeamButton.gameObject.SetActive(false);
                PlayerSetting.TeamType = Constants.TeamType.None;
            }
            
        }

        // 6. 상태창 UI 업데이트
        if (myPlayer != null)
            playerStatusUI.Setup(foundCharactersDict, myPlayer.playerId);

        if (myPlayer != null)
            Debug.Log(
                $"UpdatePlayerInRoon : playerId={myPlayer.playerId}, nickname={myPlayer.nickname}, 총 인원={foundCharactersDict.Count}");
    }


    public void OnServerPlayerListUpdated(string playerIdsStr)
    {
        foundCharactersDict.Clear();

        var idStrings = playerIdsStr.Split(',');
        var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);

        foreach (var idStr in idStrings)
        {
            if (int.TryParse(idStr, out int playerId))
            {
                var player = allPlayers.FirstOrDefault(p => p.playerId == playerId);
                if (player != null)
                {
                    foundCharactersDict[playerId] = player;
                }
            }
        }

        // 필요 시 정렬된 배열 생성
        var orderedCharacters = foundCharactersDict.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

        var myPlayer = orderedCharacters.FirstOrDefault(p => p.isOwned);
        if (myPlayer != null)
        {
            playerStatusUI.Setup(foundCharactersDict, myPlayer.playerId);
            CheckIfHost(myPlayer.playerId);
        }
    }

    // ✅ 방장인지 확인 후 버튼 활성화
    private void CheckIfHost(int myPlayerId)
    {
        if (StartGameButton == null || ChangeMapNextButton == null || ChangeMapBeforeButton == null)
        {
            Debug.LogWarning("[CheckIfHost] UI 요소가 아직 초기화되지 않음");
            return;
        }

        // 호스트 판별: 가장 작은 playerId가 호스트
        int minPlayerId = foundCharactersDict.Keys.Min();

        if (myPlayerId == minPlayerId)
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
            var hostEntry = foundCharactersDict.OrderBy(kv => kv.Key).FirstOrDefault();
            if (hostEntry.Value != null)
            {
                hostEntry.Value.CmdStartGame();
            }
            else
            {
                Debug.LogWarning("[GameLobbyUI] 방장을 찾을 수 없습니다. 플레이어가 없을 수 있습니다.");
            }
        }
    }

    public void UpdateKillLog(int deadId, int skillId, int killerId, bool isFall)
    {
        if (killerId < 0) killerId = deadId;
        
        if (!foundCharactersDict.TryGetValue(deadId, out var deadPlayer) ||
            !foundCharactersDict.TryGetValue(killerId, out var killerPlayer))
        {
            Debug.LogWarning(
                $"[UpdateKillLog] 잘못된 플레이어 ID 접근: deadId={deadId}, killerId={killerId}");
            return;
        }

        killLogUI?.AddKillLog(killerPlayer, deadPlayer, skillId, isFall);
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