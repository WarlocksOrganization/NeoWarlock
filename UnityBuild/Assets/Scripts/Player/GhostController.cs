using Cinemachine;
using Mirror;
using UI;
using UnityEngine;

namespace Player
{
    public class GhostController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float verticalSpeed = 3f; // 스페이스바로 상승 속도

        private CharacterController _characterController;
        private Vector3 _moveDirection;
        private Vector3 _targetPosition;
        private bool isMovingToTarget = false;

        [Header("Model & Animation")]
        [SerializeField] private Transform ghostModel; // 유령 모델
        [SerializeField] private Animator animator; // 유령 애니메이터
        private CinemachineVirtualCamera virtualCamera;
        
        [SerializeField] private Transform CameraRoot;

        [Header("Materials")] 
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Material ownedMaterial; // isOwned일 때 사용할 머티리얼

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();

            if (isOwned)
            {
                // 카메라 세팅
                virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = CameraRoot;
                }

                // 머티리얼 변경
                if (skinnedMeshRenderer != null && ownedMaterial != null)
                {
                    skinnedMeshRenderer.material = ownedMaterial;
                }
            }
        }

        private void Update()
        {
            if (!isOwned) return;
            
            HandleKeyboardMovement();
            HandleMouseMovement();
            MoveGhost();
            HandleRotation();
            HandleAnimation();
        }

        private void HandleKeyboardMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            float moveY = 0f;
            if (Input.GetKey(KeyCode.Space))
            {
                moveY = verticalSpeed; // 상승
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                moveY = -verticalSpeed; // 하강
            }

            Vector3 moveInput = transform.right * moveX + transform.forward * moveZ + Vector3.up * moveY;
            moveInput = moveInput.normalized;

            if (moveInput.magnitude > 0.1f)
            {
                isMovingToTarget = false;
                _moveDirection = moveInput * moveSpeed;
            }
            else if (!isMovingToTarget)
            {
                _moveDirection = Vector3.zero;
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

        private void MoveGhost()
        {
            if (isMovingToTarget)
            {
                Vector3 direction = (_targetPosition - transform.position).normalized;
                direction.y = 0;

                if (Vector3.Distance(transform.position, _targetPosition) > 0.5f)
                {
                    _moveDirection = direction * moveSpeed;
                }
                else
                {
                    isMovingToTarget = false;
                    _moveDirection = Vector3.zero;
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
                bool isMoving = _moveDirection.magnitude > 0.1f;
                animator.SetBool("isMove", isMoving);
            }
        }
    }
}
