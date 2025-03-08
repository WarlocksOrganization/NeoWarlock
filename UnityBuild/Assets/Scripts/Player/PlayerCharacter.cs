using Cinemachine;
using Mirror;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public partial class PlayerCharacter : NetworkBehaviour
    {
        [SyncVar(hook = nameof(SetNickname_Hook))]
        public string nickname;
        [SerializeField] private TMP_Text nicknameText;
        private CharacterController _characterController;
        private CinemachineVirtualCamera virtualCamera;
        private BuffSystem buffSystem;
        private EffectSystem effectSystem;
        [SerializeField]  private PlayerCharacterUI playerUI;
        
        [SerializeField] private LayerMask mouseTargetLayer;
        
        [SyncVar] private bool isDead = false;
        
        [SerializeField] private Animator animator;
        
        private float attackLockTime = 0f;

        [SerializeField] private Transform attackTransform;

        public virtual void Start()
        {
            _characterController = GetComponent<CharacterController>();
            buffSystem = GetComponent<BuffSystem>();
            effectSystem = GetComponent<EffectSystem>();
            
            InitializeCharacterModels();
            ApplyCharacterClass(characterClass);

            if (isOwned)
            {
                virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = CinemachineCameraTarget.transform;
                }

                playerUI = FindFirstObjectByType<PlayerCharacterUI>();
                if (playerUI == null)
                {
                    Debug.Log("playerUI가 없음");
                }
            }
        }

        void Update()
        {
            if (!isOwned) return;

            if (isDead) return;

            Move();
            UpdateCameraTarget();
            
            if (0 < attackLockTime)
            {
                attackLockTime-=Time.deltaTime;
                return;
            }
            
            UpdateAttack();
        }

        public void SetNickname_Hook(string _, string value)
        {
            nicknameText.text = value;
            nickname = value;
        }
        
        [Command]
        private void CmdTriggerAnimation(string animParameter)
        {
            RpcTriggerAnimation(animParameter);
        }
        
        [ClientRpc]
        private void RpcTriggerAnimation(string trigger)
        {
            animator.SetTrigger(trigger);
        }
    }
}