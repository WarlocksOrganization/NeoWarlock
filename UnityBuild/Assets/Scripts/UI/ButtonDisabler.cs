using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonDisabler : MonoBehaviour
{
    public void ButtonDisable(Button target)
    {
        // 버튼 비활성화 함수
        target.interactable = false;
        StartCoroutine(ButtonDisableCoroutine(target, 1.0f));
    } 

    private IEnumerator ButtonDisableCoroutine(Button target, float duration)
    {
        // 버튼 비활성화 코루틴
        target.interactable = false;
        yield return new WaitForSeconds(duration);
        target.interactable = true;
    }
}
