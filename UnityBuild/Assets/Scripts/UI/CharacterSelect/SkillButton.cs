using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string skillName;
    private string skillDescription;
    private string upgradeSkillName;
    private string upgradeSkillDescription;
    private Coroutine fadeCoroutine;
    [SerializeField] private SkillDescriptionUI skillDescriptionUI;
    [SerializeField] private SkillDescriptionUI upgradeSkillDescriptionUI;

    [SerializeField] private Image skillImage;
    //[SerializeField] private Image upgradeSkillImage;
    [SerializeField] private Image selecttImage;

    public void SetUp(string name, string description, Sprite icon, string upgradeName, string upgradeDescription, Sprite upgradeIcon)
    {
        skillName = name;
        skillDescription = description;
        skillImage.sprite = icon;
        skillDescriptionUI.Setup(icon, skillName, skillDescription);

        if (upgradeName != "" && upgradeName != name)
        {
            upgradeSkillName = upgradeName;
            upgradeSkillDescription = upgradeDescription;
            upgradeSkillDescriptionUI.Setup(upgradeIcon ?? upgradeIcon, upgradeSkillName, upgradeSkillDescription);
        }
        else
        {
            upgradeSkillDescriptionUI.gameObject.SetActive(false);
        }


        gameObject.SetActive(true);
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutEffectCoroutine());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        skillDescriptionUI.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(upgradeSkillName) && upgradeSkillName != skillName) {
            upgradeSkillDescriptionUI.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (skillDescriptionUI != null)
        {
            skillDescriptionUI.gameObject.SetActive(false);
            upgradeSkillDescriptionUI.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeOutEffectCoroutine()
    {
        selecttImage.color = Color.white;

        float duration = 0.2f; // 페이드아웃 지속 시간 (1.5초)
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / duration);
            selecttImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        // 최종적으로 완전 투명하게 설정
        selecttImage.color = new Color(1, 1, 1, 0);
    }
}