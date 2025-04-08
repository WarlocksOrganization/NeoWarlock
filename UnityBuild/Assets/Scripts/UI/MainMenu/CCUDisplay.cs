using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CCUDisplay : MonoBehaviour
{
    public Transform listContainer;
    public GameObject userItemPrefab;

    [SerializeField] private TMP_Text userCount;

    public void UpdateCCUDisplay(List<UserInfo> users)
    {   
        DebugUI.Instance?.Log($"[UI] 렌더링 시작 - {users.Count}명");
        userCount.text = $"현재 접속자 수: {users.Count}";
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var user in users)
        {
            var item = Instantiate(userItemPrefab, listContainer);
            item.SetActive(true);

            var nicknameText = item.transform.Find("Nickname")?.GetComponent<TextMeshProUGUI>();
            var statusText = item.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

            if (nicknameText == null) DebugUI.Instance?.Log("[UI] Nickname 없음");
            else nicknameText.text = user.nickName;

            if (statusText == null) DebugUI.Instance?.Log("[UI] Status 없음");
            else statusText.text = user.status;
        }
    }
}
