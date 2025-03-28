using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;

public class GameSystemManager : MonoBehaviour
{
    public static GameSystemManager Instance;

    [SerializeField] private GameObject[] FallGrounds;
    private int eventnum = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // ✅ 중복된 Instance 제거
    }

    public void StartEvent()
    {
        if (FallGrounds == null || FallGrounds.Length == 0) return;
        //Debug.Log($"[GameSystemManager] StartEvent() {NetworkServer.active} {FallGrounds[eventnum] != null}");
        if (eventnum < FallGrounds.Length)
        {
            GameObject selectedGround = FallGrounds[eventnum];

            NetEvent();

            if (selectedGround != null && NetworkServer.active)
            {
                // 🔹 살아있는 플레이어 중 랜덤 타겟 선정
                var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
                    .Where(p => !p.isDead)
                    .ToList();
                Debug.Log("[GameSystemManager] StartEvent()");

                if (allPlayers.Count == 0) return;
                Debug.Log("[GameSystemManager] StartEvent()");

                var target = allPlayers[Random.Range(0, allPlayers.Count)];

                // 🔹 GameHand 생성 및 초기화
                Vector3 spawnPos = target.transform.position;
                spawnPos.y = 0;

                GameHand.Instance.Initialize();
                Debug.Log("[GameSystemManager] StartEvent()");
            }
            
            // 🔹 5초 뒤 지형 파괴 실행 (Coroutine 사용)
            StartCoroutine(DelayedFall(selectedGround, 4f));
            Debug.Log("[GameSystemManager] StartEvent()");
        }
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
    public void NetEvent()
    {
    // ✅ 다음 이벤트의 FallGround 자식 찾기 및 NextFall() 실행
        int nextEvent = eventnum ;
        if (nextEvent < FallGrounds.Length)
        {
            GameObject nextGround = FallGrounds[nextEvent];

            if (nextGround != null)
            {
                // ✅ 다음 FallGround의 모든 자식 오브젝트에 NextFall() 실행
                FallGround[] nextFallGrounds = nextGround.GetComponentsInChildren<FallGround>();

                if (nextFallGrounds.Length > 0)
                {
                    foreach (var nextFallGround in nextFallGrounds)
                    {
                        nextFallGround.NextFall();
                    }
                }
            }
        }
        eventnum++; // 다음 이벤트로 이동
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
