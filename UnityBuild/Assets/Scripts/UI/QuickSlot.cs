using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    public class QuickSlot : MonoBehaviour
    {
        [SerializeField] private Image skinImage;
        [SerializeField] private Image skillCulImage;
        [SerializeField] private Image skillCulImage2;
        [SerializeField] private Image skillCulImage3; // 페이드아웃 효과를 줄 이미지
        [SerializeField] private GameObject isSelectedImage;
        [SerializeField] private TMP_Text culText;
        [SerializeField] private Image frameImage; // 스킬버튼 프레임 이미지

        private float maxSkillCul;
        private float currentSkillCul;
        private Coroutine fadeCoroutine;

        public void SetQuickSlotData(Sprite icon, float cooldown)
        {
            skinImage.sprite = icon;
            maxSkillCul = cooldown;
        }

        public void SelectSkill(bool isSelected)
        {
            isSelectedImage.SetActive(isSelected);
        }

        public void UseSkill()
        {
            currentSkillCul = maxSkillCul;
            skillCulImage2.fillAmount = 1;
            SelectSkill(false);
        }

        public void SetFrame(Sprite newFrame)
        {
            if (frameImage != null && newFrame != null)
            {
                frameImage.sprite = newFrame;
            }
        }

        void Update()
        {
            if (currentSkillCul <= 0) return;

            currentSkillCul -= Time.deltaTime;
            if (currentSkillCul > 1)
            {
                culText.text = ((int)currentSkillCul).ToString();
            }
            else
            {
                culText.text = (Mathf.Floor(currentSkillCul * 10f) / 10f).ToString();
            }
            skillCulImage.fillAmount = Mathf.Max(0f, currentSkillCul / maxSkillCul);
            
            if (currentSkillCul <= 0)
            {
                culText.text = "";
                skillCulImage2.fillAmount = 0;

                // 기존 페이드 코루틴이 실행 중이라면 중지하고 새로 시작
                if (fadeCoroutine != null)
                    StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeOutEffectCoroutine());
            }
        }

        private IEnumerator FadeOutEffectCoroutine()
        {
            skillCulImage3.color = Color.white;

            float duration = 0.2f; // 페이드아웃 지속 시간 (1.5초)
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, elapsed / duration);
                skillCulImage3.color = new Color(1,1, 1, alpha);
                yield return null;
            }

            // 최종적으로 완전 투명하게 설정
            skillCulImage3.color = new Color(1, 1, 1, 0);
        }
    }
}
