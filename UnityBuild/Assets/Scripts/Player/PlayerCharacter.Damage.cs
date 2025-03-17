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
        private int curHp = 300;

        [SyncVar] private int maxHp = 300;
        
        public void takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig)
        {
            if (curHp <= 0) return;

            DecreaseHp(damage);

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

                if (attackConfig.appliedBuff != null)
                {
                    ApplyBuffFromAttack(attackConfig.appliedBuff);
                }
            }
        }

        public void DecreaseHp(int damage)
        {
            if(curHp <= 0) return;
            curHp -= damage;
            curHp = Mathf.Clamp(curHp, 0, maxHp);
            if (curHp == 0)
            {
                SetIsDead(true);
                RpcTriggerAnimation("isDead"); // 클라이언트에도 애니메이션 트리거 전송
            }
        }
        
        private void ApplyBuffFromAttack(BuffData buffData)
        {
            if (buffSystem != null)
            {
                if (NetworkServer.active)
                {
                    buffSystem.ServerApplyBuff(buffData);
                }
                else
                {
                    buffSystem.CmdApplyBuff(buffData);
                }
            }
        }

        // ✅ SyncVar Hook을 사용하여 UI 자동 업데이트
        private void OnHpChanged(int oldHp, int newHp)
        {
            ShowFloatingDamageText(oldHp-newHp);
            playerHUD.SetHpBar((float)newHp / maxHp);
            if (newHp == 0)
            {
                playerHUD.GetComponent<CanvasGroup>().alpha = 0;
            }
        }
        
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
    }
}