using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;

public partial class SeaMonsterAI
{
    [ServerCallback]
    protected void FixedUpdate()
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
        }
    }
    
    protected IEnumerator PerformAttack()
    {
        RotateTowardsTarget(); 
        
        isAttacking = true;
        
        animator.SetFloat("Blend", selectedAttack.blendnum);
        animator.SetTrigger(selectedAttack.animTrigger);
        RpcPlayAnimation(selectedAttack.animTrigger);

        // 칼날공격일 경우 중간에 타이밍 발사 추가
        if (selectedAttack.attackName == "버블공격")
        {
            yield return new WaitForSeconds(2.5f);
            for (int i = 0; i < 35; i++)
            {
                BubbleRandomProjectile();
            }
            RpcPlaySound(Constants.SoundType.SFX_MonsterBubble);

            float remainTime = selectedAttack.attackDuration - 2.5f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else if (selectedAttack.attackName == "직선레이저공격")
        {
            RpcPlaySound(Constants.SoundType.SFX_MonsterCharge);
            
            float rotateDuration = 1.5f;
            float elapsed = 0f;

            while (elapsed < rotateDuration)
            {
                RotateTowardsTarget();
                elapsed += Time.deltaTime;
                yield return null; // 한 프레임 대기
            }

            yield return new WaitForSeconds(1.5f); // 나머지 2초 대기 (총 4초까지 대기)
            
            RpcPlaySound(Constants.SoundType.SFX_MonsterLaserAttack);

            float remainTime = selectedAttack.attackDuration - 3f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else if (selectedAttack.attackName == "칼날공격")
        {
            yield return new WaitForSeconds(1.5f);
            for (int i = 0; i < 10; i++)
            {
                for (int i1 = 0; i1 < 5; i1++)
                {
                    BladeRandomProjectile();
                
                }
                RpcPlaySound(Constants.SoundType.SFX_MonsterBlade);
                yield return new WaitForSeconds(0.2f);
            }

            float remainTime = selectedAttack.attackDuration - 3.5f;
            if (remainTime > 0)
                yield return new WaitForSeconds(remainTime);
        }
        else if (selectedAttack.attackName == "회전레이저공격")
        {
            RpcPlaySound(Constants.SoundType.SFX_MonsterCharge);
            
            float rotateDuration = 2f;
            float elapsed = 0f;

            float rad = Random.Range(0f, 1f);
            if (rad < 0.5f)
            {
                animator.SetFloat("Blend", selectedAttack.blendnum + 1);
            }

            while (elapsed < rotateDuration)
            {
                RotateToForward();
                elapsed += Time.deltaTime;
                yield return null; // 한 프레임 대기
            }

            yield return new WaitForSeconds(1.0f); // 나머지 2초 대기 (총 4초까지 대기)
            RpcPlaySound(Constants.SoundType.SFX_MonsterLaserAttack);
            
            yield return new WaitForSeconds(1.0f);
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
        
        totalAttackCounter++;
        
        if (totalAttackCounter >= 5)
        {
            totalAttackCounter = 0;
            RpcPlaySound(Constants.SoundType.SFX_MonsterScream);
            if (GameSystemManager.Instance is GameSyatemSeaMonsterManager monsterManager)
            {
                monsterManager.Attack();
            }
        }
    }
    
    [Server]
    protected void BubbleRandomProjectile()
    {
        Transform firePoint = firePoints[0]; // 기준점 (입, 앞 등)
        
        float randomAngle = Random.Range(-180f, 180f);
        
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f) * firePoint.rotation;

        GameObject prefab = projectilePrefabs[1]; // 칼날공격 전용 프리팹 사용 시 인덱스 맞추기
        GameObject projectile = Instantiate(prefab, firePoint.position, rotation);

        AttackProjectile projComponent = projectile.GetComponent<AttackProjectile>();
        if (projComponent != null)
        {
            float lifetime = 10f;

            projComponent.SetProjectileData(
                selectedAttack.damage,
                Random.Range(selectedAttack.speed, selectedAttack.speed * 2), // speed
                Random.Range(selectedAttack.radius, selectedAttack.radius * 2),// radius
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
    protected void BladeRandomProjectile()
    {
        Transform firePoint = firePoints[1]; // 기준점 (입, 앞 등)
        
        float randomAngle = Random.Range(-90f, 90f);
        
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f) * firePoint.rotation;

        GameObject prefab = projectilePrefabs[0]; // 칼날공격 전용 프리팹 사용 시 인덱스 맞추기
        GameObject projectile = Instantiate(prefab, firePoint.position, rotation);

        AttackProjectile projComponent = projectile.GetComponent<AttackProjectile>();
        if (projComponent != null)
        {
            float lifetime = 10f;

            projComponent.SetProjectileData(
                selectedAttack.damage,
                selectedAttack.speed,
                selectedAttack.radius,
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
}
