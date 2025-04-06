    using System;
    using System.Collections;
    using DataSystem;
    using DataSystem.Database;
    using GameManagement;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class PlayerCardSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text cardTypeText;
        [SerializeField] private SkillButton skillButton;
        [SerializeField] private Image cardIconImage;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardDetailText;
        [SerializeField] private Button reRollButton;

        [SerializeField] private Sprite healthIcon;
        [SerializeField] private Sprite speedIcon;
        [SerializeField] private Sprite attackIcon;

        [SerializeField] private Image cardIconFrame;
        [SerializeField] private Sprite blackIconFrame;
        [SerializeField] private Sprite blueIconFrame;
        [SerializeField] private Sprite purpleIconFrame;
        [SerializeField] private Sprite goldIconFrame;

        [SerializeField] private GameObject glowImage;
        [SerializeField] private Image explosionImage;
        private Coroutine glowCoroutine;

        private Database.PlayerCardData currentCard;
        private Database.AttackData[] SkillData;
        private PlayerCardUI playerCardUI;
        private Database.AttackData aD;

        private void Awake()
        {
            playerCardUI = FindFirstObjectByType<PlayerCardUI>();

            ResetSlot();
        }
        
        public void ResetSlot()
        {
            reRollButton.gameObject.SetActive(true);
            reRollButton.onClick.RemoveAllListeners();
            reRollButton.onClick.AddListener(Reroll);

            // ✅ 회전값 초기화 (회전 도중 꺼졌다가 다시 켜질 때 대비)
            transform.rotation = Quaternion.identity;

            // ✨ 이펙트 리셋
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

            if (explosionImage != null)
            {
                explosionImage.gameObject.SetActive(false);
                explosionImage.transform.localScale = Vector3.one;
                explosionImage.color = new Color(1f, 1f, 1f, 1f); // 불투명한 기본값
            }
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
                    skillButton.SetUp("최대 체력", "플레이어 캐릭터의 최대 체력입니다.", healthIcon);
                    cardIconFrame.sprite = blackIconFrame;
                    cardNameText.text = "최대 체력";
                    cardDetailText.text = ApplyColorToNumber($"체력 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                    break;

                case PlayerStatType.Speed:
                    cardTypeText.text = "스탯 강화";
                    skillButton.SetUp("이동 속도", "플레이어 캐릭터의 이동 속도입니다.", speedIcon);
                    cardIconFrame.sprite = blackIconFrame;
                    cardNameText.text = "이동 속도";
                    cardDetailText.text = ApplyColorToNumber($"이동 속도 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;
                
                case PlayerStatType.AttackPower:
                    cardTypeText.text = "스탯 강화";
                    skillButton.SetUp("공격력", "플레이어 캐릭터의 모든 스킬의 데미지에 적용되는 공격력입니다.", attackIcon);
                    cardIconFrame.sprite = blackIconFrame;
                    cardNameText.text = "공격력";
                    cardDetailText.text = ApplyColorToNumber($"공격력 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                    break;
            
                //특정 스킬 강화
                case PlayerStatType.AttackSpeed:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"투사체 속도 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Range:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"사거리 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Radius:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"타격 범위 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Damage:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = blueIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"데미지 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.KnockbackForce:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"넉백 거리 +{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Cooldown:
                    cardTypeText.text = "스킬 강화";
                    aD = Database.GetAttackData(currentCard.AppliedSkill); if (aD == null) return;
                    skillButton.SetUp(aD.DisplayName, aD.Description, aD.Icon);
                    cardIconFrame.sprite = purpleIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorToNumber($"쿨다운 -{cardData.BonusStat}%", "#FF3535", "#008CFF");
                break;

                case PlayerStatType.Special:
                    cardTypeText.text = $"<color=#FFD700>스킬 승급</color>";
                    aD = Database.GetAttackData(currentCard.AppliedSkill);
                    Database.AttackData ad2 = Database.GetAttackData(currentCard.AppliedSkill+100);
                    skillButton.SetUp(ad2.DisplayName, ad2.Description, ad2.Icon);
                    cardIconFrame.sprite = goldIconFrame;
                    cardNameText.text = $"{aD.DisplayName}";
                    cardDetailText.text = ApplyColorAfterArrow($"{aD.DisplayName} \n->  " +
                                          $"{ad2.DisplayName}", "#FFD700");
                                    glowImage.SetActive(true);

                    if (glowCoroutine != null)
                        StopCoroutine(glowCoroutine);

                    glowCoroutine = StartCoroutine(PlayGlowSequence());
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

    public IEnumerator PlayExplosionEffect(Image effectImage)
    {
        float duration = 0.4f;
        float time = 0f;

        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 endScale = Vector3.one * 1.8f;

        Color startColor = new Color(1f, 0.64f, 0f, 1f); // 주황색
        Color endColor = new Color(1f, 1f, 0.8f, 0f);    // 연한 노란색, 알파 0

        effectImage.gameObject.SetActive(true);
        effectImage.transform.localScale = startScale;
        effectImage.color = startColor;

        while (time < duration)
        {
            float t = time / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            effectImage.transform.localScale = Vector3.Lerp(startScale, endScale, eased);
            effectImage.color = Color.Lerp(startColor, endColor, eased);

            time += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        effectImage.gameObject.SetActive(false);
    }

    private IEnumerator PlayGlowSequence()
    {
        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_RerollSpecial);
        
        // 폭발 효과 먼저 재생
        Debug.Log("ExplodeEffect");
        yield return StartCoroutine(PlayExplosionEffect(explosionImage));

        // 그 다음 숨쉬기 glow 시작
        Debug.Log("GlowEffect");
        glowCoroutine = StartCoroutine(AnimateGlowScale());
    }

    private IEnumerator AnimateGlowScale()
        {
            float time = 0f;
            Image glowImg = glowImage.GetComponent<Image>();
            Color baseColor = new Color(1f, 0.84f, 0f);
            Color pulseColor = new Color(1f, 0.55f, 0f);

            while (true)
            {

                float cycleTime = 1.2f;
                float pingPong = Mathf.PingPong(time, cycleTime) / cycleTime;
                float scale = Mathf.Lerp(1.2f, 1.25f, pingPong);
                float alpha = Mathf.Lerp(0.5f, 1f, pingPong);
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
            string[] words = text.Split(' ');
            if (words.Length < 2) return text; // 단어가 2개 미만이면 변경할 필요 없음

            string lastWord = words[words.Length - 1]; // 마지막 단어 (숫자 + %)

            if (lastWord.Contains("+"))
            {
                words[words.Length - 1] = $"<color={pColor}>{lastWord}</color>";
            }
            
            else if (lastWord.Contains("-"))
            {
                words[words.Length - 1] = $"<color={mColor}>{lastWord}</color>";
            }

        return string.Join(" ", words);
        }

        private string ApplyColorAfterArrow(string text, string colorCode)
        {
            int arrowIndex = text.IndexOf("->");
            if (arrowIndex == -1) return text;

            string beforeArrow = text.Substring(0, arrowIndex + 2);
            string afterArrow = text.Substring(arrowIndex + 2).Trim(); // 화살표 이후 텍스트

            return $"{beforeArrow} <color={colorCode}>{afterArrow}</color>";
        }

        public Database.PlayerCardData GetCurrentCard()
        {
            return currentCard;
        }

        private void Reroll()
        {
            if (playerCardUI.TryGetNewCard(out Database.PlayerCardData newCard))
            {
                AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Reroll);
                
                reRollButton.gameObject.SetActive(false); // 리롤 버튼 비활성화
                StartCoroutine(CardRotation(newCard));
            }
        }

        private IEnumerator CardRotation(Database.PlayerCardData newCard)
        {
            float flipDuration = 0.15f;   // 앞면으로 가기까지 시간
            float spinDuration = 0.6f;   // 바뀐 뒤 전체 회전 시간
            float totalDuration = flipDuration + spinDuration;

            Quaternion startRot = transform.rotation;
            Quaternion midRot = Quaternion.Euler(0, 90f, 0);
            Quaternion endRot = Quaternion.Euler(0, 0, 0);

            // ▶ 1단계: 빠르게 반 바퀴 돌며 사라짐
            float elapsed = 0f;
            while (elapsed < flipDuration)
            {
                float t = elapsed / flipDuration;
                t = EaseInOut(t);
                transform.rotation = Quaternion.Slerp(startRot, midRot, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = midRot;
            SetCardData(newCard); // 카드 교체
            yield return new WaitForSeconds(0.05f); // 자연스러운 텀

            // ▶ 2단계: 여러 바퀴 돌면서 정면으로 정착
            float spinAngle = 360f * 3; // 3바퀴
            elapsed = 0f;
            while (elapsed < spinDuration)
            {
                float t = elapsed / spinDuration;
                t = EaseOutBack(t); // 살짝 튀어나오는 느낌의 easing

                float angle = Mathf.Lerp(90f, 0f, t) + spinAngle * (1 - t);
                transform.rotation = Quaternion.Euler(0, angle, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = endRot;
        }
        private float EaseInOut(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : -1f + (4f - 2f * t) * t;
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

    }