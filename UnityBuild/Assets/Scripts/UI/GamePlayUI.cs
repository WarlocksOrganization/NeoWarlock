using System;
using System.Linq;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using UnityEngine;

public class GamePlayUI : GameLobbyUI
{
    public override void UpdatePlayerInRoon()
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
            //Debug.Log($"[GameLobbyUI] 내 PlayerNum: {PlayerSetting.PlayerNum}");
        }

        // ✅ 게임 방 인원 수 업데이트
        GameRoomData gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null)
        {
            int maxPlayers = gameRoomData.maxPlayerCount; // ✅ 최대 인원 가져오기
            PlayerInRoonText.text = $"현재 인원 {PlayerCharacters.Length} / {maxPlayers}";
        }
        
        playerStatusUI.Setup(foundCharacters);
    }
}
