using System;
using Cinemachine;
using DataSystem;
using GameManagement;
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
        [SyncVar(hook = nameof(UpdatePlayerId))] public int playerId = -1;
        private CharacterController _characterController;
        private CinemachineVirtualCamera virtualCamera;
        private BuffSystem buffSystem;
        private EffectSystem effectSystem;
        [SerializeField]  private PlayerCharacterUI playerUI;

        [SerializeField] private LayerMask mouseTargetLayer;

        [SyncVar(hook = nameof(SetIsDead_Hook))]
        public bool isDead  = false;

        [SyncVar(hook = nameof(SetState_Hook))]
        public Constants.PlayerState State = Constants.PlayerState.NotReady;



        [SerializeField] private Animator animator;
        [SerializeField] private GameObject playerLight;

        private float attackLockTime = 0f;

        [SerializeField] private Transform attackTransform;

        private GameLobbyUI gameLobbyUI;

        [Header("Ghost Settings")]
        [SerializeField] private GameObject ghostPrefab; // ✅ 유령 프리팹
        private GameObject ghostInstance;
        private bool isGhost = false;

        public event System.Action OnStatChanged;
        public float CurrentAttackPower => AttackPower;
        public float BasePower => BaseAttackPower;
        public int MaxHp => maxHp;
        public int CurHp => curHp;


        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            buffSystem = GetComponent<BuffSystem>();
            effectSystem = GetComponent<EffectSystem>();

            InitializeCharacterModels();
        }

        public virtual void Start()
        {

            UpdateCount();

            if (isOwned)
            {
                playerLight.SetActive(true);

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

        private void OnDestroy()
        {
            //UpdateCount();
        }

        private void UpdatePlayerId(int oldValue, int newValue)
        {
            if (isServer) return; // 서버에서는 직접 할당되므로 클라이언트에서만 실행

            playerId = newValue;
            UpdateCount();
        }

        private void UpdateCount()
        {
            if (gameLobbyUI == null)
            {
                gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            }
            gameLobbyUI.UpdatePlayerInRoon();
        }

        // 스탯 UI 이벤트
        public void NotifyStatChanged()
        {
            OnStatChanged?.Invoke();
        }
        //
        void Update()
        {
            if (!isOwned) return;

            if (isDead) return;

            if (State == Constants.PlayerState.NotReady || State == Constants.PlayerState.Ready) return;

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

            if (State != Constants.PlayerState.Start) return;

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
            if (animParameter != "")
            {
                RpcTriggerAnimation(animParameter);
            }
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
                if (value) SpawnGhost();
                RpcUpdatePlayerStatus(connectionToClient); // ✅ 서버에서 실행 시 TargetRpc 호출
            }
        }

        [Command]
        private void CmdSetIsDead(bool value)
        {
            isDead = value;

            // ✅ 서버에서 클라이언트에게 UI 업데이트 전송
            RpcUpdatePlayerStatus(connectionToClient);
        }

        public void SetIsDead_Hook(bool oldValue, bool newValue)
        {
            isDead = newValue;
            _characterController.enabled = !newValue;

            //Debug.Log($"[SetIsDead_Hook] {PlayerSetting.PlayerId} 플레이어 {playerId} isDead 값 변경됨: {newValue}");

            // ✅ UI 강제 업데이트
            UpdateCount();
        }

        [TargetRpc]
        private void RpcUpdatePlayerStatus(NetworkConnection target)
        {
            if (gameLobbyUI == null)
            {
                gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            }

            gameLobbyUI.UpdatePlayerInRoon();
        }

        public void SetState(Constants.PlayerState value)
        {
            if (!isServer)
            {
                CmdSetState(value);
            }
            else
            {
                State = value;
            }
        }

        [Command]
        private void CmdSetState(Constants.PlayerState value)
        {
            State = value;
        }

        public void SetState_Hook(Constants.PlayerState oldValue, Constants.PlayerState newValue)
        {
            State = newValue;
        }

        private void SpawnGhost()
        {
            if (!isServer) return; // 서버에서만 실행

            GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            NetworkServer.Spawn(ghost, connectionToClient); // 클라이언트와 동기화
            ghostInstance = ghost;

            RpcSetupGhost(ghost);
        }

        [ClientRpc]
        private void RpcSetupGhost(GameObject ghost)
        {
            if (ghost == null) return;

            isGhost = true;
        }


    }
}