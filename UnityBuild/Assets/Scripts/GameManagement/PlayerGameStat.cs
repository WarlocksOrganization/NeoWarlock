using System;
using Mirror;
using DataSystem;
using UnityEngine;

namespace GameManagement
{
    [Serializable]
    public class PlayerGameStats
    {
        public string nickname;
        public Constants.CharacterClass characterClass;
        public int kills;
        public int deaths;
        public float totalDamage;
        public int ranking;
        public int score;

        public PlayerGameStats()
        {
        }

        public PlayerGameStats(string nickname)
        {
            this.nickname = nickname;
            this.characterClass = Constants.CharacterClass.None;
            this.kills = 0;
            this.deaths = 0;
            this.totalDamage = 0f;
            this.ranking = 0;
            this.score = 0;
        }

        public void AddKill()
        {
            kills++;
            score += 10; // ✅ 킬당 점수 증가 (예제 값)
        }

        public void AddDeath()
        {
            deaths++;
        }

        public void AddDamage(float damage)
        {
            totalDamage += damage;
        }

        public void UpdateRanking(int rank)
        {
            ranking = rank;
        }
    }
}