using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float baseFontSize = 1.0f; // 기본 폰트 크기
    [SerializeField] private float maxFontSize = 3.0f; // 최대 폰트 크기 제한

    public void SetDamageText(int damage)
    {
        // 🔹 데미지 값에 따라 색상 변경
        if (damage > 0)
            damageText.color = Color.yellow; // 데미지 (노란색)
        else if (damage < 0)
            damageText.color = Color.green; // 체력 회복 (초록색)
        else
            damageText.color = Color.white; // 0 데미지 (흰색)

        // 🔹 크기 조절 (로그 스케일 적용)
        float fontSizeScale = Mathf.Log(Mathf.Abs(damage) + 1, 10) + 1; // 로그 기반 크기 증가
        damageText.fontSize = Mathf.Min(baseFontSize * fontSizeScale, maxFontSize); // 최대 크기 제한
        float newfadeDuration = fadeDuration*fontSizeScale;

        damageText.text = Mathf.Abs(damage).ToString();
        StartCoroutine(FadeOutAndDestroy(newfadeDuration));
    }

    private IEnumerator FadeOutAndDestroy(float newfadeDuration)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        Vector3 startPosition = transform.position;

        float elapsedTime = 0f;
        while (elapsedTime < newfadeDuration)
        {
            transform.position = startPosition + new Vector3(0, elapsedTime * moveSpeed, 0);
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / newfadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}