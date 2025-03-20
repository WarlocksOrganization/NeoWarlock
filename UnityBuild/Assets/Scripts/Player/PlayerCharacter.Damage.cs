using DataSystem;
using Interfaces;
using Mirror;
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

        [SyncVar] private int maxHp = 150;

        [SyncVar] private int attackPlayersId = -1;
        [SyncVar] private int attackskillid = -1;
        [SyncVar] public int defense = 0; // ✅ 방어력 추가
        
        public void takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int attackPlayerId, int attackskillid)
        {
            if (!isServer || State == Constants.PlayerState.NotReady) return;
            if (curHp <= 0) return;

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
                    RpcApplyKnockback(direction * knockbackForce);
                    RpcTriggerAnimation("isHit"); 
                }

                if (attackConfig != null && attackConfig.appliedBuff != null)
                {
                    ApplyBuffFromAttack(attackConfig.appliedBuff, attackPlayerId, attackskillid);
                }
            }
        }

        public void DecreaseHp(int damage, int attackPlayerId, int attackskillid)
        {
            if (curHp <= 0) return;
            int predamage = damage;
            
            if (predamage > 0 && attackPlayerId >= 0 && playerId != attackPlayerId)
            {
                this.attackPlayersId = attackPlayerId;
                this.attackskillid = attackskillid;
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
                if (attackskillid == 0)
                {
                    RpcTransmitKillLog(attackPlayerId, this.attackskillid, true);
                }
                else
                {
                    RpcTransmitKillLog(attackPlayerId, attackskillid, false);
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
        }
        
        [ClientRpc]
        private void ShowFloatingDamageText(int damage)
        {
            if (floatingDamageTextPrefab == null) return;

            GameObject damageTextInstance = Instantiate(floatingDamageTextPrefab, transform.position, Quaternion.identity);
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