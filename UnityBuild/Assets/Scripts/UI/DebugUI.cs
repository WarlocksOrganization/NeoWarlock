using UnityEngine;
using TMPro;

public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance;

    [SerializeField] private TextMeshProUGUI debugText;
    private string logBuffer = "";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (DebugUI.Instance != null)
            DebugUI.Instance.Log("testlog");
        else
            Debug.LogError("[OnlineUI] DebugUI.Instance is null");
    }
    public void Log(string message)
    {
        Debug.Log($"[DebugUI] Log 호출됨: {message}");  // 추가
        
        logBuffer += message + "\n";

        if (logBuffer.Length > 1000)
            logBuffer = logBuffer.Substring(logBuffer.Length - 1000);

        if (debugText != null)
            debugText.text = logBuffer; // ← 직접 갱신
    }

}
