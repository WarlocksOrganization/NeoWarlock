using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mirror;

public class ToMainMenu : MonoBehaviour
{
    public Button changeSceneButton;

    void Start()
    {
        changeSceneButton.onClick.AddListener(DisconnectAndLoadMenu);
    }

    void DisconnectAndLoadMenu()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost(); // 서버와 클라이언트 모두 종료
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient(); // 클라이언트만 종료
        }
        Invoke(nameof(LoadMainMenu), 1f);  // 또는 Coroutine 사용 가능
    }

    void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

    //     Debug.Log("���� �޴� ��û. �ڵ� ���� ����");
    //     Debug.Log($"�� ���� ��û: {sceneName}");

    //     NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();

    //     if (networkManager != null)
    //     {
    //         Debug.Log("NetworkManager �߰ߵ�. ��Ʈ��ũ ���� ��...");

    //         if (NetworkServer.active && NetworkClient.isConnected)
    //         {
    //             Debug.Log("���� + Ŭ���̾�Ʈ ���� �� StopHost() ȣ��.");
    //             networkManager.StopHost(); // ȣ��Ʈ ���� (Ŭ���̾�Ʈ�� �ڵ� �����)
    //         }
    //         else if (NetworkServer.active)
    //         {
    //             Debug.Log("���� ���� �� StopServer() ȣ��.");
    //             networkManager.StopServer(); // ������ ����
    //         }
    //         else if (NetworkClient.isConnected)
    //         {
    //             Debug.Log("Ŭ���̾�Ʈ ���� �� StopClient() ȣ��.");
    //             networkManager.StopClient(); // Ŭ���̾�Ʈ�� ����
    //         }

    //         Debug.Log("��� �ڷ�ƾ ���� �� Mirror ��Ʈ��ũ ����.");
    //         StopAllCoroutines();  // ���� ���� �ڷ�ƾ�� �ִٸ� ����

    //         //Debug.Log("NetworkManager ���� ��...");
    //         //Destroy(networkManager.gameObject);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("NetworkManager�� ã�� �� �����ϴ�. �״�� �� ����.");
    //     }

    //     Debug.Log($"�� ��ȯ ����: {sceneName}");
    //     SceneManager.LoadScene(sceneName);
    // }