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
        
        RpcStartPhase(1, countdown1);

        StartCoroutine(ServerCountdown(countdown1, () =>
        {
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
        StartCoroutine(WaitAndStartCountdown(phase, seconds));
    }

    private IEnumerator WaitAndStartCountdown(int phase, int seconds)
    {
        yield return new WaitUntil(() =>
            FindFirstObjectByType<GamePlayUI>() != null &&
            FindFirstObjectByType<GamePlayUI>().isActiveAndEnabled &&
            FindFirstObjectByType<GamePlayUI>().gameObject.activeInHierarchy
        );

        yield return null; // 1프레임 더 대기 (UI 요소 초기화 타이밍 확보)

        var ui = FindFirstObjectByType<GamePlayUI>();
        ui?.StartCountdownUI(phase, seconds);
    }


    // 이벤트 실행 트리거 (서버+클라이언트 동기화)
    [ClientRpc]
    private void RpcTriggerEvent()
    {
        GamePlayUI?.UpdateCountdownUI(0, 2);

        if (!isServer)
        {
            GameSystemManager.Instance.StartEvent();
        }
    }

    // Phase2 타이머 시작 (서버 기준)
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