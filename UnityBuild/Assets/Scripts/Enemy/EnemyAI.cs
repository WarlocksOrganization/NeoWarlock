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

        if (target == null || target.isDead)
        {
            FindClosestPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist > attackRange)
        {
            // 이동
            Vector3 dir = (target.transform.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;

            animator.SetBool("isMoving", true);

            // ✅ Y축 회전만 적용하여 EnemyModel이 이동 방향을 바라보도록 함
            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                Vector3 euler = lookRot.eulerAngles;
                EnemyModel.rotation = Quaternion.Euler(0f, euler.y, 0f); // Y축만 반영
            }
        }
        else
        {
            animator.SetBool("isMoving", false);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.SetTrigger("isAttack");

                // 딜 적용
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
    public void takeDamage(int damage, Vector3 attackTran, float knockbackForce, AttackConfig attackConfig, int playerid, int skillid)
    {
        if (curHp <= 0) return;

        curHp -= damage;

        if (curHp <= 0)
        {
            curHp = 0;
            Die();
        }
    }

    [Server]
    private void Die()
    {
        animator.SetTrigger("isDead");
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
}