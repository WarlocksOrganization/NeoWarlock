using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class CCUDisplay : MonoBehaviour
{
    public Transform listContainer;
    public GameObject userItemPrefab;

    private enum SortType { None, Nickname, Status }
    private SortType currentSortType = SortType.None;
    private bool isAscending = true;

    [SerializeField] private RectTransform nicknameArrow;
    [SerializeField] private RectTransform statusArrow;
    [SerializeField] private TMP_Text userCount;

    private List<GameObject> userItems = new List<GameObject>();
    private List<UserInfo> currentUsers = new List<UserInfo>();

    public void UpdateCCUDisplay(List<UserInfo> users)
    {  
        currentUsers = users; // 원본 저장
        SortAndRefresh(); 

    }

    private void SortAndRefresh()
    {
        var sorted = currentUsers;

        switch (currentSortType)
        {
            case SortType.Nickname:
                sorted = isAscending 
                    ? currentUsers.OrderBy(u => u.nickName).ToList()
                    : currentUsers.OrderByDescending(u => u.nickName).ToList();
                break;
            case SortType.Status:
                sorted = isAscending
                    ? currentUsers.OrderBy(u => u.status).ToList()
                    : currentUsers.OrderByDescending(u => u.status).ToList();
                break;
        }

        userCount.text = $"현재 접속자 수: {sorted.Count}";

        while (userItems.Count < sorted.Count)
        {
            var item = Instantiate(userItemPrefab, listContainer);
            item.SetActive(true);
            userItems.Add(item);
        }

        while (userItems.Count > sorted.Count)
        {
            var item = userItems.Last();
            userItems.Remove(item);
            Destroy(item);
        }

        int idx = 0;
        foreach (var user in sorted)
        {
            var item = userItems[idx++];
            var nicknameText = item.transform.Find("Nickname")?.GetComponent<TextMeshProUGUI>();
            var statusText = item.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

            if (nicknameText != null) nicknameText.text = user.nickName;
            if (statusText != null) statusText.text = user.status;
        }
    }

    public void OnSortByNickname()
    {
        ToggleSort(SortType.Nickname);
    }

    public void OnSortByStatus()
    {
        ToggleSort(SortType.Status);
    }

    private void ToggleSort(SortType sortType)
    {
        if (currentSortType == sortType)
        {
            isAscending = !isAscending;  // 같은 기준 눌렀을 때 토글
        }
        else
        {
            currentSortType = sortType;
            isAscending = true; // 새 기준 누르면 초기값 오름차순
        }
        RotateArrows();
        SortAndRefresh();
    }
    private void RotateArrows()
    {
        nicknameArrow.localRotation = Quaternion.Euler(0, 0, 90);  // 초기화
        statusArrow.localRotation = Quaternion.Euler(0, 0, 90);    // 초기화

        RectTransform targetArrow = null;

        if (currentSortType == SortType.Nickname)
            targetArrow = nicknameArrow;
        else if (currentSortType == SortType.Status)
            targetArrow = statusArrow;

        if (targetArrow != null)
        {
            targetArrow.localRotation = Quaternion.Euler(0, 0, isAscending ? 90 : 270);
        }
    }

//     private void SortAndRefresh()
//         DebugUI.Instance?.Log($"[UI] 렌더링 시작 - {users.Count}명");
//         userCount.text = $"현재 접속자 수: {users.Count}";
//         // foreach (Transform child in listContainer)
//         // {
//         //     Destroy(child.gameObject);
//         // }

//         while (userItems.Count < users.Count)
//         {
//             var item = Instantiate(userItemPrefab, listContainer);
//             item.SetActive(true);
//             userItems.Add(item);
//         }

//         while (userItems.Count > users.Count)
//         {
//             var item = userItems.Last();
//             userItems.Remove(item);
//             Destroy(item);
//         }

//         int idx = 0;
//         foreach (var user in users)
//         {
//             var item = userItems[idx++];

//             var nicknameText = item.transform.Find("Nickname")?.GetComponent<TextMeshProUGUI>();
//             var statusText = item.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

//             if (nicknameText == null) DebugUI.Instance?.Log("[UI] Nickname 없음");
//             else nicknameText.text = user.nickName;

//             if (statusText == null) DebugUI.Instance?.Log("[UI] Status 없음");
//             else statusText.text = user.status;
//         }
//     }

//    