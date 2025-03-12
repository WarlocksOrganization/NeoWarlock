using UnityEngine;

namespace UI
{
    public class GameStatusUI : MonoBehaviour
    {
        [SerializeField] private GameObject playerListPanel; // �÷��̾� ����Ʈ UI
        [SerializeField] private GameObject gameDetailPanel; // �� ���� UI

        private bool isDetailVisible = false;

        void Start()
        {
            // ���� �� �� ���� UI�� ��Ȱ��ȭ
            gameDetailPanel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleDetailPanel();
            }
        }

        private void ToggleDetailPanel()
        {
            isDetailVisible = !isDetailVisible;
            gameDetailPanel.SetActive(isDetailVisible);
            playerListPanel.SetActive(!isDetailVisible);
        }
    }
}
