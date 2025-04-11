using System.Collections;
using System.Linq;
using Cinemachine;
using DataSystem;
using GameManagement;
using Interfaces;
using Mirror;
using Player;
using UnityEngine;
using UnityEngine.UI;

public partial class DragonAI : NetworkBehaviour, IDamagable
{
    [Header("Stats")]
    [SyncVar(hook = nameof(OnHpChanged))] public int curHp;
    [SyncVar] public int maxHp = 50;
    public float moveSpeed = 2f;
    public float attackRange = 2f;
    public int damage = 10;
    public int knockbackDamage = 1;
    public float attackCooldown = 2f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private Transform EnemyModel;

    [Header("Health UI")] 
    [SerializeField] private DragonHPBar hpBarUI;
    
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("ETC")]
    [SerializeField] private GameObject floatingDamageTextPrefab;

    private bool isLanded = false;

    private void Awake()
    {
        if (isServer)
        {
            SetCollider(false); // 🛡️ 무적 설정은 서버에서만
        }
        
        UpdateHealthUI();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHealthUI();
    }

    public void Init()
    {
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null && isServer)
        {
            gameRoomData.SetRoomType(Constants.RoomType.Raid);

            // ✅ 팀 설정을 Coroutine으로 분리
            StartCoroutine(DelaySetTeam());
        }

        int baseHp = 0;
        int bonusPerPlayer = 500;
        int playerCount = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None).Length;

        int newMaxHp = baseHp + bonusPerPlayer * playerCount;

        if (maxHp < newMaxHp)
        {
            maxHp = newMaxHp;
            curHp = maxHp;
        }

        EnemyModel.rotation = Quaternion.Euler(0, 180, 0);
        transform.position = new Vector3(0, 2.78f, 0);
        RpcAddToTargetGroup(transform);
        StartCoroutine(StartLandingSequence());
    }
    
    private IEnumerator DelaySetTeam()
    {
        yield return new WaitForSeconds(1f);

        var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.team = Constants.TeamType.TeamA;
        }
    }


    public IEnumerator StartLandingSequence()
    {
        SelectRandomTarget();
        
        animator.SetTrigger("isLanding");
        RpcPlayAnimation("isLanding");

        yield return new WaitForSeconds(5f); // 착지 애니메이션 시간
        SetCollider(true);
        RpcPlaySound(Constants.SoundType.SFX_HandEndAttack);
        yield return new WaitForSeconds(1f);
        RpcPlaySound(Constants.SoundType.SFX_DragonRoar);
        yield return new WaitForSeconds(2f); // 착지 애니메이션 시간
        isFlying = false;

        isLanded = true;
        
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (!isServer || !isLanded || curHp <= 0 || isFlying) return;

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        if (isAttacking) return;

        if (target == null || target.isDead)
        {
            SelectRandomTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        RotateTowardsTarget();

        if (attackCooldownTimer <= 0f)
        {
            var validAttacks = attackPatterns
                .Where(a => dist <= a.range)
                .ToList();

            if (validAttacks.Count > 0)
            {
                selectedAttack = validAttacks[Random.Range(0, validAttacks.Count)];
                StartCoroutine(PerformAttack());
            }
            else
            {
                MoveTowardsTarget();
            }
        }
        else
        {
            if (dist > 10)
            {
                MoveTowardsTarget();
            }
            else
            {
                var validAttacks = attackPatterns
                    .Where(a => dist <= a.range)
                    .ToList();

                if (validAttacks.Count > 0)
                {
                    RotateTowardsTarget();
                    
                    selectedAttack = validAttacks[Random.Range(0, validAttacks.Count)];
                    StartCoroutine(PerformAttack());
                }
            }
        }
    }

    [Server]
    public int takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid)
    {
        if (curHp <= 0 || damage <= 0) return 0;

        curHp -= damage;
        Debug.Log("용체력 : " + curHp);

        RpcShowFloatingDamage(damage);
        
        GameManager.Instance.RecordDamage(playerid, damage);

        if (curHp <= 0)
        {
            curHp = 0;
            Die();
        }

        return this.damage;
    }

    [Server]
    private void Die()
    {
        animator.SetTrigger("isDead");
        RpcPlayAnimation("isDead");

        
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null && isServer)
        {
            gameRoomData.SetRoomType(Constants.RoomType.Solo);

            // ✅ 모든 플레이어 팀을 TeamA로 설정
            var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                player.team = Constants.TeamType.None;
            }
        }
        RpcHideHealthUI(); // 체력바 숨기기 호출
        
        StartCoroutine(DelayGameOverCheckAfterDeath());
    }
    
    private IEnumerator DelayGameOverCheckAfterDeath()
    {
        yield return new WaitForSeconds(3f);
        GameManager.Instance.TryCheckGameOver();
    }
    
    // 체력바 숨기기 Rpc 추가
    [ClientRpc]
    private void RpcHideHealthUI()
    {
        var hpBar = FindFirstObjectByType<DragonHPBar>();
        if (hpBar != null)
            hpBar.HideHpBar();
    }

    private void OnHpChanged(int oldHp, int newHp)
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        Debug.Log(curHp + " / " + maxHp);
        if (hpBarUI == null)
        {
            hpBarUI = FindFirstObjectByType<DragonHPBar>();
        }
        
        if (hpBarUI != null && curHp > 0)
        {
            hpBarUI.UpdateHpBar(curHp, maxHp);
        }
        else
        {
            Debug.LogWarning("DragonHPBar 오브젝트를 찾을 수 없습니다.");
        }
    }

    [ClientRpc]
    private void RpcShowFloatingDamage(int damage)
    {
        if (floatingDamageTextPrefab == null) return;

        GameObject instance = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up * 3f, Quaternion.identity);
        instance.GetComponent<FloatingDamageText>().SetDamageText(damage);
    }
    
    [ClientRpc]
    private void RpcPlayAnimation(string anim)
    {
        animator.SetTrigger(anim);
    }
    
    [Server]
    public void SetCollider(bool isInvincible)
    {
        Debug.Log($"[Server] SetCollider({isInvincible})");
    
        bool colliderShouldBeEnabled = isInvincible;
    
        capsuleCollider.enabled = colliderShouldBeEnabled; 
        RpcSetColliderState(colliderShouldBeEnabled);
    }

    [ClientRpc]
    private void RpcSetColliderState(bool enabled)
    {
        Debug.Log($"[Client] RpcSetColliderState({enabled})");
    
        if (capsuleCollider != null)
            capsuleCollider.enabled = enabled;
    }
    
    [ClientRpc]
    public void RpcRemoveFromTargetGroup()
    {
        var group = GameObject.FindFirstObjectByType<CinemachineTargetGroup>();
        if (group == null)
        {
            Debug.LogWarning("CinemachineTargetGroup을 찾을 수 없습니다.");
            return;
        }

        // 기존 타겟 리스트 가져오기
        var targets = group.m_Targets.ToList();

        // 이 오브젝트의 트랜스폼을 가진 항목 제거
        targets.RemoveAll(t => t.target == transform);

        // 다시 설정
        group.m_Targets = targets.ToArray();
    }
    
    [ClientRpc]
    private void RpcPlaySound(Constants.SoundType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType, gameObject);
    }
    
    [ClientRpc]
    private void RpcPlaySound(Constants.SkillType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType, gameObject);
    }
}
