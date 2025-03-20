using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTimeChanged))] // ✅ 모든 클라이언트와 시간 동기화
    public int currentTime = 10;

    public static NetworkTimer Instance; // ✅ 싱글톤 패턴으로 접근

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    [Server] // ✅ 서버에서만 실행
    public void StartCountdown(int time)
    {
        StopAllCoroutines();
        StartCoroutine(CountdownCoroutine(time));
    }

    [Server] // ✅ 서버에서만 실행
    private IEnumerator CountdownCoroutine(int time)
    {
        currentTime = time;
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1);
            if (currentTime == 1)
            {
                StartEvent();
            }
            currentTime--;
        }
    }

    private void OnTimeChanged(int oldTime, int newTime)
    {
        GamePlayUI.Instance.UpdateCountdownUI(newTime);
    }
    
    [Server] // ✅ 서버에서만 실행
    public void StartEvent()
    {
        RpcStartEvent();
    }

    [ClientRpc]
    private void RpcStartEvent()
    {
        GamePlayUI.Instance.UpdateCountdownUI(0);
        GameSystemManager.Instance.StartEvent();
    }
}