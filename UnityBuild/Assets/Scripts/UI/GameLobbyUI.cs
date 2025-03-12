using System;
using System.Linq;
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
    public TMP_Text PlayerInRoonText;
    [SerializeField] private GameObject PlayerSelection;
    [SerializeField] private Button StartGameButton; // ✅ 게임 시작 버튼

    public GameObject[] PlayerCharacters;

    private void Start()
    {
        PlayerSelection.SetActive(true);
        CheckIfHost();
    }

    public void OpenPlayerSelection()
    {
        PlayerSelection.SetActive(true);
    }

    public void UpdatePlayerInRoon()
    {
        // ✅ 현재 씬에서 모든 PlayerCharacter 찾기
        PlayerCharacter[] foundCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);

        // ✅ netId 기준으로 정렬
        PlayerCharacters = foundCharacters
            .OrderBy(player => player.GetComponent<NetworkIdentity>().netId)
            .Select(player => player.gameObject) // GameObject만 배열에 저장
            .ToArray();

        // ✅ 본인의 플레이어 번호 찾기
        var myPlayer = foundCharacters.FirstOrDefault(p => p.isOwned);
        if (myPlayer != null)
        {
            int myIndex = Array.IndexOf(PlayerCharacters, myPlayer.gameObject);
            PlayerSetting.PlayerNum = myIndex;
            Debug.Log($"[GameLobbyUI] 내 PlayerNum: {PlayerSetting.PlayerNum}");
        }

        // ✅ 게임 방 인원 수 업데이트
        GameRoomData gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null)
        {
            int maxPlayers = gameRoomData.maxPlayerCount; // ✅ 최대 인원 가져오기
            PlayerInRoonText.text = $"현재 인원 {PlayerCharacters.Length} / {maxPlayers}";
        }
    }
    
    // ✅ 방장인지 확인 후 버튼 활성화
    private void CheckIfHost()
    {
        if (NetworkServer.active) // ✅ 방장인지 확인
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
    public void StartGame()
    {
        if (NetworkServer.active) // ✅ 방장인지 확인
        {
            (NetworkManager.singleton as RoomManager).StartGame();
        }
    }

}
