using UnityEngine;
using System.Collections.Generic;

public class ModalManager : MonoBehaviour
{
    public List<GameObject> modals; // ��ϵ� ��� ��� ����Ʈ
    public GameObject menuModal; // �޴� ���

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapePress();
        }
    }

    void HandleEscapePress()
    {
        // Ȱ��ȭ�� ����� �ִ��� Ȯ��
        bool anyModalOpen = false;
        for (int i = modals.Count - 1; i >= 0; i--)
        {
            if (modals[i].activeSelf) // ���� Ȱ��ȭ�� ��� ã��
            {
                modals[i].SetActive(false); // ��� �ݱ�
                anyModalOpen = true;
                return; // ���� ���� �ִ� �ϳ��� �ݰ� ����
            }
        }

        // ��� ����� ���� �ִٸ� �޴� ����� ���
        if (!anyModalOpen && menuModal != null)
        {
            menuModal.SetActive(!menuModal.activeSelf);
        }
    }
}
