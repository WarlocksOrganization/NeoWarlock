using System.Linq;
using Interfaces;
using Mirror;
using Player;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAI : NetworkBehaviour, IDamagable
{
    [Header("Stats")]
    [SyncVar(hook = nameof(OnHpChanged))] private int curHp;
    public int maxHp = 50;
    public float moveSpeed = 2f;
    public float attackRange = 2f;
    public int damage = 10;
    public int knockbackDamage = 1;
    public float attackCooldown = 2f;

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
    private void Update()
    {
        if (curHp <= 0) return;

        if (Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }

        if (target == null || target.isDead)
        {
            FindClosestPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist > attackRange)
        {
            Vector3 dir = (target.transform.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;

            animator.SetBool("isMoving", true);

            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                Vector3 euler = lookRot.eulerAngles;
                EnemyModel.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }
        }
        else
        {
            animator.SetBool("isMoving", false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                RpcPlayAttackAnimation(); // ✅ 클라이언트에 공격 애니메이션 재생 지시

                target.takeDamage(damage, transform.position, knockbackDamage, null, -1, -1);
            }
        }
    }

    [Server]
    private void FindClosestPlayer()
    {
        var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => !p.isDead)
            .ToList();

        float closestDistance = float.MaxValue;
        PlayerCharacter closest = null;

        foreach (var p in players)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = p;
            }
        }

        target = closest;
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
        RpcPlayDeathAnimation();
        // 약간의 딜레이 후 파괴
        Invoke(nameof(DestroySelf), 2f);
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
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
    private void ShowFloatingDamageText(int damage)
    {
        if (floatingDamageTextPrefab == null) return;

        GameObject damageTextInstance = Instantiate(floatingDamageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        damageTextInstance.GetComponent<FloatingDamageText>().SetDamageText(damage);
    }
    
    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        animator.SetTrigger("isAttack");
    }

    [ClientRpc]
    private void RpcPlayDeathAnimation()
    {
        animator.SetTrigger("isDead");
    }

}