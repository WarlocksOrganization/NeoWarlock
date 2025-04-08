using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using Player;
using TMPro;
using UnityEngine;

public class GamePlayUI : GameLobbyUI
{
    [SerializeField] private GameObject alamGameObject;
    [SerializeField] private TextMeshProUGUI alamText;
    [SerializeField] private TextMeshProUGUI countDownText;
    [SerializeField] private Animator countDownAnimator;
    [SerializeField] private GameObject StartCube;
    public ScoreBoardUI scoreBoardUI;

    private int survivePlayers = -1;
    private Constants.GameState gameState = Constants.GameState.NotStarted;
    
    [SerializeField] private Transform cardSlotParent; // HorizontalLayoutGroup 붙은 곳
    [SerializeField] private CardSlot cardSlotPrefab;
    private readonly List<CardSlot> activeCardSlots = new();

    private void Start()
    {
        alamGameObject.SetActive(false);
        playerStatusUI.ClosePanels();
    }

    public override void UpdatePlayerInRoon()
    {
        foundCharacters = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .OrderBy(player => player.playerId)
            .ToArray();

        int maxPlayerId = foundCharacters.Length > 0 ? foundCharacters.Max(p => p.playerId) : 0;
        PlayerCharacters = new GameObject[maxPlayerId + 1];

        foreach (var player in foundCharacters)
        {
            if (player.playerId >= 0 && player.playerId < PlayerCharacters.Length)
            {
                PlayerCharacters[player.playerId] = player.gameObject;
            }
        }

        survivePlayers = foundCharacters.Count(p => !p.isDead);
        PlayerInRoonText.text = $"남은 인원 : {survivePlayers} 명";

        playerStatusUI.Setup(foundCharacters, PlayerSetting.PlayerId);
    }

    public void CallGameStart()
    {
        if (gameState != Constants.GameState.NotStarted) return;

        gameState = Constants.GameState.Counting;

        // UI 준비
        playerStatusUI.OpenPanels();
        alamGameObject.SetActive(true);
        StartCube.SetActive(false);
    }

    private Coroutine countdownRoutine;

    public void StartCountdownUI(int phase, int seconds)
    {
        if (countDownText == null)
        {
            Debug.LogWarning("[GamePlayUI] countDownText가 아직 초기화되지 않았습니다.");
            return;
        }

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        countdownRoutine = StartCoroutine(UICountdownRoutine(phase, seconds));
    }

    private IEnumerator UICountdownRoutine(int phase, int seconds)
    {
        for (int t = seconds; t > 0; t--)
        {
            UpdateCountdownUI(t, phase);
            yield return new WaitForSeconds(1f);
        }

        UpdateCountdownUI(0, phase);

        if (phase == 1)
        {
            // ✅ 0 처리 후, 따로 딜레이 코루틴 실행
            StartCoroutine(DelayedHideCountdown());
        }
    }

    private IEnumerator DelayedHideCountdown()
    {
        yield return new WaitForSeconds(0.8f);
        countDownText.gameObject.SetActive(false);
    }

    public void UpdateCountdownUI(int time, int phase)
    {
        if (phase == 1)
        {
            if (gameState != Constants.GameState.Counting && time > 0) return;

            countDownText.gameObject.SetActive(true);

            if (time == 0)
            {
                AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Start);
                countDownText.text = "SMASH!";
                countDownAnimator.SetTrigger("isStart"); // ✅ 트리거 설정

                foreach (var player in foundCharacters)
                    player.SetState(Constants.PlayerState.Start);

                gameState = Constants.GameState.Start;
                UpdatePlayerInRoon();
               
                AudioManager.Instance.PlayBGM(GameSystemManager.Instance.mapConfig.bgmType);
            }
            else
            {
                countDownText.color = Color.yellow;
                countDownText.text = time.ToString();
                countDownAnimator.SetTrigger("isCounting"); // ✅ 숫자 카운트 트리거

                AudioManager.Instance.ApplyBGMVolumeToMixer(0);
                AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Count);
                foreach (var player in foundCharacters)
                    player.SetState(Constants.PlayerState.Counting);
            }
        }
        else if (phase == 2)
        {
            alamText.gameObject.SetActive(true);
            alamText.color = time <= 5 ? new Color(0.8f, 0, 0) : Color.white;
            alamText.text = $"{time}초";

            if (time == 0)
            {
                alamText.text = "";
            }
        }
    }
    
    public void ShowFinalScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        //Debug.Log("[GamePlayUI] ShowFinalScoreBoard 진입");
        // 먼저 활성화 후 기다리도록 수정
        scoreBoardUI.gameObject.SetActive(true);
        StartCoroutine(WaitAndShowScoreBoard(records, roundIndex));
    }

    private IEnumerator WaitAndShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        // scoreBoardUI가 완전히 준비될 때까지 기다림
        yield return new WaitUntil(() => scoreBoardUI != null); // 또는 바로 WaitForEndOfFrame도 가능

        yield return new WaitForSeconds(0.05f); // 렌더 타이밍 안정화용

        //Debug.Log("[GamePlayUI] WaitAndShowScoreBoard 진입");
        scoreBoardUI.ShowScoreBoard(records, roundIndex);
    }

    public void ShowGameOverTextAndScore(Constants.PlayerRecord[] records, int roundIndex, Action onComplete = null)
    {
        StartCoroutine(GameOverSequence(records, roundIndex, onComplete));
    }

    private IEnumerator GameOverSequence(Constants.PlayerRecord[] records, int roundIndex, Action onComplete)
    {
        //Debug.Log("[GamePlayUI] GameOverSequence 진입");

        AudioManager.Instance.ApplyBGMVolumeToMixer(0);
        countDownText.gameObject.SetActive(true);
        countDownText.color = Color.green;
        countDownText.text = "Game Over";
        countDownAnimator.SetTrigger("isGameOver");
        
        StartCube.SetActive(true);

        yield return new WaitForSeconds(3f);

        countDownText.text = "";
    
        //Debug.Log("[GamePlayUI] GameOverSequence2 진입");
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_ScoreBoard);
        ShowFinalScoreBoard(records, roundIndex);

        // ⏳ 스코어보드 노출 시간
        yield return new WaitForSeconds(Constants.ScoreBoardTime);

        // ✅ 다음 라운드를 위해 상태 초기화
        gameState = Constants.GameState.NotStarted;

        onComplete?.Invoke(); // ✅ 완료 콜백
    }
    
    public void ShowCards(List<Database.PlayerCardData> cards)
    {
        // 기존 카드 제거
        foreach (var slot in activeCardSlots)
            Destroy(slot.gameObject);
        activeCardSlots.Clear();

        foreach (var card in cards)
        {
            var slot = Instantiate(cardSlotPrefab, cardSlotParent);
            slot.Init(card);
            activeCardSlots.Add(slot);
        }
    }
}
