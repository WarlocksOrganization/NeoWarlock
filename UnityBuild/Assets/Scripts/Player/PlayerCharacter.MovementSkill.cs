using System.Collections;
using DataSystem;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {

        private MovementSkillBase movementSkill;
        private float lastMovementSkillTime = -Mathf.Infinity;

        private void TryUseMovementSkill()
        {
            if (Input.GetKeyDown(KeyCode.Space) && movementSkill != null && CanUseMovementSkill())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mouseTargetLayer))
                {
                    lastMovementSkillTime = Time.time;
                    attackLockTime = movementSkill.EndTime;
                    canMove = false;
                    playerUI?.UseSkill(4);
                    animator.SetTrigger("isMoveSkill");
                    CmdUseMovementSkill(hit.point);
                }
            }
        }

        public void SetMovementSkill(MovementSkillBase movementSkill)
        {
            this.movementSkill = movementSkill;
            playerUI?.SetQuickSlotData(4, movementSkill.SkillIcon, movementSkill.Cooldown);
            CMDSetMovementSkill(movementSkill.SkillType);
        }

        [Command]
        private void CMDSetMovementSkill(Constants.SkillType skillType)
        {
            movementSkill = MovementSkillFactory.GetMovementSkill(skillType);
            playerUI?.SetQuickSlotData(4, movementSkill.SkillIcon, movementSkill.Cooldown);
        }
        
        [Command]
        private void CmdUseMovementSkill(Vector3 targetPosition)
        {
            if (movementSkill == null) return;

            playerModel.transform.rotation = Quaternion.LookRotation((targetPosition - transform.position).normalized);
            // ✅ 1. 스킬 이펙트 먼저 출력
            RpcPlaySkillEffect(movementSkill.SkillType);

            // ✅ 2. 시전 시간 후 이동 실행
            StartCoroutine(CastAndMove(targetPosition, movementSkill.CastTime));
        }

        private IEnumerator CastAndMove(Vector3 targetPosition, float castTime)
        {
            yield return new WaitForSeconds(castTime); // ✅ 시전 시간 대기

            // ✅ 3. 이동 실행
            Vector3 newPosition = movementSkill.GetTargetPosition(this, targetPosition);
            RpcUseMovementSkill(newPosition, movementSkill.MoveDuration, movementSkill.EndTime);
        }

        [ClientRpc]
        private void RpcUseMovementSkill(Vector3 targetPosition, float moveDuration, float endLag)
        {
            StartCoroutine(MovementSkillRoutine(targetPosition, moveDuration, endLag));
        }

        private IEnumerator MovementSkillRoutine(Vector3 targetPosition, float moveDuration, float endLag)
        {
            canMove = false;
            _characterController.enabled = false; // ✅ 이동 중 CharacterController 비활성화

            float elapsed = 0f;
            Vector3 startPosition = transform.position;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
                yield return null;
            }

            transform.position = targetPosition; // 최종 위치 보정

            yield return new WaitForSeconds(endLag); // ✅ 이동 후 대기 시간 적용

            _characterController.enabled = true; // ✅ 이동 후 CharacterController 다시 활성화
            canMove = true;
        }

        private bool CanUseMovementSkill()
        {
            return Time.time >= lastMovementSkillTime + movementSkill.Cooldown;
        }
    }
}
