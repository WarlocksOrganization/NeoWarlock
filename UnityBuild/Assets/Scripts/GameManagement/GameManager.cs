// ✅ GameManager.cs - 수정된 버전

using System.Collections;
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

        public static Dictionary<string, int[]> playerCards = new Dictionary<string, int[]>();
        public bool isCardSelectionStarted = false;
        public bool roundEnded = false;
        private bool isCheckingGameOver = false;

        private Dictionary<int, Constants.PlayerStats> playerStatsDict = new();
        private List<int> deathOrder = new();
        private Dictionary<int, Constants.PlayerRecord> playerRecords = new();
        public int currentRound = 0;

        private List<(int playerId, int rank)> roundRanks;
        private List<(int playerId, int kills, int outKills, int damageDone, int rank)> roundData;

        public bool isLan = false;
        

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Database.LoadDataBase();
                Application.runInBackground = true;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ResetRoundState() => roundEnded = false;
        public void ResetRound() => currentRound = 0;

        public void Init(PlayerCharacter[] characters)
        {
            playerStatsDict.Clear();
            playerRecords.Clear();

            foreach (var pc in characters)
            {
                playerStatsDict[pc.playerId] = new Constants.PlayerStats
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
                    userId = pc.userId,
                    team = pc.team,
                };
            }

            deathOrder.Clear();
            currentRound = 0;
            playerCards.Clear();
        }

        public void RecordDamage(int attackerId, int damage)
        {
            playerStatsDict.TryGetValue(attackerId, out var stats);
            if (stats != null) stats.damageDone += damage;
        }

        public void RecordKill(int attackerId, bool isOutKill)
        {
            playerStatsDict.TryGetValue(attackerId, out var stats);
            if (stats != null)
            {
                if (isOutKill) stats.outKills++;
                else stats.kills++;
            }
        }

        public void RecordDeath(int playerId)
        {
            if (!deathOrder.Contains(playerId))
            {
                deathOrder.Add(playerId);
                Debug.Log($"[RecordDeath] 플레이어 {playerId} 사망 기록됨");
            }
        }

        public List<(int playerId, int rank)> GetCurrentRoundRanks()
        {
            var result = new List<(int playerId, int rank)>();
            int totalPlayers = playerStatsDict.Count;
            int rank = totalPlayers;

            foreach (var id in deathOrder)
            {
                result.Add((id, rank));
                rank--;
            }

            foreach (var (playerId, stats) in playerStatsDict)
            {
                if (!deathOrder.Contains(playerId))
                    result.Add((playerId, 1));
            }

            return result.OrderBy(r => r.rank).ToList();
        }

        public void AddRoundResult(List<(int playerId, int kills, int outKills, int damageDone, int rank)> roundData)
        {
            foreach (var d in roundData)
            {
                if (!playerRecords.TryGetValue(d.playerId, out var record)) continue;

                var score = d.kills * 200 + d.outKills * 300 + d.damageDone + GetRankBonus(d.rank);
                Debug.Log($"[AddRoundResult] PlayerId: {d.playerId}, Kills: {d.kills}, Rank: {d.rank}, Score: {score}");

                record.roundStatsList.Add(new Constants.RoundStats
                {
                    kills = d.kills,
                    outKills = d.outKills,
                    damageDone = d.damageDone,
                    rank = d.rank,
                    score = score
                });
            }

            currentRound++;

            foreach (var (playerId, stats) in playerStatsDict)
            {
                stats.kills = 0;
                stats.outKills = 0;
                stats.damageDone = 0;
            }

            deathOrder.Clear();

            if (currentRound >= 3) GameResult();
        }
        
        public int GetRankBonus(int rank)
        {
            int totalPlayers = playerRecords?.Count ?? 0;

            if (totalPlayers == 0)
            {
                Debug.LogWarning("[GameManager] GetRankBonus 호출 시 totalPlayers가 0입니다.");
                return 0;
            }

            // 1등은 totalPlayers * 100점, 꼴등은 100점
            int bonus = Mathf.Max(100, (totalPlayers - rank + 1) * 100);
            return bonus;
        }


        public void TryCheckGameOver()
        {
            if (!NetworkServer.active || roundEnded || isCheckingGameOver) return;
            StartCoroutine(DelayedGameOverCheck());
        }

        private IEnumerator DelayedGameOverCheck()
        {
            isCheckingGameOver = true;
            yield return new WaitForSeconds(0.5f);

            var alivePlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
                .Where(p => !p.isDead && p.curHp > 0)
                .ToList();
            
            Debug.Log($"[DelayedGameOverCheck] 생존인원 : {alivePlayers} ");

            var room = FindFirstObjectByType<GameRoomData>();
            bool isTeamGame = room != null && room.roomType == Constants.RoomType.Team;
            bool isRaidGame = room != null && room.roomType == Constants.RoomType.Raid;
            
            if (alivePlayers.Count == 0)
            {
                Debug.Log("[GameOverCheck] 모든 플레이어 사망 → 게임 종료");

                roundEnded = true;
                roundRanks = GetCurrentRoundRanks();
                roundData = roundRanks.Select(tuple =>
                {
                    var stats = GetPlayerStats(tuple.playerId);
                    return (tuple.playerId, stats.kills, stats.outKills, stats.damageDone, tuple.rank);
                }).ToList();

                AddRoundResult(roundData);

                foreach (var conn in NetworkServer.connections.Values)
                {
                    var player = conn.identity.GetComponent<GamePlayer>();
                    player?.RpcPrepareScoreBoard();
                    player?.RpcSendFinalScore(GetAllPlayerRecords(), currentRound - 1);
                }

                StartCoroutine(ServerRoundTransition());
                isCheckingGameOver = false;
                yield break;
            }

            if (isRaidGame)
            {
                DragonAI dragon = FindFirstObjectByType<DragonAI>();
                bool isDragonAlive = dragon != null && dragon.curHp > 0;
                int alivePlayerCount = alivePlayers.Count;

                if ((isDragonAlive && alivePlayerCount >= 1) || (!isDragonAlive && alivePlayerCount > 1))
                {
                    // 아직 게임 끝나지 않음
                    isCheckingGameOver = false;
                    yield break;
                }
            }
            else if (isTeamGame)
            {
                var aliveTeamSet = new HashSet<Constants.TeamType>(
                    alivePlayers.Select(p => p.team).Where(t => t != Constants.TeamType.None)
                );

                if (aliveTeamSet.Count > 1)
                {
                    isCheckingGameOver = false;
                    yield break;
                }
            }
            else
            {
                if (alivePlayers.Count > 1)
                {
                    isCheckingGameOver = false;
                    yield break;
                }
            }

            roundEnded = true;
            roundRanks = GetCurrentRoundRanks();
            roundData = roundRanks.Select(tuple =>
            {
                var stats = GetPlayerStats(tuple.playerId);
                return (tuple.playerId, stats.kills, stats.outKills, stats.damageDone, tuple.rank);
            }).ToList();

            AddRoundResult(roundData);

            foreach (var conn in NetworkServer.connections.Values)
            {
                var player = conn.identity.GetComponent<GamePlayer>();
                player?.RpcPrepareScoreBoard();
                player?.RpcSendFinalScore(GetAllPlayerRecords(), currentRound - 1);
            }

            StartCoroutine(ServerRoundTransition());
            isCheckingGameOver = false;
        }

        private IEnumerator ServerRoundTransition()
        {
            yield return new WaitForSeconds(Constants.ScoreBoardTime);

            FindFirstObjectByType<GameRoomData>()?.PrepareNextRound();
            isCardSelectionStarted = false;
            yield return new WaitForSeconds(2f);

            var gameRoomData = FindFirstObjectByType<GameRoomData>();
            if (gameRoomData == null) yield break;

            if (currentRound < 3)
            {
                gameRoomData.StartNextRound();

                var playerList = FindObjectsByType<GamePlayer>(FindObjectsSortMode.None);
                foreach (var p in playerList)
                {
                    if (p.isServer)
                        p.StartCoroutine(p.WaitForAllPlayersThenStartCardSelection());
                }
            }
            else
            {
                gameRoomData.EndGame();
            }
        }

        public void SetPlayerCards(string userId, int[] cards)
        {
            if (playerCards.ContainsKey(userId)) playerCards[userId] = cards;
            else playerCards.Add(userId, cards);
        }

        public void GameResult()
        {
            Debug.Log("[GameManager] 게임 종료. 결과 기록 중...");
            // 결과 기록 로직 생략
        }

        public void Reset()
        {
            foreach (var (playerId, stats) in playerStatsDict)
            {
                stats.kills = 0;
                stats.outKills = 0;
                stats.damageDone = 0;
                stats.totalScore = 0;
                stats.roundRanks.Clear();
            }
            deathOrder.Clear();
        }

        public void ResetRoundStateOnly()
        {
            foreach (var (playerId, stats) in playerStatsDict)
            {
                stats.kills = 0;
                stats.outKills = 0;
                stats.damageDone = 0;
                stats.curHp = stats.isDead ? 0 : stats.curHp;
                stats.isDead = false;
            }
            deathOrder.Clear();
            roundEnded = false;
        }

        public Constants.PlayerStats GetPlayerStats(int playerId)
        {
            if (playerStatsDict.TryGetValue(playerId, out var stats))
                return stats;

            Debug.LogWarning($"[GameManager] GetPlayerStats - 존재하지 않는 playerId 요청: {playerId}");
            return null; // 또는 예외 처리
        }

        public Constants.PlayerRecord[] GetAllPlayerRecords() => playerRecords.Values.ToArray().OrderBy(r => r.playerId).ToArray();
    }
}
