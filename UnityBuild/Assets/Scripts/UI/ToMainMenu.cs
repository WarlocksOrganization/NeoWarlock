using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

public class ToMainMenu : MonoBehaviour
{
    public Button changeSceneButton;

    void Start()
    {
        changeSceneButton.onClick.AddListener(() => ChangeScene("MainMenu"));
    }

    void ChangeScene(string sceneName)
    {
        Debug.Log("메인 메뉴 요청. 코드 추후 적용");
        Debug.Log($"씬 변경 요청: {sceneName}");

        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();

        if (networkManager != null)
        {
            Debug.Log("NetworkManager 발견됨. 네트워크 종료 중...");

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                Debug.Log("서버 + 클라이언트 상태 → StopHost() 호출.");
                networkManager.StopHost(); // 호스트 종료 (클라이언트도 자동 종료됨)
            }
            else if (NetworkServer.active)
            {
                Debug.Log("서버 상태 → StopServer() 호출.");
                networkManager.StopServer(); // 서버만 종료
            }
            else if (NetworkClient.isConnected)
            {
                Debug.Log("클라이언트 상태 → StopClient() 호출.");
                networkManager.StopClient(); // 클라이언트만 종료
            }

            Debug.Log("모든 코루틴 정지 및 Mirror 네트워크 종료.");
            StopAllCoroutines();  // 실행 중인 코루틴이 있다면 정지

            //Debug.Log("NetworkManager 삭제 중...");
            //Destroy(networkManager.gameObject);
        }
        else
        {
            Debug.LogWarning("NetworkManager를 찾을 수 없습니다. 그대로 씬 변경.");
        }

        Debug.Log($"씬 전환 실행: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}