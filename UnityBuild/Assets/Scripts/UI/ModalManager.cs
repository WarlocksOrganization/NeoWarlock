using UnityEngine;
using System.Collections.Generic;

public class ModalManager : MonoBehaviour
{
    public List<GameObject> modals; // 등록된 모든 모달 리스트
    public GameObject menuModal; // 메뉴 모달

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapePress();
        }
    }

    void HandleEscapePress()
    {
        // 활성화된 모달이 있는지 확인
        bool anyModalOpen = false;
        for (int i = modals.Count - 1; i >= 0; i--)
        {
            if (modals[i].activeSelf) // 현재 활성화된 모달 찾기
            {
                modals[i].SetActive(false); // 모달 닫기
                anyModalOpen = true;
                return; // 가장 위에 있는 하나만 닫고 종료
            }
        }

        // 모든 모달이 닫혀 있다면 메뉴 모달을 토글
        if (!anyModalOpen && menuModal != null)
        {
            menuModal.SetActive(!menuModal.activeSelf);
        }
    }
}
