using UnityEngine;

namespace UI
{
    public class GameStatusUI : MonoBehaviour
    {
        [SerializeField] private GameObject playerListPanel; // 플레이어 리스트 UI
        [SerializeField] private GameObject gameDetailPanel; // 상세 정보 UI

        private bool isDetailVisible = false;

        void Start()
        {
            // 시작 시 상세 정보 UI는 비활성화
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
