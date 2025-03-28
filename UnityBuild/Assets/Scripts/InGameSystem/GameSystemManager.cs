using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;

public class GameSystemManager : MonoBehaviour
{
    public static GameSystemManager Instance;
    
    protected int eventnum = 0;

    protected void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // ✅ 중복된 Instance 제거
    }

    public virtual void StartEvent()
    {
       
    }
    
    public virtual void NetEvent()
    {
   
    }
    
    // GameSystemManager.cs

    public void EndEventAndStartNextTimer()
    {
        var timer = FindFirstObjectByType<NetworkTimer>();
        if (timer != null && NetworkServer.active)
        {
            timer.StartPhase2(Constants.MaxGameEventTime);
        }
    }

}
