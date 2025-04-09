using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using TMPro;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public partial class PlayerCharacter : NetworkBehaviour
    {
        [SyncVar(hook = nameof(SetNickname_Hook))]
        public string nickname;
        [SyncVar(hook = nameof(SetUserIdhook))]
        public string userId;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private GameObject gmText;
        [SyncVar(hook = nameof(UpdatePlayerId))] public int playerId = -1;
        private CharacterController _characterController;
        private CinemachineVirtualCamera virtualCamera;
        private Transform cameraTargetGroupTransform;
        
        private BuffSystem buffSystem;
        private EffectSystem effectSystem;
        private PlayerCharacterUI playerUI;
        
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
        
        [Header("Teal Settings")]
        [SyncVar(hook = nameof(OnTeamChanged))] 
        public Constants.TeamType team = Constants.TeamType.None;
        
        public event System.Action OnStatChanged;
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            buffSystem = GetComponent<BuffSystem>();
            effectSystem = GetComponent<EffectSystem>();
            
            InitializeCharacterModels();
        }

        public virtual void Start()
        {
            AttackPower = BaseAttackPower;
            
            MaxSpeed = BaseMaxSpeed;
            MoveSpeed = BaseMaxSpeed;
            
            maxHp = BaseHp;
            curHp = BaseHp;
            
            if (isOwned)
            {
                EnableOnlyThisAudioListener();
                playerLight.SetActive(true);
                
                virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
                
                var targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();
                cameraTargetGroupTransform = targetGroup?.transform;
                
                if (targetGroup != null)
                {
                    virtualCamera.Follow = targetGroup.transform;
                    virtualCamera.LookAt = targetGroup.transform;

                    // 이 플레이어의 마우스-중심 타겟 오브젝트 등록
                    var targets = targetGroup.m_Targets.ToList();
                    targets.Add(new CinemachineTargetGroup.Target
                    {
                        target = CinemachineCameraTarget.transform, // 여전히 움직이는 타겟!
                        weight = 1f,
                        radius = 2f
                    });
                    targetGroup.m_Targets = targets.ToArray();
                }
                
                playerUI = FindFirstObjectByType<PlayerCharacterUI>();
                if (playerUI == null)
                {
                    Debug.Log("playerUI가 없음");
                }
            }
        }
        
        private void EnableOnlyThisAudioListener()
        {
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(sortMode: FindObjectsSortMode.None);
            foreach (var listener in allListeners)
            {
                listener.enabled = false;
            }

            AudioListener myListener = GetComponent<AudioListener>();
            if (myListener != null)
            {
                myListener.enabled = true;
            }
            else
            {
                Debug.LogWarning("[AudioManager] AudioListener component is missing on this GameObject.");
            }
        }

        private void UpdatePlayerId(int oldValue, int newValue)
        {
            playerId = newValue;
                
            var ui = FindFirstObjectByType<GameLobbyUI>();
            ui?.UpdatePlayerInRoon(); // ✅ 죽었을 때 내 UI 갱신
        }

        public void NotifyStatChanged()
        {
            OnStatChanged?.Invoke();
        }

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
            
            var ui = FindFirstObjectByType<GameLobbyUI>();
            ui?.UpdatePlayerInRoon(); // ✅ 팀 바뀌면 내 UI 갱신
        }
        
        public void SetUserIdhook(string _, string value)
        {
            userId = value;
    
            HashSet<string> highlightIds = new HashSet<string> { "1", "2", "3", "4", "5", "6" };
            if (highlightIds.Contains(userId))
            {
                gmText.SetActive(true);
            }
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
            }
        }

        [Command]
        private void CmdSetIsDead(bool value)
        {
            _characterController.enabled = !value;
            isDead = value;
        }

        public void SetIsDead_Hook(bool oldValue, bool newValue)
        {
            isDead = newValue;
            _characterController.enabled = !newValue;
            
            var ui = FindFirstObjectByType<GameLobbyUI>();
            ui?.UpdatePlayerInRoon(); // ✅ 죽었을 때 내 UI 갱신
        }
        
        public void SetState(Constants.PlayerState value)
        {
            if (!isServer)
            {
                if (isOwned)
                {
                    CmdSetState(value);
                }
            }
            else
            {
                State = value;
            }
        }


        [Command]
        public void CmdSetState(Constants.PlayerState value)
        {
            State = value;
        }

        public void SetState_Hook(Constants.PlayerState oldValue, Constants.PlayerState newValue)
        {
            State = newValue;
            gravityVelocity = Vector3.zero;

            if (newValue == Constants.PlayerState.Start && isOwned && PLayerCharacterClass == Constants.CharacterClass.None)
            {
                // 클라이언트에서 자기 PlayerSetting 기반으로 서버에 캐릭터 정보 요청
                CmdApplyPlayerSettings(
                    PlayerSetting.PlayerCharacterClass,
                    PlayerSetting.MoveSkill,
                    PlayerSetting.AttackSkillIDs,
                    PlayerSetting.TeamType,
                    PlayerSetting.PlayerCards.Select(c => c.ID).ToArray()
                );
            }
        }

        [Command]
        public void CmdApplyPlayerSettings(Constants.CharacterClass characterClass, Constants.SkillType moveSkill, int[] attackSkills, Constants.TeamType team, int[] cardIds)
        {
            // 이 메서드 안에서 SetCharacterData, ApplyCardBonuses 등 적용
            SetCharacterData(characterClass, moveSkill, attackSkills);
            this.team = team;

            var cardList = cardIds.Select(id => Database.GetPlayerCardData(id)).ToList();
            ApplyCardBonuses(cardList);

            Debug.Log($"[CmdApplyPlayerSettings] 적용 완료: {characterClass}, {moveSkill}, {string.Join(",", attackSkills)}");
        }
        
        private void SpawnGhost()
        {
            if (!isServer) return; // 서버에서만 실행

            Vector3 spawnPos = transform.position;
            spawnPos.y = Mathf.Max(spawnPos.y + 1f, 3f);
            GameObject ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            NetworkServer.Spawn(ghost, connectionToClient); // 클라이언트와 동기화
            ghostInstance = ghost;
        }
        
        [Command]
        public void CmdResurrect()
        {
            StartCoroutine(DelayedResurrect(connectionToClient));
        }

        private IEnumerator DelayedResurrect(NetworkConnectionToClient conn)
        {
            // 1. 먼저 위치 이동
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
            RpcMoveToSpawn(conn, spawnPos);
            transform.position = spawnPos;

            // 2. 0.5초 대기
            yield return new WaitForSeconds(0.5f);

            // 3. 체력 회복
            curHp = maxHp;

            // 4. 상태 복구
            isDead = false;
            _characterController.enabled = true;

            // 5. 고스트 제거
            if (ghostInstance != null)
            {
                NetworkServer.Destroy(ghostInstance);
                ghostInstance = null;
            }

            // 6. 위치도 서버 측에서 초기화 (동기화용)
            transform.position = spawnPos;

            // 7. 부활 애니메이션 & 카메라 & UI
            RpcTriggerAnimation("isLive");
            RpcResetCamera(conn);

            ResetStatsToBase();
        }
        
        public void ResetStatsToBase()
        {
            AttackPower = BaseAttackPower;
            
            MaxSpeed = BaseMaxSpeed;
            MoveSpeed = BaseMaxSpeed;
            
            maxHp = BaseHp;
            curHp = BaseHp;
            
            attackPlayersId = -1;
            attackskillid = -1;

            // 필요시: availableAttacks 클론 제거 or 리셋
            // itemSkillId = -1;
            // availableAttacks[4] = null;

            NotifyStatChanged();
        }
        
        [TargetRpc]
        private void RpcMoveToSpawn(NetworkConnection target, Vector3 spawnPos)
        {
            transform.position = spawnPos;
        }
        
        [TargetRpc]
        private void RpcResetCamera(NetworkConnection target)
        {
            var cam = FindFirstObjectByType<Cinemachine.CinemachineVirtualCamera>();
            if (cam != null)
            {
                cam.Follow = CinemachineCameraTarget.transform;
            }
        }
        
        [Command]
        public void CmdStartGame()
        {
            var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Init(allPlayers);
            }
            
            var manager = Networking.RoomManager.singleton as Networking.RoomManager;
            manager.StartGame();
        }

        private void OnTeamChanged(Constants.TeamType oldTeam, Constants.TeamType newTeam)
        {
            team = newTeam;

            if (newTeam == Constants.TeamType.TeamA)
            {
                nicknameText.color = new Color(1,0.3f,0.3f);
            }
            
            if (newTeam == Constants.TeamType.TeamB)
            {
                nicknameText.color = new Color(0.3f,0.3f,1);
            }
            
            var ui = FindFirstObjectByType<GameLobbyUI>();
            ui?.UpdatePlayerInRoon(); // ✅ 팀 바뀌면 내 UI 갱신
        }
        
        [Command]
        public void CmdSetTeam(Constants.TeamType newTeam)
        {
            team = newTeam;
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();

            GameRoomData data = FindFirstObjectByType<GameRoomData>();
            data?.Invoke(nameof(GameRoomData.UpdatePlayerList), 0.1f); // 약간 지연해서 호출
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            // 💡 나중에 들어온 클라이언트에서 자신 포함 모든 플레이어 확인
            StartCoroutine(DelayedUpdatePlayerInRoom());
        }
        private IEnumerator DelayedUpdatePlayerInRoom()
        {
            yield return new WaitForSeconds(0.1f); // 동기화 기다림

            var ui = FindFirstObjectByType<GameLobbyUI>();
            ui?.UpdatePlayerInRoon();
        }
        
        private void OnDestroy()
        {
            if (isServer)
            {
                var room = FindFirstObjectByType<GameRoomData>();
                room?.Invoke(nameof(GameRoomData.UpdatePlayerList), 0.1f); // 약간 딜레이 줘도 좋음
            }
        }

    }
}