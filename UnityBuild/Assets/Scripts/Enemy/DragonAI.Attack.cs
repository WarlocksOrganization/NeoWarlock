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
    [SerializeField] protected DragonAttackConfig[] attackPatterns;

    protected DragonAttackConfig selectedAttack;
    protected bool isAttacking = false;
    protected float attackCooldownTimer = 0f;

    [Header("Target")]
    protected PlayerCharacter target;
    
    public GameObject[] projectilePrefabs;              // 발사체 프리팹
    public Transform[] firePoints;                      // 발사 기준점
    
    protected bool isFlying = false;
    
    protected int flyAttackCounter = 0; // ✅ 추가: Fly 공격 누적 카운터
    protected int totalAttackCounter = 0; // ✅ 추가: 전체 공격 횟수 카운터
    
    protected Vector3 centerPoint =  new Vector3(0, 2.78f, 0);
    
    [Server]
    protected void SelectRandomTarget()
    {
        var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => !p.isDead)
            .ToList();

        if (players.Count > 0)
        {
            target = players[Random.Range(0, players.Count)];
        }
    }

    protected virtual IEnumerator PerformAttack()
    {
        RotateTowardsTarget(); 
        
        isAttacking = true;

        animator.SetBool("isMoving", false);
        animator.SetFloat("Blend", selectedAttack.blendnum);
        animator.SetTrigger(selectedAttack.animTrigger);
        RpcPlayAnimation(selectedAttack.animTrigger);

        // 칼날공격일 경우 중간에 타이밍 발사 추가
        if (selectedAttack.attackName == "칼날공격")
        {
            yield return new WaitForSeconds(2.5f);
            for (int i = 0; i < 5; i++)
            {
                FireRandomProjectile();
            }
            RpcPlaySound(Constants.SkillType.PowerSlash);

            yield return new WaitForSeconds(1.5f); // 4초까지 대기
            RotateTowardsTarget(); 
            
            for (int i = 0; i < 5; i++)
            {
                FireRandomProjectile();
            }
            RpcPlaySound(Constants.SkillType.PowerSlash);

            float remainTime = selectedAttack.attackDuration - 4f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else if (selectedAttack.attackName == "메테오공격")
        {
            yield return new WaitForSeconds(1f);
            GameSyatemDragonManager gameSyatemDragonManager = GameSystemManager.Instance as GameSyatemDragonManager;
            gameSyatemDragonManager?.MeteorAttack(transform.position);
            RpcPlaySound(Constants.SoundType.SFX_DragonRoar);

            float remainTime = selectedAttack.attackDuration - 1f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else if (selectedAttack.attackName == "바람공격")
        {
            yield return new WaitForSeconds(1.5f);

            RpcPlaySound(Constants.SoundType.SFX_DragonWind);
            
            FireProjectilesIn8Directions(); // ✅ 8방향 발사
            RpcPlaySound(Constants.SkillType.Slash); // 원하시는 효과음으로 교체 가능

            float remainTime = selectedAttack.attackDuration - 1.5f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }

        else if (selectedAttack.attackName == "불공격")
        {
            yield return new WaitForSeconds(1.5f);
            RpcPlaySound(Constants.SkillType.Fire);

            float remainTime = selectedAttack.attackDuration - 1.5f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else
        {
            yield return new WaitForSeconds(selectedAttack.attackDuration);
        }

        attackCooldownTimer = selectedAttack.cooldown;
        isAttacking = false;
        
        totalAttackCounter++;
        flyAttackCounter++;

        if (totalAttackCounter >= 8)
        {
            totalAttackCounter = 0;
            flyAttackCounter = 0;
            StartCoroutine(FlyAndAttack2Sequence()); // ✅ Fly2 공격 우선 실행
        }
        else if (flyAttackCounter >= 5)
        {
            flyAttackCounter = 0;
            StartCoroutine(FlyAndAttackSequence()); // ✅ Fly 공격
        }
    }
    
    protected IEnumerator FlyAndAttackSequence()
    {
        isFlying = true;
        animator.SetTrigger("isFly");
        RpcPlayAnimation("isFly");

        SetCollider(false);
        
        RpcPlaySound(Constants.SoundType.SFX_DragonWing);

        yield return new WaitForSeconds(2f);

        RpcRemoveFromTargetGroup();
        transform.position = centerPoint;

        yield return new WaitForSeconds(1f);
        EnemyModel.rotation = Quaternion.Euler(0, 180, 0);

        if (GameSystemManager.Instance is GameSyatemDragonManager dragonManager)
        {
            dragonManager.DragonFlyAttack();
        }
    }
    
    protected IEnumerator FlyAndAttack2Sequence()
    {
        isFlying = true;
        animator.SetTrigger("isFly");
        RpcPlayAnimation("isFly");

        SetCollider(false);

        yield return new WaitForSeconds(2f);

        RpcRemoveFromTargetGroup();
        transform.position = centerPoint;

        yield return new WaitForSeconds(1f);
        EnemyModel.rotation = Quaternion.Euler(0, 180, 0);
        
        animator.SetTrigger("isFlyAttack");
        RpcPlayAnimation("isFlyAttack");
        
        yield return new WaitForSeconds(3f);

        if (GameSystemManager.Instance is GameSyatemDragonManager dragonManager)
        {
            dragonManager.DragonFlyAttack2();
        }
    }
    
    [Server]
    protected void FireRandomProjectile()
    {
        Transform firePoint = firePoints[0]; // 기준점 (입, 앞 등)
        
        float randomAngle = Random.Range(-90f, 90f);
        
        if (curHp <= maxHp / 2f)
        {
            randomAngle = Random.Range(-180f, 180f);
        }
        
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
    
    [Server]
    protected void FireProjectilesIn8Directions()
    {
        Transform firePoint = firePoints[0]; // 기준 발사 위치
        GameObject prefab = projectilePrefabs[0];

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f + 45f;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 dir = rotation * Vector3.forward;

            GameObject projectile = Instantiate(prefab, firePoint.position, Quaternion.LookRotation(dir));

            AttackProjectile projComponent = projectile.GetComponent<AttackProjectile>();
            if (projComponent != null)
            {
                float lifetime = 10f;

                projComponent.SetProjectileData(
                    selectedAttack.damage,
                    selectedAttack.speed,
                    selectedAttack.radius,
                    selectedAttack.range,
                    lifetime,
                    selectedAttack.knockback,
                    selectedAttack.config,
                    gameObject,
                    -1, -1
                );
            }

            NetworkServer.Spawn(projectile);
        }

        if (curHp <= maxHp / 2f)
        {
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f; // 360 / 8 = 45도씩
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 dir = rotation * Vector3.forward;

                GameObject projectile = Instantiate(prefab, firePoint.position, Quaternion.LookRotation(dir));

                AttackProjectile projComponent = projectile.GetComponent<AttackProjectile>();
                if (projComponent != null)
                {
                    float lifetime = 10f;

                    projComponent.SetProjectileData(
                        selectedAttack.damage,
                        selectedAttack.speed,
                        selectedAttack.radius,
                        selectedAttack.range,
                        lifetime,
                        selectedAttack.knockback,
                        selectedAttack.config,
                        gameObject,
                        -1, -1
                    );
                }

                NetworkServer.Spawn(projectile);
            }
        }
    }
}
