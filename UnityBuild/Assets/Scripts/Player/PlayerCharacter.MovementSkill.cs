using System.Collections;
using DataSystem;
using DataSystem.Database;
using Interfaces;
using kcp2k;
using Mirror;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        private MovementSkillConfig movementSkill;
        private float lastMovementSkillTime = -Mathf.Infinity;

        private void TryUseMovementSkill()
        {
            if (Input.GetKeyDown(KeyCode.Space) && movementSkill != null && CanUseMovementSkill())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mouseTargetLayer))
                {
                    lastMovementSkillTime = Time.time;
                    attackLockTime = movementSkill.endTime;
                    canMove = false;
                    playerUI?.UseSkill(0, movementSkill.cooldown);
                    CmdTriggerAnimation("isMoveSkill");

                    Vector3 targetPos = Vector3.zero;
                    if (moveKeyboard.magnitude > 0.1f)
                    {
                        targetPos = transform.position + moveKeyboard * movementSkill.maxDistance;
                    }
                    else
                    {
                        Vector3 direction = (hit.point - transform.position).normalized;
                        float distanceToTarget = Vector3.Distance(transform.position, hit.point);
                        float moveDistance = Mathf.Min(distanceToTarget, movementSkill.maxDistance);
                        targetPos = transform.position + direction * moveDistance;
                    }
                    
                    CmdUseMovementSkill(targetPos);
                }
            }
        }


        public void SetMovementSkill(Constants.SkillType skillType)
        {
            movementSkill = Database.GetMovementSkillData(skillType);
            playerUI?.SetQuickSlotData(0, movementSkill.skillIcon, movementSkill.cooldown);
        }

        [Command]
        private void CmdUseMovementSkill(Vector3 targetPosition)
        {
            if (movementSkill == null) return;

            playerModel.transform.rotation = Quaternion.LookRotation((targetPosition - transform.position).normalized);
            RpcPlaySkillEffect(movementSkill.skillType);
            StartCoroutine(CastAndMove(targetPosition, movementSkill.castTime));
        }

        private IEnumerator CastAndMove(Vector3 targetPosition, float castTime)
        {
            yield return new WaitForSeconds(castTime);
            RpcUseMovementSkill(targetPosition, movementSkill.moveDuration, movementSkill.endTime);
        }

        [ClientRpc]
        private void RpcUseMovementSkill(Vector3 targetPosition, float moveDuration, float endLag)
        {
            StartCoroutine(MovementSkillRoutine(targetPosition, moveDuration, endLag));
        }

        private IEnumerator MovementSkillRoutine(Vector3 targetPosition, float moveDuration, float endLag)
        {
            canMove = false;
            _characterController.enabled = false;

            float elapsed = 0f;
            Vector3 startPosition = transform.position;
            
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
                yield return null;
            }

            transform.position = targetPosition;

            _characterController.enabled = true;
            canMove = true;
        }

        private bool CanUseMovementSkill()
        {
            return Time.time >= lastMovementSkillTime + movementSkill.cooldown;
        }
    }
}
