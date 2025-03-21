    using System;
    using System.Collections;
    using DataSystem.Database;
    using GameManagement;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class PlayerCardSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text cardTypeText;
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Image cardIconImage;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardDetailText;
        [SerializeField] private Button reRollButton;

        [SerializeField] private Sprite healthIcon;
        [SerializeField] private Sprite speedIcon;

        [SerializeField] private Image cardIconFrame;
        [SerializeField] private Sprite blackIconFrame;
        [SerializeField] private Sprite blueIconFrame;
        [SerializeField] private Sprite purpleIconFrame;
        [SerializeField] private Sprite goldIconFrame;

        [SerializeField] private GameObject glowImage;
        private Coroutine glowCoroutine;

        private Database.PlayerCardData currentCard;
        private Database.AttackData[] SkillData;
        private PlayerCardUI playerCardUI;

        private void Awake()
        {
            playerCardUI = FindFirstObjectByType<PlayerCardUI>();
        
            reRollButton.gameObject.SetActive(true);
            reRollButton.onClick.RemoveAllListeners();
            reRollButton.onClick.AddListener(Reroll);
        }

        private void Initialize()
        {
            SkillData = new Database.AttackData[4];
            for (int i = 1; i < 4; i++)
            {
                SkillData[i] = Database.GetAttackData(PlayerSetting.AttackSkillIDs[i]);
            }
        }

        public void SetCardData(Database.PlayerCardData cardData)
        {
            Initialize();
            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
                glowCoroutine = null;
            }
            if (glowImage != null)
            {
                glowImage.SetActive(false);
                glowImage.transform.localScale = Vector3.one;
            }
            currentCard = cardData;
        
            cardIconImage.sprite = Database.GetCardIcon(cardData.StatType);

            // 카드 타입에 따른 처리
            switch (cardData.StatType)
            {
                //기본 스탯
                case PlayerStatType.Health:
                    cardTypeText.text = "스탯 강화";
                    skillIconImage.sprite = healthIcon;
                    cardIconFrame.sprite = blackIconFrame;
                    cardNameText.text = "최대 체력";
                    cardDetailText.text = ApplyColorToNumber($"체력 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                    break;

                case PlayerStatType.Speed:
                    cardTypeText.text = "스탯 강화";
                    skillIconImage.sprite = speedIcon;
                    cardIconFrame.sprite = blackIconFrame;
                    cardNameText.text = "이동 속도";
                    cardDetailText.text = ApplyColorToNumber($"이동 속도 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;
            
                //특정 스킬 강화
                case PlayerStatType.AttackSpeed:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"투사체 속도 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Range:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"최대 거리 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Radius:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"타격 범위 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Damage:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"데미지 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.KnockbackForce:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"넉백 거리 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Cooldown:
                    cardTypeText.text = "스킬 강화";
                    skillIconImage.sprite = SkillData[currentCard.AppliedSkillIndex].Icon;
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"쿨다운 -{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Special:
                    cardTypeText.text = $"<color=#FFD700>스킬 승급</color>";
                    skillIconImage.sprite = Database.GetAttackData(currentCard.AppliedSkillIndex+(int)PlayerSetting.PlayerCharacterClass*10+100).Icon;
                    cardIconFrame.sprite = goldIconFrame;
                    cardNameText.text = $"{SkillData[currentCard.AppliedSkillIndex].DisplayName}";
                    cardDetailText.text = ApplyColorAfterArrow($"{SkillData[currentCard.AppliedSkillIndex].DisplayName} \n->  " +
                                          $"{Database.GetAttackData(currentCard.AppliedSkillIndex+(int)PlayerSetting.PlayerCharacterClass*10+100).DisplayName}", "#FFD700");
                    glowImage.SetActive(true);
                
                    if (glowCoroutine != null)
                        StopCoroutine(glowCoroutine);
                    glowCoroutine = StartCoroutine(AnimateGlowScale());

                    break;

                default:
                    cardTypeText.text = "알 수 없는 카드";
                    cardDetailText.text = "효과 없음";

                    glowImage.SetActive(false);
                    if (glowCoroutine != null)
                    {
                        StopCoroutine(glowCoroutine);
                        glowCoroutine = null;
                    }

                    glowImage.transform.localScale = Vector3.one;
                    break;
            }
        }

        private IEnumerator AnimateGlowScale()
        {
            float time = 0f;
            float speed = 4f;
            Image glowImg = glowImage.GetComponent<Image>();
            Color baseColor = new Color(1f, 0.84f, 0f);
            Color pulseColor = new Color(1f, 0.55f, 0f);

            while (true)
            {

                float cycleTime = 0.8f;
                float pingPong = Mathf.PingPong(time, cycleTime) / cycleTime;
                float scale = Mathf.Lerp(1f, 1.5f, pingPong);
                float alpha = Mathf.Lerp(0.4f, 1f, pingPong);
                glowImage.transform.localScale = new Vector3(scale, scale, 1f);
                if (glowImg != null)
                    {
                        Color pulse = Color.Lerp(baseColor, pulseColor, pingPong);
                        glowImg.color = new Color(pulse.r, pulse.g, pulse.b, alpha);
                    }
                time += Time.deltaTime;
                yield return null;
            }
        }

    private string ApplyColorToNumber(string text, string pColor, string mColor)
        {
            string[] words = text.Split(' '); // 공백 기준으로 단어 분리
            if (words.Length < 2) return text; // 단어가 2개 미만이면 변경할 필요 없음

            string lastWord = words[words.Length - 1]; // 마지막 단어 (숫자 + %)

            if (lastWord.Contains("+")) // %가 포함된 경우 숫자로 판단
            {
                words[words.Length - 1] = $"<color={pColor}>{lastWord}</color>";
            }
            
            else if (lastWord.Contains("-")) // %가 포함된 경우 숫자로 판단
            {
                words[words.Length - 1] = $"<color={mColor}>{lastWord}</color>";
            }

        return string.Join(" ", words); // 다시 문자열로 합치기
        }

        private string ApplyColorAfterArrow(string text, string colorCode)
        {
            int arrowIndex = text.IndexOf("->"); // 화살표 위치 찾기
            if (arrowIndex == -1) return text; // 화살표가 없으면 원본 반환

            string beforeArrow = text.Substring(0, arrowIndex + 2); // "->" 포함 앞부분
            string afterArrow = text.Substring(arrowIndex + 2).Trim(); // 화살표 이후 텍스트

            return $"{beforeArrow} <color={colorCode}>{afterArrow}</color>"; // 색상 적용 후 결합
        }

        // ✅ 현재 카드 데이터를 반환
        public Database.PlayerCardData GetCurrentCard()
        {
            return currentCard;
        }

        private void Reroll()
        {
            if (playerCardUI.TryGetNewCard(out Database.PlayerCardData newCard))
            {
                reRollButton.gameObject.SetActive(false); // ✅ 버튼 비활성화
                StartCoroutine(CardRotation(newCard));
            }
        }

        private IEnumerator CardRotation(Database.PlayerCardData newCard)
        {
            float rotationPerStep = 30f;  // 한 스텝당 회전 각도 (30도씩 회전)
            int stepsPerRotation = 12;    // 한 바퀴(360도)를 몇 단계로 나눌 것인가 (30도씩 12번 = 360도)
            int totalRotations = 3;       // 몇 바퀴 회전할 것인가 (3바퀴)
            int currentStep = 0;          // 현재 회전 단계

            // **1단계: 빠르게 90도까지 회전 (카드 변경)**
            while (currentStep < 3)  // 30도씩 3번 → 90도
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + rotationPerStep, 0);
                currentStep++;
                yield return null;
            }

            // 정확히 90도 맞추고 카드 교체
            transform.rotation = Quaternion.Euler(0, 90, 0);
            SetCardData(newCard); // 새 카드 적용

            // **2단계: 빠르게 여러 번 추가 회전**
            int targetSteps = totalRotations * stepsPerRotation;  // 총 회전 스텝 수 (3바퀴 * 12스텝 = 36스텝)

            while (currentStep < targetSteps + 3)  // 기존 3스텝(90도) 포함해서 총 39스텝
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + rotationPerStep, 0);
                currentStep++;
                yield return null;
            }

            // **3단계: 최종적으로 0도에 정착**
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }