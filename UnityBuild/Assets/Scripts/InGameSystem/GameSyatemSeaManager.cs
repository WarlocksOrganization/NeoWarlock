using System;
using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSyatemSeaManager : GameSystemManager
{
    [SerializeField] private GameObject[] FallGrounds;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private GameObject monsterTrans;
    
    [SerializeField] private GameObject skillItemPickupPrefab;

    [SyncVar(hook = nameof(OnFallGroundOrderChanged))]
    private string fallGroundOrderStr; // 순서 정보를 문자열로 공유 (SyncVar 제한 우회)

    private int[] fallGroundOrder;           // 섞인 순서
    private GameObject[] originalFallGrounds; // 섞기 전 순서

    protected override void Start()
    {
        base.Start();

        if (isServer)
        {
            ShuffleFallGroundsExceptLast(); // 서버에서만 섞음
        }
        else
        {
            // 클라이언트는 훅 함수에서 fallGroundOrder 적용
        }
        
        var dirLight = FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(l => l.type == LightType.Directional);

        if (dirLight != null)
        {
            dirLight.color = new Color(0, 1, 1);
        }
    }

    public override void StartEvent()
    {
        base.StartEvent();
        
        if (!NetworkServer.active) return;
        
        if (FallGrounds == null || FallGrounds.Length == 0) return;
        if (eventnum >= FallGrounds.Length) return;

        GameObject selectedGround = FallGrounds[eventnum];

        // 원래 배열에서 현재 ground가 몇 번째였는지 찾기
        int originalIndex = Array.IndexOf(originalFallGrounds, selectedGround);

        // 회전 적용
        monsterTrans.transform.rotation = Quaternion.Euler(0, -45 + 90 * originalIndex, 0);
        Debug.Log(-45 + 90 * originalIndex);
        
        NetEvent();

        MeteorFall();

        StartCoroutine(DelayedFall(selectedGround, 5f));
        Debug.Log("[GameSystemManager] StartEvent()");
    }

    private void ShuffleFallGroundsExceptLast()
    {
        originalFallGrounds = FallGrounds.ToArray(); // 원래 순서 저장
        
        int len = FallGrounds.Length;
        if (len <= 1) return;

        var indices = Enumerable.Range(0, len).ToList();
        for (int i = 0; i < indices.Count - 1; i++)
        {
            int rand = Random.Range(i, indices.Count);
            (indices[i], indices[rand]) = (indices[rand], indices[i]);
        }

        fallGroundOrder = indices.ToArray();
        fallGroundOrderStr = string.Join(",", fallGroundOrder); // 문자열로 저장하여 SyncVar로 전송

        ApplyFallGroundOrder();
    }

    private void ApplyFallGroundOrder()
    {
        if (fallGroundOrder == null || fallGroundOrder.Length != FallGrounds.Length)
            return;

        GameObject[] ordered = new GameObject[FallGrounds.Length];
        for (int i = 0; i < fallGroundOrder.Length; i++)
        {
            ordered[i] = FallGrounds[fallGroundOrder[i]];
        }

        FallGrounds = ordered;
    }

    private void OnFallGroundOrderChanged(string oldOrder, string newOrder)
    {
        // 클라이언트가 SyncVar 업데이트 받을 때 실행됨
        fallGroundOrder = newOrder.Split(',').Select(int.Parse).ToArray();
        ApplyFallGroundOrder();
    }

    private IEnumerator DelayedFall(GameObject groundGroup, float delay)
    {
        yield return new WaitForSeconds(4);
        
        PlaySFX(Constants.SoundType.SFX_MonsterLaser, monsterAnimator.gameObject);
        
        yield return new WaitForSeconds(delay-4);

        if (groundGroup != null)
        {
            RpcDelayedFall(groundGroup.transform.position, groundGroup.name);
        }
        yield return new WaitForSeconds(10f);
        GameSystemManager.Instance.EndEventAndStartNextTimer();
    }
    
    [ClientRpc]
    private void RpcDelayedFall(Vector3 position, string groundName)
    {
        MeteorExplosion(position);

        // 이름으로 찾아서 Fall 실행 (네트워크 객체 찾기 위해 이름 활용)
        
        GameObject target = GameObject.Find(groundName);
        if (target != null)
        {
            // 자식 포함 전체에서 FallGround 컴포넌트 찾기
            FallGround[] fallGrounds = target.GetComponentsInChildren<FallGround>(true);
            foreach (var fg in fallGrounds)
            {
                fg.Fall();
            }
        }
        else
        {
            Debug.LogWarning("[SpaceManager] RpcDelayedFall(): Ground 오브젝트를 찾을 수 없습니다: " + groundName);
        }
    }



    public override void NetEvent()
    {
        if (eventnum >= FallGrounds.Length) return;
        
        if (Random.value < 0.5f)
        {
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            
            int itemCount = Random.Range(1, 4); // 🔹 1~3 사이의 랜덤한 개수

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 spawnPosition = new Vector3(
                    Random.Range(-40f, 40f),
                    Random.Range(30f, 40f),
                    Random.Range(-40f, 40f)
                );

                GameObject pickup = Instantiate(skillItemPickupPrefab, spawnPosition, Quaternion.identity);

                NetworkServer.Spawn(pickup);
            }
        }

        RpcPlayNextFall(eventnum);

        eventnum++; // 다음 이벤트로 이동
    }
    
    [ClientRpc]
    private void RpcPlayNextFall(int index)
    {
        if (index >= FallGrounds.Length) return;

        GameObject nextGround = FallGrounds[index];
        if (nextGround != null)
        {
            // 자식 포함 전체에서 FallGround 컴포넌트 찾기
            FallGround[] nextFallGrounds = nextGround.GetComponentsInChildren<FallGround>(true);
            foreach (var nextFallGround in nextFallGrounds)
            {
                nextFallGround.NextFall();
            }
        }
    }


    [ClientRpc]
    private void MeteorFall()
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_FallingMeteor, monsterAnimator.gameObject);
        monsterAnimator.SetTrigger("isStart");
    }
    
    private void MeteorExplosion(Vector3 target)
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_MeteorExplosion, target);
    }
}