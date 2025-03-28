using System.Collections;
using System.Linq;
using Mirror;
using Player;
using UnityEngine;

public class GameSyatemLavaManager : GameSystemManager
{
    
    public override void StartEvent()
    {
        
    }
    
    // 🔹 Coroutine으로 5초 후 지형 파괴
    private IEnumerator DelayedFall(GameObject groundGroup, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (groundGroup != null)
        {
            FallGround[] fallGrounds = groundGroup.GetComponentsInChildren<FallGround>();
            foreach (var fallGround in fallGrounds)
            {
                fallGround.Fall();
            }
        }
        GameSystemManager.Instance.EndEventAndStartNextTimer();
    }
    
    public override void NetEvent()
    {
        
    }
}
