using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTimeChanged))] // ✅ 모든 클라이언트와 시간 동기화
    public int currentTime = 10;

    private GamePlayUI gamePlayUI;

    private void Awake()
    {
        gamePlayUI = FindFirstObjectByType<GamePlayUI>();
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
        gamePlayUI.UpdateCountdownUI(newTime);
    }
    
    [Server] // ✅ 서버에서만 실행
    public void StartEvent()
    {
        RpcStartEvent();
    }

    [ClientRpc]
    private void RpcStartEvent()
    {
        gamePlayUI.UpdateCountdownUI(0);
        GameSystemManager.Instance.StartEvent();
    }
}