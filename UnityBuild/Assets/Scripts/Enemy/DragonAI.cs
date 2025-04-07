using System.Collections;
using System.Linq;
using DataSystem;
using Interfaces;
using Mirror;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class DragonAI : NetworkBehaviour, IDamagable
{
     [Header("Stats")]
    [SyncVar(hook = nameof(OnHpChanged))] private int curHp;
    public int maxHp = 50;
    public float moveSpeed = 2f;
    public float attackRange = 2f;
    public int damage = 10;
    public int knockbackDamage = 1;
    public float attackCooldown = 2f;
    
    [Header("Attack Settings")]
    [SerializeField] private DragonAttackConfig[] attackPatterns;

    private DragonAttackConfig selectedAttack;

    private bool isAttacking = false;
    private float attackCooldownTimer = 0f;

    [Header("Target")]
    private PlayerCharacter target;
    private float lastAttackTime;

    [Header("Components")]
    [SerializeField] private Animator animator;

    [SerializeField] private Transform EnemyModel;

    [Header("Health UI")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider healthSlider;
    
    [Header("ETC")]
    [SerializeField] private GameObject floatingDamageTextPrefab;
    

    private bool isLanded = false;

    private void Awake()
    {
        curHp = maxHp;
        UpdateHealthUI();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateHealthUI();
    }
    
    [ServerCallback]
    private void Start()
    {
        StartCoroutine(StartLandingSequence());
        RpcAddToTargetGroup(transform);
    }
    
    [ClientRpc]
    private void RpcAddToTargetGroup(Transform target)
    {
        var localPlayer = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isOwned);

        if (localPlayer != null)
        {
            localPlayer.AddTargetToCamera(target);
        }
    }

    private IEnumerator StartLandingSequence()
    {
        yield return new WaitForSeconds(20f);

        SelectRandomTarget();

        if (target != null)
        {
            RpcPlayAnimation("isLanding");

            yield return new WaitForSeconds(5f); // 착지 애니메이션 시간

            isLanded = true; // 이제부터 AI 행동 시작
        }
    }
    
    [Server]
    private void SelectRandomTarget()
    {
        var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => !p.isDead)
            .ToList();

        if (players.Count > 0)
        {
            target = players[Random.Range(0, players.Count)];
        }
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (!isServer || !isLanded || curHp <= 0) return;

        // 공격 쿨타임 갱신
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.fixedDeltaTime;

        if (isAttacking) return; // 공격 중이면 아무것도 안 함

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
    
    private IEnumerator PerformAttack()
    {
        isAttacking = true;

        animator.SetBool("isMoving", false);
        RpcPlayAnimation(selectedAttack.animTrigger);

        yield return new WaitForSeconds(selectedAttack.attackDuration); // 타격 타이밍

        attackCooldownTimer = selectedAttack.cooldown;
        isAttacking = false;
    }


    private void RotateTowardsTarget()
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        Vector3 rotation = Quaternion.Lerp(EnemyModel.rotation, lookRotation, Time.deltaTime * 5f).eulerAngles;
        EnemyModel.rotation = Quaternion.Euler(0f, rotation.y, 0f);
    }

    private void MoveTowardsTarget()
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        animator.SetBool("isMoving", true);
    }

    [Server]
    public int takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid)
    {
        if (curHp <= 0) return 0;

        curHp -= damage;

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
        RpcPlayAnimation("isDead");
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
    private void RpcPlayAnimation(string trigger)
    {
        animator.SetTrigger(trigger);
    }
    
    [ClientRpc]
    private void ShowFloatingDamageText(int damage)
    {
        if (floatingDamageTextPrefab == null) return;

        GameObject damageTextInstance = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up*3f, Quaternion.identity);
        damageTextInstance.GetComponent<FloatingDamageText>().SetDamageText(damage);
    }

}