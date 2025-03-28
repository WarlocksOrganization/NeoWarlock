using System.Collections;
using GameManagement;
using Mirror;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    private GamePlayUI gamePlayUI;

    // ✅ gamePlayUI를 매번 동적으로 참조 (씬 리로드 대응)
    private GamePlayUI GamePlayUI => gamePlayUI != null ? gamePlayUI : gamePlayUI = FindFirstObjectByType<GamePlayUI>();
    
    private bool gameStarted = false;

    // 서버에서 Phase1 카운트다운 시작
    
    [Server]
    public void StartGameFlow(int countdown1, int countdown2)
    {
        if (gameStarted) return;
        gameStarted = true;

        Debug.Log("[NetworkTimer] StartGameFlow 시작 - Phase1 카운트다운 시작");
        RpcStartPhase(1, countdown1);

        StartCoroutine(ServerCountdown(countdown1, () =>
        {
            Debug.Log("[NetworkTimer] Phase1 종료 - Phase2 진입 지시");
            StartCoroutine(DelayThenStartPhase2(countdown2));
        }));
    }

    // Phase2 시작 지연 (카운트다운 UI 시간 확보용)
    private IEnumerator DelayThenStartPhase2(int countdown2)
    {
        yield return new WaitForSeconds(1f); // 1초 지연
        StartPhase2(countdown2);
    }

    // 서버용 코루틴
    private IEnumerator ServerCountdown(int time, System.Action onComplete)
    {
        yield return new WaitForSeconds(time);
        onComplete?.Invoke();
    }

    // 모든 클라이언트에 Phase1 or Phase2 카운트다운 시작 알림
    [ClientRpc]
    private void RpcStartPhase(int phase, int seconds)
    {
        Debug.Log($"[NetworkTimer] RpcStartPhase 호출 - Phase {phase}, {seconds}초");
        GamePlayUI?.StartCountdownUI(phase, seconds);
    }

    // 이벤트 실행 트리거 (서버+클라이언트 동기화)
    [ClientRpc]
    private void RpcTriggerEvent()
    {
        Debug.Log("[NetworkTimer] RpcTriggerEvent - Phase2 이벤트 실행");
        GamePlayUI?.UpdateCountdownUI(0, 2);
        GameSystemManager.Instance.StartEvent();
    }

    // Phase2 타이머 시작 (서버 기준)
    [Server]
    public void StartPhase2(int countdown)
    {
        Debug.Log("[NetworkTimer] StartPhase2 시작");
        RpcStartPhase(2, countdown);
        StartCoroutine(ServerCountdown(countdown, () =>
        {
            Debug.Log("[NetworkTimer] Phase2 종료 - 이벤트 실행");
            RpcTriggerEvent();
            GameSystemManager.Instance.StartEvent();
        }));
    }
}