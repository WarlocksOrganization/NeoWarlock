using System;
using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameSyatemSeaMonsterManager : GameSystemManager
{
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private GameObject monsterTrans;
    
    [SerializeField] private GameObject skillItemPickupPrefab;

    protected override void Start()
    {
        base.Start();
        
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
        
        Debug.Log("[GameSystemManager] StartEvent()");
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
        
    }

    
    public void Attack()
    {
        monsterAnimator.SetTrigger("isAttack");
        RocAttack();
    }
    
    [ClientRpc]
    public void RocAttack()
    {
        monsterAnimator.SetTrigger("isAttack");
    }

    
    private void MeteorExplosion(Vector3 target)
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_MeteorExplosion, target);
    }
}