using System.Linq;
using DataSystem;
using DataSystem.Database;
using Player;
using UnityEngine;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // 새로운 GameObject를 생성하고 GameManager 컴포넌트를 추가
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            OnAwake();
        }
        #endregion

        void OnAwake()
        {
            Database.LoadDataBase();
        }
        
        public Constants.PlayerStats[] PlayerStatsArray { get; private set; }
        
        public void Init(PlayerCharacter[] characters)
        {
            PlayerStatsArray = new Constants.PlayerStats[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                var pc = characters[i];
                PlayerStatsArray[i] = new Constants.PlayerStats
                {
                    playerId = pc.playerId,
                    characterClass = pc.PLayerCharacterClass,
                    nickname = pc.nickname
                };
            }
        }

        public void RecordDamage(int attackerId, int damage)
        {
            var stats = PlayerStatsArray.FirstOrDefault(p => p.playerId == attackerId);
            if (stats != null)
            {
                stats.damageDone += damage;
            }
        }
        
        
        public void RecordKill(int attackerId, bool isOutKill)
        {
            var stats = PlayerStatsArray.FirstOrDefault(p => p.playerId == attackerId);
            if (stats != null)
            {
                if (isOutKill)
                    stats.outKills += 1;
                else
                    stats.kills += 1;
            }

            Debug.Log(11111);
        }
        
        public void CalculateTotalScores()
        {
            foreach (var stats in PlayerStatsArray)
            {
                // 예시 점수 기준
                stats.totalScore = stats.kills * 100 + stats.outKills * 200 + stats.damageDone;

                // 순위별 가산점 (예: 1등 = 30, 2등 = 20, 3등 = 10)
                for (int i = 0; i < stats.roundRanks.Count; i++)
                {
                    int rank = stats.roundRanks[i];
                    if (rank == 1) stats.totalScore += 300;
                    else if (rank == 2) stats.totalScore += 200;
                    else if (rank == 3) stats.totalScore += 100;
                }
            }
        }

        public Constants.PlayerStats[] GetSortedPlayerStats()
        {
            return PlayerStatsArray;
        }
    }
}