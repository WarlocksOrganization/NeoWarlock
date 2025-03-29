using System.Collections;
using DataSystem;
using Mirror;
using UnityEngine;

public class GameSyatemLavaManager : GameSystemManager
{
    [SerializeField] private Transform lavaTrans;
    [SerializeField] private float riseDuration = 5f; // 5초
    [SerializeField] private float risePerEvent = 0.5f;

    [SerializeField] private GameObject FlyingDragon;
    [SerializeField] private GameObject FlyingDragonSoundObject;
    
    [SerializeField] private GameObject skillItemPickupPrefab;
    [SerializeField] private GameObject AttackPrefab;

    public override void StartEvent()
    {
        if (!NetworkServer.active || lavaTrans == null) return;
        
        eventnum++; // 다음 이벤트로 증가

        float targetY = eventnum * risePerEvent;
        StartCoroutine(RaiseLava(targetY, riseDuration));
        
        NetEvent();
        
        RpcShakeCameraWhileLavaRises(1f, 1.5f, riseDuration);
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
        
        GameSystemManager.Instance.EndEventAndStartNextTimer(); // 다음 타이머 시작
    }

    public override void NetEvent()
    {
        if (Random.value <= 0.5f)
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
    }

    [ClientRpc]
    private void StartFlyingDragon(Vector3 dir)
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_FlyingDragon, FlyingDragonSoundObject);
        FlyingDragon.transform.rotation = Quaternion.LookRotation(dir);
        FlyingDragon.GetComponent<Animator>().SetTrigger("isStart");
    }
}