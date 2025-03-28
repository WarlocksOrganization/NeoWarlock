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
        public Constants.PlayerRecord GetTopTotalScorePlayer()
        {
            int finalRound = Mathf.Max(0, currentRound - 1); // 음수 방지

            return playerRecords.Values
                .OrderByDescending(p => p.GetTotalScoreUpToRound(finalRound))
                .FirstOrDefault(); // 빈 리스트 안전 처리
        }

        public Constants.PlayerStats GetTopKillPlayer()
        {
            return playerStatsArray
                .OrderByDescending(p => p.kills + p.outKills)  // 혹은 p.kills만
                .First();
        }

        public Constants.PlayerStats GetTopDamagePlayer()
        {
            return playerStatsArray
                .OrderByDescending(p => p.damageDone)
                .First();
        }
    }

}