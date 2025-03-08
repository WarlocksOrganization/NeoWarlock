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

        [SyncVar(hook = nameof(OnHpChanged))] // ✅ Hook 추가
        private int curHp = 100;

        [SyncVar] private int maxHp = 100;

        public void takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig)
        {
            if (curHp <= 0) return;

            curHp -= damage;
            curHp = Mathf.Clamp(curHp, 0, maxHp);

            if (curHp == 0)
            {
                isDead = true;
                RpcTriggerAnimation("isDead"); // 클라이언트에도 애니메이션 트리거 전송
            }
            else
            {
                Vector3 direction = transform.position - attackTran;
                direction.y = 0;

                if (knockbackForce > 0)
                {
                    RpcApplyKnockback(direction * knockbackForce);
                    RpcTriggerAnimation("isHit"); // 클라이언트에서도 "isHit" 트리거 실행
                }

                if (attackConfig.appliedBuff != null)
                {
                    ApplyBuffFromAttack(attackConfig.appliedBuff);
                }
            }
        }
        
        private void ApplyBuffFromAttack(BuffData buffData)
        {
            if (buffSystem != null)
            {
                buffSystem.CmdApplyBuff(buffData);
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
        private void RpcApplyKnockback(Vector3 force)
        {
            ApplyKnockback(force); // ✅ 넉백 적용 함수 호출
        }
    }
}