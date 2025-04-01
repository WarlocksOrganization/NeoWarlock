using System.Linq;
using DataSystem;
using GameManagement;
using Interfaces;
using Mirror;
using Networking;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public partial class PlayerCharacter : IDamagable
    {
        [SerializeField] private PlayerHUD playerHUD;
        [SerializeField] private GameObject floatingDamageTextPrefab;

        [SyncVar(hook = nameof(OnHpChanged))] // ✅ Hook 추가
        public int curHp = 150;

        [SyncVar(hook = nameof(OnHpChanged))] public int maxHp = 150;
        public int MaxHp => maxHp;
        public int CurHp => curHp;

        [SyncVar] private int attackPlayersId = -1;
        [SyncVar] private int attackskillid = -1;
        [SyncVar(hook = nameof(OnDefenseChanged))] public int defense = 0; // ✅ 방어력 추가
        public int Defense => defense;
        
        public int takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int attackPlayerId, int attackskillid)
        {
            if (!isServer || State == Constants.PlayerState.NotReady) return 0;
            if (curHp <= 0) return 0;
            
            if (damage > 0)
            {
                damage = Mathf.Max(damage - defense, 0);
            }
            DecreaseHp(damage, attackPlayerId, attackskillid);

            if (curHp > 0)
            {
                Vector3 direction = transform.position - attackTran;
                direction.y = 0;
                direction = direction.normalized;

                if (knockbackForce != 0)
                {
                    Debug.Log(knockbackForce);
                    RpcApplyKnockback(direction * knockbackForce * Constants.KnockBackFactor);
                    RpcTriggerAnimation("isHit"); 
                }

                if (attackConfig != null && attackConfig.appliedBuff != null)
                {
                    ApplyBuffFromAttack(attackConfig.appliedBuff, attackPlayerId, attackskillid);
                }
            }

            return damage;
        }
        private void OnDefenseChanged(int oldVal, int newVal) => NotifyStatChanged();

        public void DecreaseHp(int damage, int attackPlayerId, int attackskillid)
        {
            if (curHp <= 0) return;
            int predamage = damage;
            
            if (predamage > 0 && attackPlayerId >= 0 && playerId != attackPlayerId)
            {
                this.attackPlayersId = attackPlayerId;
                this.attackskillid = attackskillid;
                
                var gameplayUI = FindAnyObjectByType<GamePlayUI>();
                if (gameplayUI != null)
                {
                    GameManager.Instance.RecordDamage(attackPlayerId, damage);
                }
            }

            foreach (var player in FindObjectsByType<PlayerCharacter>(sortMode: FindObjectsSortMode.None))
            {
                if (player.playerId == attackPlayerId)
                {
                    FileLogger.LogSkillHit(player.userId, userId, damage, attackskillid.ToString());
                    break;
                }
            }

            if (damage > 0) // 🔹 체력 감소 (데미지 입음)
            {
                damage = Mathf.Min(damage, curHp); // 현재 체력보다 큰 데미지는 curHp만큼 감소
            }
            else if (damage < 0) // 🔹 체력 회복 (음수 데미지)
            {
                damage = Mathf.Max(damage, -(maxHp - curHp)); // maxHp 초과 회복 방지
            }

            curHp -= damage; // 🔹 체력 변경

            if (predamage != 0)
            {
                ShowFloatingDamageText(damage);
            }

            if (curHp == 0)
            {
                SetIsDead(true);
                RpcTriggerAnimation("isDead"); // 클라이언트에도 애니메이션 트리거 전송
                
                var gameplayUI = FindAnyObjectByType<GamePlayUI>();
                if (gameplayUI != null)
                {
                    bool isOutKill = attackskillid == 0;
                    if (!isOutKill && attackPlayerId >= 0)
                    {
                        GameManager.Instance.RecordKill(attackPlayerId, false);
                    }
                    else if (isOutKill && attackPlayersId >= 0)
                    {
                        GameManager.Instance.RecordKill(attackPlayersId, isOutKill);
                    }
                }
                GameManager.Instance.RecordDeath(playerId);
                
                var gp = connectionToClient.identity.GetComponent<GamePlayer>();
                if (gp != null && isServer)
                {
                    gp.CheckGameOver();
                }
                
                if (attackskillid == 0)
                {
                    RpcTransmitKillLog(attackPlayerId, this.attackskillid, true);
                }
                else if(attackPlayerId != playerId)
                {
                    RpcTransmitKillLog(attackPlayerId, attackskillid, false);
                }
                else
                {
                    RpcTransmitKillLog(-1, -1, false);
                }

                PlayerCharacter[] playerCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
                    .OrderBy(player => player.GetComponent<NetworkIdentity>().netId)
                    .ToArray();
                if (playerCharacters.Count() > attackPlayerId)
                {
                    string attackerUserId = attackPlayerId == -1 ? (attackPlayersId == -1 ? "0" : playerCharacters[attackPlayersId].userId) : playerCharacters[attackPlayerId].userId;
                    FileLogger.LogKill(attackerUserId, userId, attackskillid == 0 ? "outside" : "skill", attackskillid.ToString());
                }
            }
        }
        
        private void ApplyBuffFromAttack(BuffData buffData, int attackPlayerId, int attackskillid)
        {
            if (buffSystem != null)
            {
                if (NetworkServer.active)
                {
                    buffSystem.ServerApplyBuff(buffData, attackPlayerId, attackskillid);
                }
                else
                {
                    buffSystem.CmdApplyBuff(buffData, attackPlayerId, attackskillid);
                }
            }
        }

        // ✅ SyncVar Hook을 사용하여 UI 자동 업데이트
        private void OnHpChanged(int oldHp, int newHp)
        {
            playerHUD.SetHpBar((float)newHp / maxHp);
            if (newHp == 0)
            {
                playerHUD.GetComponent<CanvasGroup>().alpha = 0;
            }
            else
            {
                playerHUD.GetComponent<CanvasGroup>().alpha = 1;
            }

            if (isOwned)
            {
                playerUI.SetDamageEffect(1f-(float)newHp / maxHp);
                NotifyStatChanged();
            }
        }
        
        [ClientRpc]
        private void ShowFloatingDamageText(int damage)
        {
            if (floatingDamageTextPrefab == null) return;

            GameObject damageTextInstance = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
            damageTextInstance.GetComponent<FloatingDamageText>().SetDamageText(damage);
        }
        
        [ClientRpc]
        private void RpcApplyKnockback(Vector3 force)
        {
            ApplyKnockback(force); // ✅ 넉백 적용 함수 호출
        }

        [ClientRpc]
        private void RpcTransmitKillLog(int killID, int skillid, bool isFall)
        {
            playerProjector?.CloseProjectile();
            if (playerId == killID || killID < 0)
            {
                gameLobbyUI?.UpdateKillLog(playerId, skillid, attackPlayersId, isFall);
            }
            else
            {
                gameLobbyUI?.UpdateKillLog(playerId, skillid, killID, isFall);
            }
        }
    }
}