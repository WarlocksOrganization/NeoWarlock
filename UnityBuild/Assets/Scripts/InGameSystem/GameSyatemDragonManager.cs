using System;
using System.Collections;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSyatemDragonManager : GameSystemManager
{
    [SerializeField] private Transform lavaTrans;

    [SerializeField] private GameObject FlyingDragon;
    [SerializeField] private GameObject FlyingDragonSoundObject;
    
    [SerializeField] private GameObject skillItemPickupPrefab;
    [SerializeField] private GameObject AttackPrefab;
    [SerializeField] private AttackConfig attackConfig;

    public override void StartEvent()
    {
        if (!NetworkServer.active || lavaTrans == null) return;
        
        eventnum++; // 다음 이벤트로 증가

        //StartCoroutine(RaiseLava(targetY, riseDuration));
        
        //NetEvent();
        
        //RpcShakeCameraWhileLavaRises(1f, 1.5f, riseDuration);
        
        GameSystemManager.Instance.EndEventAndStartNextTimer(); // 다음 타이머 시작
    }

    private IEnumerator RaiseLava(float targetY, float duration)
    {
        Vector3 startPos = lavaTrans.position;
        Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);
        
        PlaySFX(Constants.SoundType.SFX_Lava);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            lavaTrans.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lavaTrans.position = endPos; // 정확히 목표 위치로
    }

    public override void NetEvent()
    {
        if (Random.value < 0.5f)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            StartFlyingDragon(randomDirection);
            
            int itemCount = Random.Range(1, 4); // 🔹 1~3 사이의 랜덤한 개수

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 spawnPosition = new Vector3(
                    Random.Range(-20f, 20f),
                    Random.Range(30f, 40f),
                    Random.Range(-20f, 20f)
                );

                GameObject pickup = Instantiate(skillItemPickupPrefab, spawnPosition, Quaternion.identity);

                NetworkServer.Spawn(pickup);
            }
        }
        /*else
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            
            StartFlyingDragon(randomDirection);
            
            int attackCount = Random.Range(5, 10); // 🔹 2~4개 낙하 공격 소환
            
            // ✅ Coroutine으로 시간차 낙하 공격 시작
            //StartCoroutine(SpawnFallingAttacks(attackCount));
        }*/
    }

    public void MeteorAttack(Vector3 pos)
    {
        int attackCount = Random.Range(5, 10); // 🔹 2~4개 낙하 공격 소환
            
        // ✅ Coroutine으로 시간차 낙하 공격 시작
        StartCoroutine(SpawnFallingAttacks(attackCount, pos));
    }
    
    private IEnumerator SpawnFallingAttacks(int count, Vector3 pos)
    {
        yield return new WaitForSeconds(1.5f);
        var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
    
        foreach (var player in players)
        {
            if (player.isDead)
            {
                continue;
            }
            Quaternion downRotation = Quaternion.LookRotation(Vector3.down);

            GameObject attack = Instantiate(AttackPrefab, pos + player.transform.position + Vector3.up * 40f, downRotation);
            
            GameObject dragon = FindFirstObjectByType<DragonAI>().gameObject;

            attack.GetComponent<AttackProjectile>().SetProjectileData(
                10, // damage
                20, // speed
                7.5f, // radius
                5, // range
                10, // duration
                3, // knockback
                attackConfig,
                dragon,
                -1,
                -1
            );

            NetworkServer.Spawn(attack);

            // ✅ 0.1초 지연
            yield return new WaitForSeconds(0.25f);
        }
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = new Vector3(
                Random.Range(-50f, 50f),
                40f,
                Random.Range(-50f, 50f)
            );

            Quaternion downRotation = Quaternion.LookRotation(Vector3.down);

            GameObject attack = Instantiate(AttackPrefab, pos + spawnPosition, downRotation);
            
            GameObject dragon = FindFirstObjectByType<DragonAI>().gameObject;

            attack.GetComponent<AttackProjectile>().SetProjectileData(
                10, // damage
                20, // speed
                7.5f, // radius
                5, // range
                10, // duration
                3, // knockback
                attackConfig,
                dragon,
                -1,
                -1
            );

            NetworkServer.Spawn(attack);

            // ✅ 0.1초 지연
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void DragonFlyAttack()
    {
        StartCoroutine(FlyAndAttackSequence());
    }

    private IEnumerator FlyAndAttackSequence()
    {
        // 드래곤 이륙 방향 설정 및 애니메이션
        

        // 5초 간격으로 3회 낙하 공격
        for (int i = 0; i < 3; i++)
        {
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            StartFlyingDragon(dir);
        
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_FlyingDragon, FlyingDragonSoundObject);
            FlyingDragon.transform.rotation = Quaternion.LookRotation(dir);
            FlyingDragon.GetComponent<Animator>().SetTrigger("isFlyAttack");
            
            
            int attackCount = Random.Range(5, 10);
            StartCoroutine(SpawnFallingAttacks(attackCount, Vector3.zero));
            yield return new WaitForSeconds(5f);
        }

        // 착지 준비
        FindFirstObjectByType<DragonAI>()?.Init();
    }
    
    public void DragonFlyAttack2()
    {
        StartCoroutine(FlyAndAttackSequence2());
    }

    private IEnumerator FlyAndAttackSequence2()
    {
        // 드래곤 이륙 방향 설정 및 애니메이션
        
        FlyingDragon.GetComponent<Animator>().SetTrigger("isFlyAttack2");
        StartFlyAttack2();

        // 5초 간격으로 3회 낙하 공격
        for (int i = 0; i < 6; i++)
        {
            int attackCount = Random.Range(10, 15);
            StartCoroutine(SpawnFallingAttacks(attackCount, Vector3.zero));
            yield return new WaitForSeconds(5f);
        }

        FindFirstObjectByType<DragonAI>()?.Init();
    }

    [ClientRpc]
    private void StartFlyingDragon(Vector3 dir)
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_DragonFire, FlyingDragonSoundObject);
        FlyingDragon.transform.rotation = Quaternion.LookRotation(dir);
        FlyingDragon.GetComponent<Animator>().SetTrigger("isFlyAttack");
    }
    
    [ClientRpc]
    private void StartFlyAttack2()
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_DragonRoar);
        FlyingDragon.GetComponent<Animator>().SetTrigger("isFlyAttack2");
    }
}