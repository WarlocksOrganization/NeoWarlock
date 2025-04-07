using System.Collections;
using System.Linq;
using DataSystem;
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
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider healthSlider;

    [Header("ETC")]
    [SerializeField] private GameObject floatingDamageTextPrefab;

    private bool isLanded = false;

    private void Awake()
    {
        Instance = this;
        curHp = maxHp;
        UpdateHealthUI();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHealthUI();
    }

    public void Init()
    {
        transform.position = new Vector3(0, 2.78f, 0);
        RpcAddToTargetGroup(transform);
        StartCoroutine(StartLandingSequence());
    }

    private IEnumerator StartLandingSequence()
    {
        SelectRandomTarget();

        if (target != null)
        {
            animator.SetTrigger("isLanding");
            RpcPlayAnimation("isLanding");

            yield return new WaitForSeconds(10f); // 착지 애니메이션 시간

            isLanded = true;
        }
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (!isServer || !isLanded || curHp <= 0) return;

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
            MoveTowardsTarget();
        }
    }

    [Server]
    public int takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid)
    {
        if (curHp <= 0) return 0;

        curHp -= damage;

        RpcShowFloatingDamage(damage);

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
        if (healthSlider != null)
        {
            healthSlider.value = (float)curHp / maxHp;
        }

        if (healthCanvas != null)
        {
            healthCanvas.enabled = curHp > 0;
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
}
