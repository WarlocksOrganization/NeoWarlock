// ✅ GameManager.cs - 전체 재작성
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using Mirror;
using Player;
using UnityEngine;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Database.LoadDataBase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private Constants.PlayerStats[] playerStatsArray;
        private List<int> deathOrder = new();
        private Dictionary<int, Constants.PlayerRecord> playerRecords = new();
        public int currentRound = 0;
        
        private HashSet<NetworkConnectionToClient> readyPlayers = new();

        public bool isLan = false;
        
        public void ResetRoundState()
        {
            roundEnded = false;
        }

        public void Init(PlayerCharacter[] characters)
        {
            playerStatsArray = new Constants.PlayerStats[characters.Length];
            playerRecords = new();

            for (int i = 0; i < characters.Length; i++)
            {
                var pc = characters[i];
                playerStatsArray[i] = new Constants.PlayerStats
                {
                    playerId = pc.playerId,
                    characterClass = pc.PLayerCharacterClass,
                    nickname = pc.nickname,
                    userId = pc.userId
                };

                playerRecords[pc.playerId] = new Constants.PlayerRecord
                {
                    playerId = pc.playerId,
                    nickname = pc.nickname,
                    characterClass = pc.PLayerCharacterClass,
                    userId = pc.userId
                };
            }

            deathOrder.Clear();
            currentRound = 0;
        }

        public void RecordDamage(int attackerId, int damage)
        {
            var stats = playerStatsArray.FirstOrDefault(p => p.playerId == attackerId);
            if (stats != null)
                stats.damageDone += damage;
        }

        public void RecordKill(int attackerId, bool isOutKill)
        {
            var stats = playerStatsArray.FirstOrDefault(p => p.playerId == attackerId);
            if (stats != null)
            {
                if (isOutKill) stats.outKills++;
                else stats.kills++;
            }
        }

        public void RecordDeath(int playerId)
        {
            if (!deathOrder.Contains(playerId))
                deathOrder.Add(playerId);
        }

        public List<(int playerId, int rank)> GetCurrentRoundRanks()
        {
            var result = new List<(int playerId, int rank)>();
            int totalPlayers = playerStatsArray.Length;
            int rank = totalPlayers;

            foreach (var id in deathOrder)
            {
                result.Add((id, rank));
                rank--;
            }

            foreach (var stats in playerStatsArray)
            {
                if (!deathOrder.Contains(stats.playerId))
                    result.Add((stats.playerId, 1));
            }

            return result.OrderBy(r => r.rank).ToList();
        }

        public void AddRoundResult(List<(int playerId, int kills, int outKills, int damageDone, int rank)> roundData)
        {
            foreach (var d in roundData)
            {
                var record = playerRecords[d.playerId];
                var roundStats = new Constants.RoundStats
                {
                    kills = d.kills,
                    outKills = d.outKills,
                    damageDone = d.damageDone,
                    rank = d.rank
                };
                record.roundStatsList.Add(roundStats);
            }
            currentRound++;
            
            // ✅ 라운드 종료 후 stats 초기화
            foreach (var stats in playerStatsArray)
            {
                stats.kills = 0;
                stats.outKills = 0;
                stats.damageDone = 0;
            }

            deathOrder.Clear();

            if (currentRound >= 3)
            {
                GameResult();
            }
        }

        public int GetScoreAtRound(Constants.PlayerRecord record, int roundIndex)
        {
            if (record == null || roundIndex >= record.roundStatsList.Count) return 0;
            var r = record.roundStatsList[roundIndex];
            return r.kills * 200 + r.outKills * 300 + r.damageDone + GetRankBonus(r.rank);
        }

        public int GetRankBonus(int rank) => rank switch
        {
            1 => 600,
            2 => 500,
            3 => 400,
            4 => 300,
            5 => 200,
            6 => 100,
            _ => 0
        };

        public List<Constants.PlayerRecord> GetSortedRecords(int roundInclusive)
        {
            return playerRecords.Values
                .OrderByDescending(p => p.GetTotalScoreUpToRound(roundInclusive))
                .ThenBy(p => p.playerId)
                .ToList();
        }

        public List<Constants.PlayerRecord> GetRoundOnlySortedRecords(int roundIndex)
        {
            return playerRecords.Values
                .Where(p => p.roundStatsList.Count > roundIndex)
                .OrderByDescending(p => GetScoreAtRound(p, roundIndex))
                .ThenBy(p => p.playerId)
                .ToList();
        }

        public Constants.PlayerRecord GetPlayerRecord(int playerId) => playerRecords[playerId];
        
        public void ResetRound() => currentRound = 0;

        public void Reset()
        {
            foreach (var stats in playerStatsArray)
            {
                stats.kills = 0;
                stats.outKills = 0;
                stats.damageDone = 0;
                stats.totalScore = 0;
                stats.roundRanks.Clear();
            }
            deathOrder.Clear();
        }
        
        public Constants.PlayerStats[] GetSortedPlayerStats()
        {
            return playerStatsArray.OrderByDescending(p => p.totalScore).ToArray();
        }

        public Constants.PlayerStats GetPlayerStats(int playerId)
        {
            return playerStatsArray.First(p => p.playerId == playerId);
        }
        
        public Constants.PlayerRecord[] GetAllPlayerRecords()
        {
            return playerRecords.Values.ToArray();
        }
        
        public List<int> GetAlivePlayers()
        {
            return playerStatsArray
                .Where(p => !deathOrder.Contains(p.playerId))
                .Select(p => p.playerId)
                .ToList();
        }
        
        public bool roundEnded = false;

        public void TryCheckGameOver()
        {
            if (roundEnded) return;

            var alive = GetAlivePlayers();
            if (alive.Count > 1) return;

            roundEnded = true;

            var roundRanks = GetCurrentRoundRanks();
            var roundData = roundRanks.Select(tuple =>
            {
                var stats = GetPlayerStats(tuple.playerId);
                return (tuple.playerId, stats.kills, stats.outKills, stats.damageDone, tuple.rank);
            }).ToList();

            AddRoundResult(roundData);

            foreach (var conn in NetworkServer.connections.Values)
            {
                conn.identity.GetComponent<GamePlayer>()?.RpcUpdateRound(currentRound);
                conn.identity.GetComponent<GamePlayer>()?.RpcSendFinalScore(GetAllPlayerRecords(), currentRound - 1);
            }
        }

        public void GameResult()
        {
            if (Application.platform != RuntimePlatform.LinuxServer)
            {
                Debug.LogWarning("[GameManager] 서버 모드에서 실행 중이 아닙니다.");
                return;
            }

            Debug.Log("[GameManager] 게임 종료. 결과 기록 중...");
            List<Dictionary<string, object>> playerLogs = new List<Dictionary<string, object>>();
            var allPlayerRecords = GameManager.Instance.GetAllPlayerRecords();
            foreach (Constants.PlayerRecord playerRecord in allPlayerRecords)
            {
                List<int> roundRanks = new List<int> 
                {
                    playerRecord.roundStatsList[0].rank,
                    playerRecord.roundStatsList[1].rank,
                    playerRecord.roundStatsList[2].rank
                };

                List<int> roundScore = new List<int> 
                {
                    playerRecord.GetScoreAtRound(0),
                    playerRecord.GetScoreAtRound(1),
                    playerRecord.GetScoreAtRound(2)
                };

                var gamePlayers = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);
                var player = gamePlayers.Where(p => p.UserId == playerRecord.userId)
                    .OrderByDescending(p => p.PlayerCards.Count())
                    .FirstOrDefault();

                if (player == null)
                {
                    Debug.LogWarning($"[GameManager] 플레이어를 찾을 수 없습니다. userId: {playerRecord.userId}");
                }
                else if (player.PlayerCards.Count() < 9)
                {
                    Debug.LogWarning($"[GameManager] 플레이어 카드 수가 부족합니다. 임의로 채웁니다. userId: {playerRecord.userId}");
                    int playerCardCount = player.PlayerCards.Count();
                    player.PlayerCards = new int[9]
                    {
                        playerCardCount > 0 ? player.PlayerCards[0] : 0,
                        playerCardCount > 1 ? player.PlayerCards[1] : 0,
                        playerCardCount > 2 ? player.PlayerCards[2] : 0,
                        playerCardCount > 3 ? player.PlayerCards[3] : 0,
                        playerCardCount > 4 ? player.PlayerCards[4] : 0,
                        playerCardCount > 5 ? player.PlayerCards[5] : 0,
                        playerCardCount > 6 ? player.PlayerCards[6] : 0,
                        playerCardCount > 7 ? player.PlayerCards[7] : 0,
                        playerCardCount > 8 ? player.PlayerCards[8] : 0
                    };
                }
                

                Dictionary<string, object> playerLog = new Dictionary<string, object>
                {
                    ["userId"] = playerRecord.userId,
                    ["classCode"] = (int)playerRecord.characterClass,
                    ["round1Set"] = new int[] {player.PlayerCards[0], player.PlayerCards[1], player.PlayerCards[2]},
                    ["round2Set"] = new int[] {player.PlayerCards[3], player.PlayerCards[4], player.PlayerCards[5]},
                    ["round3Set"] = new int[] {player.PlayerCards[6], player.PlayerCards[7], player.PlayerCards[8]},
                    ["roundRank"] = roundRanks,
                    ["roundScore"] = roundScore
                };
                playerLogs.Add(playerLog);
            }
            
            var manager = Networking.RoomManager.singleton as Networking.RoomManager;
            Dictionary<string, string> roomData = manager.GetRoomData();
            var socketManager = Networking.SocketManager.singleton as Networking.SocketManager;
            if (socketManager != null && socketManager.IsConnected())
            {
                int roomId = int.TryParse(roomData["roomId"], out roomId) ? roomId : 0;
                int gameId = int.TryParse(roomData["gameId"], out gameId) ? gameId : 0;
                socketManager.RequestGameEnd(roomId, gameId);
            }
            else
            {
                Debug.LogWarning("[GameManager] SocketManager가 존재하지 않거나 연결되지 않았습니다.");
            }
            FileLogger.LogGameEnd(manager.GetMapId(), playerLogs.Count(), playerLogs);
        }
    }
}