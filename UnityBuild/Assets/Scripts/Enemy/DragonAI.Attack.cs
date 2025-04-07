using System.Collections;
using System.Linq;
using DataSystem;
using Interfaces;
using Mirror;
using Player;
using UnityEngine;
using UnityEngine.UI;

public partial class DragonAI
{
    [Header("Attack Settings")]
    [SerializeField] private DragonAttackConfig[] attackPatterns;

    private DragonAttackConfig selectedAttack;
    private bool isAttacking = false;
    private float attackCooldownTimer = 0f;

    [Header("Target")]
    private PlayerCharacter target;
    
    public GameObject[] projectilePrefabs;              // 발사체 프리팹
    public Transform[] firePoints;                      // 발사 기준점

    
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

    private IEnumerator PerformAttack()
    {
        isAttacking = true;

        animator.SetBool("isMoving", false);
        animator.SetTrigger(selectedAttack.animTrigger);
        RpcPlayAnimation(selectedAttack.animTrigger);

        // 칼날공격일 경우 중간에 타이밍 발사 추가
        if (selectedAttack.attackName == "칼날공격")
        {
            yield return new WaitForSeconds(2.5f);
            FireRandomProjectile();

            yield return new WaitForSeconds(1.5f); // 4초까지 대기
            FireRandomProjectile();

            float remainTime = selectedAttack.attackDuration - 4f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else
        {
            yield return new WaitForSeconds(selectedAttack.attackDuration);
        }

        attackCooldownTimer = selectedAttack.cooldown;
        isAttacking = false;
    }
    
    [Server]
    private void FireRandomProjectile()
    {
        Transform firePoint = firePoints[0]; // 기준점 (입, 앞 등)
        float randomAngle = Random.Range(-45f, 45f);
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f) * firePoint.rotation;

        GameObject prefab = projectilePrefabs[0]; // 칼날공격 전용 프리팹 사용 시 인덱스 맞추기
        GameObject projectile = Instantiate(prefab, firePoint.position, rotation);

        AttackProjectile projComponent = projectile.GetComponent<AttackProjectile>();
        if (projComponent != null)
        {
            float lifetime = 5f;

            projComponent.SetProjectileData(
                selectedAttack.damage,
                selectedAttack.speed,                    // speed
                selectedAttack.radius,                // radius
                selectedAttack.range,  // range
                lifetime,
                selectedAttack.knockback,
                selectedAttack.config,        // AttackConfig
                gameObject,            // owner
                -1, -1                 // playerid, skillid
            );
        }

        NetworkServer.Spawn(projectile);
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
}
