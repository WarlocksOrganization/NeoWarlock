using System.Collections;
using System.Collections.Generic;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillLogUI : MonoBehaviour
{
    [SerializeField] private GameObject killLogPrefab; // 킬 로그 프리팹
    [SerializeField] private Transform logContainer; // 킬 로그를 표시할 부모 오브젝트 (UI)
    [SerializeField] private Transform[] logPositions; // ✅ 다른 오브젝트의 자식으로 있는 UI 위치들
    [SerializeField] private float moveSpeed = 5f; // 로그 이동 속도

    private Queue<KillLogItem> logPool = new Queue<KillLogItem>(); // 사용할 수 있는 로그들 (재사용)
    private List<KillLogItem> activeLogs = new List<KillLogItem>(); // 현재 화면에 있는 로그들

    public static KillLogUI Instance;

    private void Awake()
    {
        Instance = this;
        InitializeLogPool();
    }

    // 🔹 킬 로그 아이템 미리 생성해서 Pool에 저장
    private void InitializeLogPool()
    {
        for (int i = 0; i < logPositions.Length; i++)
        {
            GameObject logInstance = Instantiate(killLogPrefab, logContainer);
            logInstance.transform.SetParent(logContainer, false); // ✅ 부모 설정 유지
            KillLogItem logItem = logInstance.GetComponent<KillLogItem>();
            logItem.gameObject.SetActive(false);
            logPool.Enqueue(logItem);
        }
    }

    // 🔹 킬 로그 추가
    public void AddKillLog(PlayerCharacter killer, PlayerCharacter victim, int skillId)
    {
        if (logPool.Count == 0) return; // 사용할 수 있는 로그 아이템이 없으면 리턴

        KillLogItem logItem = logPool.Dequeue();
        logItem.gameObject.SetActive(true);
        activeLogs.Add(logItem); // 사용 중인 로그 리스트에 추가

        logItem.SetKillLog(killer, victim, skillId);
        RepositionLogs(); // ✅ 모든 로그 재배치
    }

    // 🔹 킬 로그 반환 및 재정렬
    public void ReturnLogToPool(KillLogItem logItem)
    {
        if (activeLogs.Contains(logItem))
        {
            activeLogs.Remove(logItem);
        }

        logItem.gameObject.SetActive(false);
        logPool.Enqueue(logItem);

        RepositionLogs(); // ✅ 로그 재배치
    }

    // 🔹 로그들을 위치 배열에 맞게 부드럽게 재배치
    private void RepositionLogs()
    {
        for (int i = 0; i < activeLogs.Count; i++)
        {
            Vector3 worldPos = logPositions[i].position; // ✅ logPositions[i]의 World Position
            Vector3 localPos = logContainer.InverseTransformPoint(worldPos); // ✅ logContainer 기준 Local Position 변환
            StartCoroutine(MoveLogToPosition(activeLogs[i], localPos));
        }
    }

    // 🔹 로그를 부드럽게 이동하는 애니메이션
    private IEnumerator MoveLogToPosition(KillLogItem logItem, Vector3 targetPosition)
    {
        RectTransform logTransform = logItem.GetComponent<RectTransform>();
        Vector3 startPosition = logTransform.localPosition;
        float elapsedTime = 0f;
        float duration = 0.3f; // 이동 속도 조절

        while (elapsedTime < duration)
        {
            logTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime * moveSpeed;
            yield return null;
        }

        logTransform.localPosition = targetPosition;
    }
}
