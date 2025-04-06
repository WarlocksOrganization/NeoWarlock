using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BuffSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Buff UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Image fillImage;

    [Header("Tooltip UI")]
    [SerializeField] private GameObject tooltipObject; // ✨ 툴팁 UI 오브젝트 직접 연결
    [SerializeField] private TMP_Text tooltipTextComponent; // ✨ 텍스트 컴포넌트 연결

    private float duration;
    private float remainingTime;
    private string tooltipText;

    public void Initialize(Sprite sprite, float time, string tooltip)
    {
        icon.sprite = sprite;
        duration = time;
        remainingTime = time;
        tooltipText = tooltip;

        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    void Update()
    {
        if (remainingTime <= 0)
        {
            Destroy(gameObject); // ⏱️ 시간이 다 되면 자동 제거
            return;
        }
        
        remainingTime -= Time.deltaTime;
        fillImage.fillAmount = 1f - Mathf.Clamp01(remainingTime / duration);
    }

    public bool IsExpired() => remainingTime <= 0;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipObject != null && tooltipTextComponent != null)
        {
            tooltipObject.SetActive(true);
            tooltipTextComponent.text = tooltipText;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }
}