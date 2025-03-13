using System;
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
        [SyncVar] public int playerNumber = -1;
        private CharacterController _characterController;
        private CinemachineVirtualCamera virtualCamera;
        private BuffSystem buffSystem;
        private EffectSystem effectSystem;
        [SerializeField]  private PlayerCharacterUI playerUI;
        
        [SerializeField] private LayerMask mouseTargetLayer;
        
        [SyncVar(hook = nameof(SetIsDead_Hook))]
        private bool isDead = true;
        
        [SerializeField] private Animator animator;
        
        private float attackLockTime = 0f;

        [SerializeField] private Transform attackTransform;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            buffSystem = GetComponent<BuffSystem>();
            effectSystem = GetComponent<EffectSystem>();
            
            InitializeCharacterModels();
        }

        public virtual void Start()
        {
            
            GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            if (gameLobbyUI != null)
            {
                gameLobbyUI.UpdatePlayerInRoon();
            }
            
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
            
            if (!_characterController.isGrounded)
            {
                gravityVelocity.y += gravity * Time.deltaTime; // 중력 가속도 증가
            }
            else
            {
                gravityVelocity.y = -1f; // ✅ 바닥에 있으면 살짝 눌러줌 (떨림 방지)
            }

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

        public void SetIsDead(bool value)
        {
            if (!isServer)
            {
                CmdSetIsDead(value);
            }
            else
            {
                isDead = value;
            }
        }

        [Command]
        private void CmdSetIsDead(bool value)
        {
            isDead = value;
        }

        public void SetIsDead_Hook(bool oldValue, bool newValue)
        {
            isDead = newValue;
            _characterController.enabled = !newValue;
        }
    }
}