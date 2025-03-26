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
        [SyncVar] public float MaxSpeed = 5f;
        [SyncVar] public float MoveSpeed = 5.0f;
        public float KnockbackDamping = 5f;
        [SyncVar] public float KnockbackFactor = 1f;
        private float animationSpeed;
        
        private Vector3 _moveDirection = Vector3.zero;
        private Vector3 _knockbackDirection = Vector3.zero;
        private Vector3 _targetPosition; // 목표 위치
        private bool canMove = true;
        private bool isMovingToTarget = false;

        private Vector3 moveKeyboard;
        
        private Vector3 gravityVelocity = Vector3.zero; // ✅ 중력 가속도를 저장할 변수
        private float gravity = -9.81f; // ✅ Unity 기본 중력 값
        
        public GameObject moveIndicatorPrefab; // ✅ 이동 위치를 표시할 이펙트 프리팹

        public void Move()
        {
            _moveDirection = Vector3.zero;
            Vector3 finalMove = Vector3.zero;

            ApplyKnockbackMovement(); // ✅ 넉백 감속 유지
            finalMove += _knockbackDirection;

            HandleMouseMovement();
            // ✅ 캐릭터 이동만 차단, 넉백은 계속 적용됨
            if (attackLockTime <= 0 && canMove && MoveSpeed > 0)
            {
                HandleKeyboardMovement();
                finalMove += _moveDirection;
            }

            ApplyGravity();
            
            finalMove += gravityVelocity;


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

            moveKeyboard = transform.right * moveX + transform.forward * moveZ;
            moveKeyboard = moveKeyboard.normalized;
            bool isMoving = moveKeyboard.magnitude > 0.1f;

            if (isMoving)
            {
                isMovingToTarget = false; // 키보드 이동 시 마우스 이동 중단
                _moveDirection = moveKeyboard * MoveSpeed;
            }
            else if (!isMovingToTarget)
            {
                _moveDirection = Vector3.zero;
            }
        }
        
        private void ApplyGravity()
        {
            if (_characterController.isGrounded)
            {
                gravityVelocity.y = -2f; // ✅ 땅에 있을 때 약간의 중력 유지 (떨림 방지)
            }
            else
            {
                gravityVelocity.y += gravity * Time.deltaTime; // ✅ 중력 가속도 적용
            }
        }
        
        // ✅ 넉백 감속을 별도로 실행
        private void ApplyKnockbackMovement()
        {
            if (_knockbackDirection.magnitude > 0.1f)
            {
                _knockbackDirection = Vector3.Lerp(_knockbackDirection, Vector3.zero, KnockbackDamping * Time.deltaTime);
            }
            else
            {
                _knockbackDirection = Vector3.zero;
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
                    
                    // ✅ 이동 위치 이펙트 표시
                    ShowMoveIndicator(_targetPosition);
                }
            }
            if (isMovingToTarget && attackLockTime <= 0 && canMove)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;
            direction.y = 0; // 수직 이동 방지

            if (Vector3.Distance(transform.position, _targetPosition) > 0.5f)
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
            _knockbackDirection.x *= KnockbackFactor;
            _knockbackDirection.z *= KnockbackFactor;
            isMovingToTarget = false; // 넉백 중에는 마우스 이동 중단
        }
        
        private void UpdateAnimator()
        {
            if (animator != null)
            {
                animationSpeed = Mathf.Clamp(_moveDirection.magnitude, 0, 1) * MoveSpeed/5f;
                animator.SetFloat("isMove", animationSpeed); // ✅ 이동 여부에 따라 isMove 설정
                
                bool isFalling = !_characterController.isGrounded;
                animator.SetBool("isFall", isFalling);
            }
        }
        
        // ✅ 이동 위치 이펙트 생성 함수
        private void ShowMoveIndicator(Vector3 position)
        {
            if (moveIndicatorPrefab == null) return; // 프리팹이 없으면 실행 X

            GameObject moveIndicatorInstance = Instantiate(moveIndicatorPrefab, position + Vector3.up * 0.1f, Quaternion.identity);

            // ✅ 1초 후 자동 삭제 (파티클을 사용하면 따로 제거할 필요 없음)
            Destroy(moveIndicatorInstance, 1f);
        }
    }
}
