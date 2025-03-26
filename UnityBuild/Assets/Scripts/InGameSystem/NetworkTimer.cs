// ✅ 목적: 타이머 동기화 + UI 출력 + 이벤트 시작까지 확실히 되는 구조로 재구성

using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    private GamePlayUI gamePlayUI;

    private void Awake()
    {
        gamePlayUI = FindFirstObjectByType<GamePlayUI>();
    }

    // 서버 → 모든 클라이언트에게 Phase1 타이머 시작 지시
    [Server]
    public void StartGameFlow(int countdown1, int countdown2)
    {
        RpcStartPhase(1, countdown1);
        StartCoroutine(ServerCountdown(countdown1, () =>
        {
            RpcForcePhaseStart(2);
            StartPhase2(countdown2); // 👈 이후 재사용 가능하도록 분리
        }));
    }

    // 서버 전용 코루틴 (클라이언트는 자신 타이머 따로 돌림)
    private IEnumerator ServerCountdown(int time, System.Action onComplete)
    {
        yield return new WaitForSeconds(time);
        onComplete?.Invoke();
    }

    // 클라이언트에서 카운트다운 시작
    [ClientRpc]
    private void RpcStartPhase(int phase, int seconds)
    {
        gamePlayUI?.StartCountdownUI(phase, seconds);
    }

    // 클라이언트에서 게임 상태 전환
    [ClientRpc]
    private void RpcForcePhaseStart(int phase)
    {
        if (phase == 2)
        {
            gamePlayUI?.ForceStartPhase2();
        }
    }

    // 클라이언트에서 이벤트 실행
    [ClientRpc]
    private void RpcTriggerEvent()
    {
        gamePlayUI?.UpdateCountdownUI(0, 2); // ✅ phase 2로 명확히 전달
        GameSystemManager.Instance.StartEvent();
    }
    
    [Server]
    public void StartPhase2(int countdown)
    {
        RpcStartPhase(2, countdown);
        StartCoroutine(ServerCountdown(countdown, () =>
        {
            RpcTriggerEvent();
            GameSystemManager.Instance.StartEvent();
        }));
    }

}