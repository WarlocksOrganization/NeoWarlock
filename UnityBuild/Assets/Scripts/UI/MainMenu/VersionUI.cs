using UnityEngine;

public class VersionUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.GetComponent<TMPro.TextMeshProUGUI>().text = "Ver. " + Application.version;
    }
}
