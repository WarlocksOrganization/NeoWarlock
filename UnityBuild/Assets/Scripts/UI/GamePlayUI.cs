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
        for (int t = seconds; t >= 0; t--)
        {
            UpdateCountdownUI(t, phase);
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateCountdownUI(int time, int phase)
    {
        if (phase == 1) // Phase1: 게임 시작 전 카운트다운
        {
            if (gameState != Constants.GameState.Counting) return;

            if (time == 0)
            {
                countDownText.text = "SMASH!";
                foreach (var player in foundCharacters)
                    player.SetState(Constants.PlayerState.Start);

                gameState = Constants.GameState.Start;
                UpdatePlayerInRoon();
                StartCoroutine(HideAfterDelay(countDownText.gameObject, 0.8f));

                AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_GameStart);
            }
            else
            {
                AudioManager.Instance.StopBGM();
                foreach (var player in foundCharacters)
                    player.SetState(Constants.PlayerState.Counting);

                countDownText.gameObject.SetActive(true);
                countDownText.text = time.ToString();
                StartCoroutine(PunchScale(countDownText.transform));
            }
        }
        else if (phase == 2) // Phase2: 이벤트 타이머
        {
            alamText.gameObject.SetActive(true);
            alamText.color = time <= 5 ? new Color(0.8f, 0, 0) : Color.white;
            alamText.text = $"{time}초";
        }
    }

    IEnumerator PunchScale(Transform target, float upScale = 1.1f, float downScale = 0.5f, float totalDuration = 0.3f)
    {
        Vector3 original = Vector3.one;
        Vector3 overshoot = original * upScale;
        Vector3 undershoot = original * downScale;
        float halfDuration = totalDuration / 2f;
        float t = 0f;

        while (t < halfDuration)
        {
            float ratio = t / halfDuration;
            target.localScale = Vector3.Lerp(original, overshoot, Mathf.SmoothStep(0f, 1f, ratio));
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            float ratio = t / halfDuration;
            target.localScale = Vector3.Lerp(overshoot, undershoot, Mathf.SmoothStep(0f, 1f, ratio));
            t += Time.deltaTime;
            yield return null;
        }

        target.localScale = undershoot;
    }

    IEnumerator HideAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        target.SetActive(false);
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
        countDownText.gameObject.SetActive(true);
        countDownText.color = Color.green;
        countDownText.text = "Game Over";
        StartCoroutine(PunchScale(countDownText.transform));

        yield return new WaitForSeconds(3f);
        ShowFinalScoreBoard(records, roundIndex);
    }

    public void ForceStartPhase2()
    {
        gameState = Constants.GameState.Start;
        UpdatePlayerInRoon();
        AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_SSAFY_GameStart);
    }
    
    
}
