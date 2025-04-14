using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragonHPBar : MonoBehaviour
{
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text curHpText;
    [SerializeField] private TMP_Text maxHpText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider delayedSlider;
    
    private CanvasGroup canvasGroup;

    private Coroutine delayedRoutine;
    private Coroutine fadeOutRoutine;
    private float fadeDelay = 5f;
    private float fadeDuration = 1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void SetBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
    }

    public void UpdateHpBar(float newHp, float maxHp)
    {
        if (healthSlider == null || delayedSlider == null)
        {
            Debug.LogWarning("슬라이더가 연결되어 있지 않습니다.");
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;

            if (fadeOutRoutine != null)
                StopCoroutine(fadeOutRoutine);

            fadeOutRoutine = StartCoroutine(FadeOutAfterDelay());
        }

        if (curHpText != null) curHpText.text = ((int)newHp).ToString();
        if (maxHpText != null) maxHpText.text = ((int)maxHp).ToString();

        maxHp = Mathf.Max(maxHp, newHp);

        float newValue = newHp / maxHp;
        healthSlider.value = newValue;

        if (delayedRoutine != null)
            StopCoroutine(delayedRoutine);

        delayedRoutine = StartCoroutine(AnimateDelayedBar(delayedSlider.value, newValue));
    }

    private IEnumerator AnimateDelayedBar(float from, float to)
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            delayedSlider.value = Mathf.Lerp(from, to, t);
            yield return null;
        }

        delayedSlider.value = to;
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
        
    public void HideHpBar()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (delayedRoutine != null)
            StopCoroutine(delayedRoutine);

        if (fadeOutRoutine != null)
            StopCoroutine(fadeOutRoutine);
    }

}