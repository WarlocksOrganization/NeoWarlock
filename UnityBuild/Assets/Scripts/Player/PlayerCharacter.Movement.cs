using System.Collections;
using DataSystem;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter : IMovable
    {
        [Header("Player Movement")]
        public float MaxSpeed = 5f;
        [SyncVar(hook = nameof(OnMoveSpeedChanged))]
        public float MoveSpeed = 5.0f;
        public float KnockbackDamping = 5f;
        private float animationSpeed;
        
        private Vector3 _moveDirection = Vector3.zero;
        private Vector3 _knockbackDirection = Vector3.zero;
        private Vector3 _targetPosition; // 목표 위치
        private bool canMove = true;
        private bool isMovingToTarget = false;

        public void Move()
        {
            _moveDirection = Vector3.zero;
            Vector3 finalMove = Vector3.zero;
            
            ApplyKnockbackMovement(); // ✅ 넉백 감속은 항상 적용
            finalMove += _knockbackDirection;
            
            // ✅ 캐릭터 이동만 차단, 넉백은 계속 적용됨
            if (attackLockTime <= 0 && canMove)
            {
                HandleKeyboardMovement();
                HandleMouseMovement();
                finalMove += _moveDirection;
            }

            if (canMove)
            {
                _characterController.Move(finalMove * Time.deltaTime);
                TryUseMovementSkill();
            }

            RotateModelToMoveDirection();
            UpdateAnimator();
            
        }

        private void HandleKeyboardMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            bool isMoving = move.magnitude > 0.1f;

            if (isMoving)
            {
                isMovingToTarget = false; // 키보드 이동 시 마우스 이동 중단
                _moveDirection = move * MoveSpeed;
            }
            else if (!isMovingToTarget)
            {
                _moveDirection = Vector3.zero;
            }
        }
        
        // ✅ 넉백 감속을 별도로 실행
        private void ApplyKnockbackMovement()
        {
            if (_knockbackDirection.magnitude > 0.1f)
            {
                _knockbackDirection = Vector3.Lerp(_knockbackDirection, Vector3.zero, KnockbackDamping * Time.deltaTime);
            }
        }
        
        private void HandleMouseMovement()
        {
            if (Input.GetMouseButtonDown(1)) // 마우스 우클릭으로 목표 위치 설정
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mouseTargetLayer))
                {
                    _targetPosition = hit.point;
                    isMovingToTarget = true;
                }
            }

            if (isMovingToTarget)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;
            direction.y = 0; // 수직 이동 방지

            if (Vector3.Distance(transform.position, _targetPosition) > 0.1f)
            {
                _moveDirection = direction * MoveSpeed;
            }
            else
            {
                isMovingToTarget = false; // 목표 지점 도착 시 정지
                _moveDirection = Vector3.zero;
            }
        }

        private void RotateModelToMoveDirection()
        {
            if (_moveDirection.magnitude > 0.1f)
            {
                Vector3 direction = new Vector3(_moveDirection.x, 0, _moveDirection.z).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                playerModel.transform.rotation = Quaternion.Lerp(playerModel.transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        public void ApplyKnockback(Vector3 force)
        {
            _knockbackDirection = force;
            isMovingToTarget = false; // 넉백 중에는 마우스 이동 중단
        }
        
        private void UpdateAnimator()
        {
            if (animator != null)
            {
                animationSpeed = Mathf.Clamp(_moveDirection.magnitude, 0, 1) * MoveSpeed/5f;
                animator.SetFloat("isMove", animationSpeed); // ✅ 이동 여부에 따라 isMove 설정
            }
        }

        private void OnMoveSpeedChanged(float oldValue, float newValue)
        {
            MoveSpeed = newValue;
        }
    }
}
