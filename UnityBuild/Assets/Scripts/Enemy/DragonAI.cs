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
    public static DragonAI Instance;

    [Header("Stats")]
    [SyncVar(hook = nameof(OnHpChanged))] private int curHp;
    public int maxHp = 50;
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
    [SerializeField] private Slider healthSlider;
    
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("ETC")]
    [SerializeField] private GameObject floatingDamageTextPrefab;

    private bool isLanded = false;

    private void Awake()
    {
        if (isServer)
        {
            SetInvincible(false); // 🛡️ 무적 설정은 서버에서만
        }
        
        Instance = this;
        UpdateHealthUI();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHealthUI();
    }

    public void Init()
    {
        if (GameManager.Instance.dragonState.curHp <= 0)
        {
            return;
        }
        
        int baseHp = 500;
        int bonusPerPlayer = 500;
        int playerCount = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None).Length;

        int newMaxHp = baseHp + bonusPerPlayer * playerCount;

        if (GameManager.Instance.dragonState.maxHp < newMaxHp)
        {
            GameManager.Instance.dragonState.curHp += (newMaxHp - GameManager.Instance.dragonState.maxHp);
            GameManager.Instance.dragonState.maxHp = newMaxHp;
        }

        maxHp = GameManager.Instance.dragonState.maxHp;
        curHp = Mathf.Min(GameManager.Instance.dragonState.curHp, maxHp);
        
        EnemyModel.rotation = Quaternion.Euler(0, 180, 0);
        transform.position = new Vector3(0, 2.78f, 0);
        RpcAddToTargetGroup(transform);
        StartCoroutine(StartLandingSequence());
    }

    public IEnumerator StartLandingSequence()
    {
        SelectRandomTarget();

        if (target != null)
        {
            animator.SetTrigger("isLanding");
            RpcPlayAnimation("isLanding");

            yield return new WaitForSeconds(5f); // 착지 애니메이션 시간
            SetInvincible(true);
            yield return new WaitForSeconds(3f); // 착지 애니메이션 시간
            isFlying = false;

            isLanded = true;
        }
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
                    selectedAttack = validAttacks[Random.Range(0, validAttacks.Count)];
                    StartCoroutine(PerformAttack());
                }
            }
        }
    }

    [Server]
    public int takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid)
    {
        if (curHp <= 0) return 0;

        curHp -= damage;
        Debug.Log("용체력 : " + curHp);

        RpcShowFloatingDamage(damage);
        
        GameManager.Instance.dragonState.curHp = curHp;

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
        RpcPlayAnimation(selectedAttack.animTrigger);
    }

    private void OnHpChanged(int oldHp, int newHp)
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null)
        {
            GameObject hpBarObj = GameObject.Find("DragonHPBar");
            if (hpBarObj != null)
            {
                healthSlider = hpBarObj.GetComponent<Slider>();
                healthSlider.GetComponent<CanvasGroup>().alpha = 1;
            }
            else
            {
                Debug.LogWarning("DragonHPBar 오브젝트를 찾을 수 없습니다.");
            }
        }
        
        if (healthSlider != null)
        {
            healthSlider.value = (float)curHp / maxHp;
        }

        if (healthSlider != null)
        {
            healthSlider.enabled = curHp > 0;
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
    public void SetInvincible(bool isInvincible)
    {
        Debug.Log("SetInvincible" + isInvincible);
        capsuleCollider.enabled = isInvincible; // 서버에서 콜라이더 먼저 적용
        RpcSetColliderState(isInvincible);      // 클라이언트에게도 전파
    }

    [ClientRpc]
    private void RpcSetColliderState(bool enabled)
    {
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
}
