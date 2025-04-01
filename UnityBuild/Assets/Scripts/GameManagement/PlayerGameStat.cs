using System;
using Mirror;
using DataSystem;
using UnityEngine;
using System.Collections.Generic;

namespace GameManagement
{
    [Serializable]
    public class PlayerGameStats
    {
        public string nickname;
        public string userId;
        public List<int> selectedCards;
        public Constants.CharacterClass characterClass;
        public int kills;
        public int deaths;
        public float totalDamage;
        public int ranking;
        public int score;

        public PlayerGameStats()
        {
        }

        public PlayerGameStats(string nickname, string userId = "0")
        {
            this.nickname = nickname;
            this.userId = userId;
            this.characterClass = Constants.CharacterClass.None;
            this.selectedCards = new List<int>();
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