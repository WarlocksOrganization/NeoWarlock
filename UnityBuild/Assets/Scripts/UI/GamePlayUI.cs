using System.Collections;
using System.Linq;
using DataSystem;
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
    [SerializeField] private ScoreBoardUI scoreBoardUI;

    private int survivePlayers = -1;
    private Constants.GameState gameState = Constants.GameState.NotStarted;

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

    public void StartCountdownUI(int phase, int seconds)
    {
        StopAllCoroutines(); // 중복 방지
        StartCoroutine(UICountdownRoutine(phase, seconds));
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
        }
    }

    public void ShowFinalScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        scoreBoardUI.ShowScoreBoard(records, roundIndex);
    }

    public void ShowGameOverTextAndScore(Constants.PlayerRecord[] records, int roundIndex)
    {
        StartCoroutine(GameOverSequence(records, roundIndex));
    }

    private IEnumerator GameOverSequence(Constants.PlayerRecord[] records, int roundIndex)
    {
        AudioManager.Instance.ApplyBGMVolumeToMixer(0);
        countDownText.gameObject.SetActive(true);
        countDownText.color = Color.green;
        countDownText.text = "Game Over";
        countDownAnimator.SetTrigger("isGameOver");

        yield return new WaitForSeconds(3f);
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_ScoreBoard);
        ShowFinalScoreBoard(records, roundIndex);
    }

    public void ForceStartPhase2()
    {
        gameState = Constants.GameState.Start;
        UpdatePlayerInRoon();
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_GameStart);
    }

    public override void UpdateMapUI(Constants.RoomMapType type)
    {
        return;
    }
    
}
