using Cinemachine;
using DataSystem;
using Mirror;
using Player.Combat;
using UI;
using UnityEngine;

namespace Player
{
    public class GhostController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float verticalSpeed = 3f;
        [SerializeField] private float gravity = 1f;
        private float groundCheckDistance = 0.5f; // Ray 거리
        [SerializeField] private LayerMask groundLayer; // 땅 레이어

        private CharacterController _characterController;
        private Vector3 _moveDirection;
        private Vector3 _targetPosition;
        private bool isMovingToTarget = false;

        [Header("Model & Animation")]
        [SerializeField] private Transform ghostModel;
        [SerializeField] private Animator animator;
        private CinemachineVirtualCamera virtualCamera;
        
        [SerializeField] private Transform CameraRoot;

        [Header("Materials")] 
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Material ownedMaterial;
        
        [Header("Attack Settings")]
        [SerializeField] private float attackRadius = 3f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackKnockback = 1f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float attackDelay = 0.3f;          // 공격 애니메이션 후 실제 데미지 타이밍
        [SerializeField] private float attackRecoveryTime = 0.6f;   // 공격 후 이동 불가 시간
        [SerializeField] private GameObject explosionPrefab; // Explosion 프리팹
        [SerializeField] private ParticleSystem attackEffect;     // 공격 시 이펙트

        private float lastAttackTime;
        private bool isAttacking = false;
        private float attackLockUntil = 0f;
        private PlayerCharacterUI playerCharacterUI;
        
        private void Start()
        {
            _characterController = GetComponent<CharacterController>();

            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_PlayerDead, gameObject);
            
            if (isOwned)
            {
                virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = CameraRoot;
                }

                if (skinnedMeshRenderer != null && ownedMaterial != null)
                {
                    skinnedMeshRenderer.material = ownedMaterial;
                }

                playerCharacterUI = FindFirstObjectByType<PlayerCharacterUI>();
            }
        }

        private void Update()
        {
            if (!isOwned) return;

            ApplyGravity();
            HandleKeyboardMovement();
            HandleMouseMovement();
            MoveGhost();
            HandleRotation();
            HandleAnimation();
            HandleAttackInput();
        }

        private void HandleKeyboardMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveInput = transform.right * moveX + transform.forward * moveZ;
            moveInput = moveInput.normalized;
            
            if (Input.GetKey(KeyCode.Space))
            {
                _moveDirection.y = verticalSpeed;
            }

            if (moveInput.magnitude > 0.1f)
            {
                isMovingToTarget = false;
                _moveDirection.x = moveInput.x * moveSpeed;
                _moveDirection.z = moveInput.z * moveSpeed;
            }
            else if (!isMovingToTarget)
            {
                _moveDirection.x = 0f;
                _moveDirection.z = 0f;
            }
        }

        private void HandleMouseMovement()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    _targetPosition = hit.point;
                    isMovingToTarget = true;
                }
            }
        }

        private void ApplyGravity()
        {
            if (!IsGrounded())
            {
                _moveDirection.y -= gravity*Time.deltaTime;
            }
            else if (_moveDirection.y < 0f)
            {
                _moveDirection.y = 0; // 지면에 닿았을 때 아래로 계속 쌓이지 않게 함
            }
        }

        private bool IsGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        }

        private void MoveGhost()
        {
            if (isMovingToTarget)
            {
                Vector3 direction = (_targetPosition - transform.position).normalized;
                direction.y = 0;

                if (Vector3.Distance(transform.position, _targetPosition) > 0.5f)
                {
                    _moveDirection.x = direction.x * moveSpeed;
                    _moveDirection.z = direction.z * moveSpeed;
                    // 👉 y값은 유지 (중력 or 상승에 의한 값)
                }
                else
                {
                    isMovingToTarget = false;
                    _moveDirection.x = 0f;
                    _moveDirection.z = 0f;
                }
            }

            _characterController.Move(_moveDirection * Time.deltaTime);
        }

        private void HandleRotation()
        {
            if (_moveDirection.magnitude > 0.1f)
            {
                Vector3 direction = new Vector3(_moveDirection.x, 0, _moveDirection.z).normalized;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    ghostModel.rotation = Quaternion.Lerp(ghostModel.rotation, targetRotation, Time.deltaTime * 10f);
                }
            }
        }

        private void HandleAnimation()
        {
            if (animator != null)
            {
                bool isMoving = new Vector3(_moveDirection.x, 0, _moveDirection.z).magnitude > 0.1f;
                animator.SetBool("isMove", isMoving);
            }
        }
        
        private void HandleAttackInput()
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                isAttacking = true;
                lastAttackTime = Time.time;
                attackLockUntil = Time.time + attackRecoveryTime;

                animator.SetTrigger("isAttack");
                playerCharacterUI.UseGhostSkill(attackCooldown);

                // 공격 처리는 약간의 딜레이 후에 서버에서 실행
                Invoke(nameof(PerformAttack), attackDelay);
            }
        }
        
        private void PerformAttack()
        {
            CmdExplodeAttack(transform.position);

            Invoke(nameof(EndAttack), attackRecoveryTime);
        }

        private void EndAttack()
        {
            isAttacking = false;
        }
        
        [Command]
        private void CmdExplodeAttack(Vector3 position)
        {
            GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        
            Explosion explosionComp = explosion.GetComponent<Explosion>();
            if (explosionComp != null)
            {
                explosionComp.Initialize(attackDamage, attackRadius, attackKnockback, null, gameObject, -1, -1); // config 등은 필요 시 전달
            }

            NetworkServer.Spawn(explosion);
            RpcPlayAttackEffect();
        }
        
        [ClientRpc]
        private void RpcPlayAttackEffect()
        {
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_GhostAttack, gameObject);
        }

    }
}
