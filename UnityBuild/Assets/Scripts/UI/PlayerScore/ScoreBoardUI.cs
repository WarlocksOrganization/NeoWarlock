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
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    
    [SerializeField] private GameObject scorePanelPrefab;
    [SerializeField] private Transform scorePanelParent;

    private List<PlayerScorePanel> scorePanels = new();
    
    [SerializeField] private GameObject rankPanelPrefab;
    [SerializeField] private Transform rankPanelParent;
    private List<RectTransform> panelPositions = new();

    [SerializeField] private GameObject teamResult;
    [SerializeField] private TMP_Text teamResultText;


    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button resultToggleButton;
    [SerializeField] private TMP_Text toggleButtonText;

    [SerializeField] private TMP_Text bestPlayerName;
    [SerializeField] private TMP_Text bestPlayerStat;
    [SerializeField] private Image bestPlayerIcon;
    [SerializeField] private TMP_Text bestKillName;
    [SerializeField] private TMP_Text bestKillStat;
    [SerializeField] private Image bestKillIcon;
    [SerializeField] private TMP_Text bestDamageName;
    [SerializeField] private TMP_Text bestDamageStat;
    [SerializeField] private Image bestDamageIcon;

    [SerializeField] private GameObject playerCharacterPrefab;
    [SerializeField] private Transform bestSpawnPoint;
    [SerializeField] private Camera bestPlayerCamera;
    [SerializeField] private Vector3 cameraOffset = new(-0.8f, 0.9f, -2.4f);
    [SerializeField] private Vector3 cameraLookOffset = new(0f, 1.8f, 0f);

    private readonly Dictionary<Constants.CharacterClass, (string folder, string prefix)> classAnimMap =
        new()
        {
            { Constants.CharacterClass.Necromancer, ("Necro", "N") },
            { Constants.CharacterClass.Warrior, ("Warrior", "W") },
            { Constants.CharacterClass.Mage, ("Magician", "M") },
            { Constants.CharacterClass.Archer, ("Archer", "A") },
            { Constants.CharacterClass.Priest, ("Priest", "P") }
        };

    private PlayableGraph? currentGraph;

    private bool isBestPlayerSpawned;
    

    private void Awake()
    {
        returnToLobbyButton.gameObject.SetActive(false);
        resultToggleButton.gameObject.SetActive(false);
        returnToLobbyButton.onClick.AddListener(OnClickReturnToLobby);
    }

    private void Start()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void ShowReturnToLobbyButton()
    {
        returnToLobbyButton.gameObject.SetActive(true);
        resultToggleButton.gameObject.SetActive(true);

        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        var delay = 20f;
        yield return new WaitForSeconds(delay);

        // ✅ 버튼을 누르지 않았을 경우에만 자동 복귀 실행
        if (returnToLobbyButton.gameObject.activeSelf)
        {
            Debug.Log("[ScoreBoardUI] 201초가 지나 자동으로 로비로 돌아갑니다.");
            OnClickReturnToLobby();
        }
    }
    
    private void CreateRankPanels(int count)
    {
        // 기존 것 제거
        foreach (Transform child in rankPanelParent)
            Destroy(child.gameObject);
        panelPositions.Clear();

        for (int i = 0; i < count; i++)
        {
            var rankGO = Instantiate(rankPanelPrefab, rankPanelParent);
            var rect = rankGO.GetComponent<RectTransform>();
            var rank = rankGO.GetComponent<RankPanel>();

            rank?.Init(i + 1); // 1등부터
            panelPositions.Add(rect);
        }
    }
    
    private void CreateScorePanels(Constants.PlayerRecord[] records)
    {
        // 기존 것 제거
        foreach (Transform child in scorePanelParent)
            Destroy(child.gameObject);

        scorePanels.Clear();

        foreach (var record in records.OrderBy(r => r.playerId))
        {
            var panelGO = Instantiate(scorePanelPrefab, scorePanelParent);
            var panel = panelGO.GetComponent<PlayerScorePanel>();

            panel.id = record.playerId;
            scorePanels.Add(panel);
        }
    }

    public void ShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        teamResult.gameObject.SetActive(false);
        gameObject.SetActive(true);
        CreateRankPanels(records.Length);
        CreateScorePanels(records);
        
        scorePanelParent.gameObject.SetActive(false);

        // 🔸 여기서 바로 시작하지 않고 한 프레임 대기
        StartCoroutine(DelayedRankingFlow(records, roundIndex));
    }
    
    private IEnumerator DelayedRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        yield return null; // 🔸 한 프레임 대기해서 레이아웃 정렬 완료를 기다림
        yield return new WaitForEndOfFrame(); // 필요하다면 추가로 대기
        
        scorePanelParent.gameObject.SetActive(true);

        StartCoroutine(ShowRankingFlow(records, roundIndex));
    }


    private IEnumerator ShowRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        // 1. 현재 라운드 결과 표시
        roundText.text = $"{roundIndex + 1} 라운드 결과";

        var roundSorted = records
            .OrderByDescending(r => GameManager.Instance.GetScoreAtRound(r, roundIndex))
            .ThenByDescending(r =>
                r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills) // 🔥 킬수 포함
            .ThenBy(r => r.playerId) // 🔥 아이디 순서
            .ToList();

        for (var i = 0; i < roundSorted.Count; i++)
        {
            var record = roundSorted[i];
            var stats = record.roundStatsList[roundIndex];

            var panel = scorePanels[record.playerId];
            panel.gameObject.SetActive(true);

            var localPlayerId = PlayerSetting.PlayerId;

            panel.SetupWithScore(record, GameManager.Instance.GetScoreAtRound(record, roundIndex), roundIndex, false,
                localPlayerId);
            panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f));

        // 2. 누적 점수 (반영 전)
        roundText.text = $"{roundIndex + 1} 라운드 통계";

        var preSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex - 1))
            .ThenByDescending(r =>
                r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills) // 🔥 킬수 포함
            .ThenBy(r => r.playerId) // 🔥 아이디 순서
            .ToList();

        for (var i = 0; i < preSorted.Count; i++)
        {
            var panel = scorePanels[preSorted[i].playerId];
            var preScore = preSorted[i].GetTotalScoreUpToRound(roundIndex - 1);
            var localPlayerId = PlayerSetting.PlayerId;
            panel.SetupWithScore(preSorted[i], preScore, roundIndex - 1, true, localPlayerId);
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }
        
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        bool isTeamMode = gameRoomData != null && gameRoomData.roomType == Constants.RoomType.Team;

        if (isTeamMode)
        {
            // 🔷 팀 점수 계산
            int teamAScoreBefore = preSorted
                .Where(p => p.team == Constants.TeamType.TeamA)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex - 1));

            int teamBScoreBefore = preSorted
                .Where(p => p.team == Constants.TeamType.TeamB)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex - 1));

            // 🔷 팀 점수 UI 표시
            if (teamResultText != null)
            {
                string colorA = ColorToHex(1f, 0.3f, 0.3f); // 밝은 붉은색
                string colorB = ColorToHex(0.3f, 0.4f, 1f); // 파란빛
                
                string result = $"<color=#{colorA}>Team A</color> : {teamAScoreBefore}점 vs <color=#{colorB}>Team B</color> : {teamBScoreBefore}점";
                teamResultText.text = result;
                teamResult.gameObject.SetActive(true);
            }
        }

        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f));
        yield return new WaitForSeconds(1f);

        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_ScoreCount);

        var finalSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex))
            .ThenByDescending(r =>
                r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills) // 🔥 킬수 포함
            .ThenBy(r => r.playerId) // 🔥 아이디 순서
            .ToList();

        for (var i = 0; i < finalSorted.Count; i++)
        {
            var panel = scorePanels.First(p => p.id == finalSorted[i].playerId);
            var before = preSorted.First(p => p.playerId == finalSorted[i].playerId)
                .GetTotalScoreUpToRound(roundIndex - 1);
            var after = finalSorted[i].GetTotalScoreUpToRound(roundIndex);
            var localPlayerId = PlayerSetting.PlayerId;
            panel.SetupWithScore(finalSorted[i], after, roundIndex, true, localPlayerId);
            panel.AnimateScore(before, after);
            panel.MoveTo(panelPositions[i].anchoredPosition);

            // ✅ 데미지 애니메이션 추가
            var prevDamage = 0;
            for (var r = 0; r <= roundIndex - 1; r++)
                if (r < finalSorted[i].roundStatsList.Count)
                    prevDamage += finalSorted[i].roundStatsList[r].damageDone;

            var finalDamage = prevDamage;
            if (roundIndex < finalSorted[i].roundStatsList.Count)
                finalDamage += finalSorted[i].roundStatsList[roundIndex].damageDone;

            panel.AnimateDamage(prevDamage, finalDamage);
        }
        
        if (isTeamMode)
        {
            int teamAScoreBefore = preSorted
                .Where(p => p.team == Constants.TeamType.TeamA)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex - 1));

            int teamBScoreBefore = preSorted
                .Where(p => p.team == Constants.TeamType.TeamB)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex - 1));

            int teamAScoreFinal = finalSorted
                .Where(p => p.team == Constants.TeamType.TeamA)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex));

            int teamBScoreFinal = finalSorted
                .Where(p => p.team == Constants.TeamType.TeamB)
                .Sum(p => p.GetTotalScoreUpToRound(roundIndex));

            teamResult.gameObject.SetActive(true);
            StartCoroutine(AnimateTeamScore(teamAScoreBefore, teamAScoreFinal, teamBScoreBefore, teamBScoreFinal));
        }

        var totalRounds = 3;
        if (roundIndex >= totalRounds - 1)
        {
            yield return new WaitForSeconds(3f); // 연출 간격

            var ResultBoard = transform.Find("ResultBoard").gameObject;
            var ScoreBoard = transform.Find("ScoreBoard").gameObject;
            var ToggleButton = transform.Find("ToggleButton").GetComponent<Button>();
            var LobbyButton = transform.Find("LobbyButton").GetComponent<Button>();


            gameObject.SetActive(true);
            Constants.PlayerRecord bestPlayer;
            ResultBoard.SetActive(true);
            SetResultBoardData(records, out bestPlayer);
            StartCoroutine(PopIn(ResultBoard.transform));
            showBestPlayer(bestPlayer);

            ScoreBoard.SetActive(false);
            ToggleButton.gameObject.SetActive(true);
            LobbyButton.gameObject.SetActive(true);

            ToggleButton.onClick.RemoveAllListeners();
            ToggleButton.onClick.AddListener(() =>
            {
                var showResult = !ResultBoard.activeSelf;
                ResultBoard.SetActive(showResult);
                ScoreBoard.SetActive(!showResult);
                toggleButtonText.text = showResult ? "상세 보기" : "결과 보기";
            });
            LobbyButton.onClick.AddListener(() => OnClickReturnToLobby());
        }
    }
    
    private IEnumerator AnimateTeamScore(int fromA, int toA, int fromB, int toB, float duration = 1f)
    {
        string colorA = ColorToHex(1f, 0.3f, 0.3f); // 밝은 붉은색
        string colorB = ColorToHex(0.3f, 0.4f, 1f); // 파란빛
        
        float time = 0f;
        while (time < duration)
        {
            float t = time / duration;
            int scoreA = Mathf.FloorToInt(Mathf.Lerp(fromA, toA, t));
            int scoreB = Mathf.FloorToInt(Mathf.Lerp(fromB, toB, t));

            teamResultText.text = $"<color=#{colorA}>Team A</color> : {scoreA}점 vs <color=#{colorB}>Team B</color> : {scoreB}점";
            time += Time.deltaTime;
            yield return null;
        }

        // 마지막 값 보정
        teamResultText.text = $"<color=#{colorA}>Team A</color> : {toA}점 vs <color=#{colorB}>Team B</color> : {toB}점";
    }
    
    private string ColorToHex(float r, float g, float b)
    {
        Color color = new Color(r, g, b);
        return ColorUtility.ToHtmlStringRGB(color); // 예: "FF4C4C"
    }

    private void SetResultBoardData(Constants.PlayerRecord[] records, out Constants.PlayerRecord bestPlayer)
    {
        Debug.Log("[ResultBoard] SetResultBoardData 진입");

        bestPlayer = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(GameManager.Instance.currentRound - 1))
            .FirstOrDefault();

        var bestKill = records
            .OrderByDescending(r => r.roundStatsList.Sum(rs => rs.kills + rs.outKills)) // 1차 기준: 킬 수
            .ThenByDescending(r => r.GetTotalScoreUpToRound(GameManager.Instance.currentRound - 1)) // 2차 기준: 점수
            .First();

        var bestDamage = records
            .OrderByDescending(r => r.roundStatsList.Sum(rs => rs.damageDone))
            .FirstOrDefault();

        Debug.Log(
            $"[ResultBoard] bestPlayer = {bestPlayer?.nickname}, bestKill = {bestKill?.nickname}, bestDamage = {bestDamage?.nickname}");

        if (bestPlayer == null || bestKill == null || bestDamage == null)
        {
            Debug.LogError("❗ GetTopXXXPlayer 중 null 반환됨");
            return;
        }

        var myId = PlayerSetting.PlayerId;

        string ColorizeName(string name, int id)
        {
            return id == myId ? $"<color=yellow>{name}</color>" : name;
        }

        bestPlayerName.text = ColorizeName(bestPlayer.nickname, bestPlayer.playerId);
        bestPlayerStat.text = $"점수 합계 : {bestPlayer.GetTotalScoreUpToRound(2)}";
        bestPlayerIcon.sprite = Database.GetCharacterClassData(bestPlayer.characterClass).CharacterIcon;

        bestKillName.text = ColorizeName(bestKill.nickname, bestKill.playerId);
        var bestKillCounts = bestKill.roundStatsList.Sum(rs => rs.kills + rs.outKills);
        bestKillStat.text = $"누적 처치 : {bestKillCounts}";
        bestKillIcon.sprite = Database.GetCharacterClassData(bestKill.characterClass).CharacterIcon;

        bestDamageName.text = ColorizeName(bestDamage.nickname, bestDamage.playerId);
        var bestDamageDone = bestDamage.roundStatsList.Sum(rs => rs.damageDone);
        bestDamageStat.text = $"누적 데미지 : {bestDamageDone}";
        bestDamageIcon.sprite = Database.GetCharacterClassData(bestDamage.characterClass).CharacterIcon;
    }

    private void showBestPlayer(Constants.PlayerRecord bestPlayer)
    {
        if (isBestPlayerSpawned) return;

        isBestPlayerSpawned = true;

        var bestPlayerInstance = Instantiate(playerCharacterPrefab, bestSpawnPoint.position, Quaternion.identity);

        var pc = bestPlayerInstance.GetComponent<PlayerCharacter>();
        pc.PLayerCharacterClass = bestPlayer.characterClass;
        pc.SetCharacterClass(bestPlayer.characterClass);

        void TryDestroy<T>(GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp != null) Destroy(comp);
        }

        TryDestroy<PlayerInput>(bestPlayerInstance); // 조작 입력 제거
        TryDestroy<NetworkIdentity>(bestPlayerInstance);

        var anim = bestPlayerInstance.transform
            .Find("PlayerModel/Premade_Character")
            .GetComponent<Animator>();

        StartCoroutine(PlaySequence(bestPlayer.characterClass, anim));

        void SetLayerRecursively(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
                SetLayerRecursively(child, layer);
        }

        SetLayerRecursively(bestPlayerInstance.transform, LayerMask.NameToLayer("BestPlayer"));
        bestPlayerInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 아바타 정면 보정
        var camTarget = bestPlayerInstance.transform;

        var camPos = camTarget.position + cameraOffset;
        var lookAt = camTarget.position + cameraLookOffset; // 더 높은 지점 응시

        bestPlayerCamera.transform.position = camPos;
        bestPlayerCamera.transform.LookAt(lookAt);
        //bestPlayerInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    public IEnumerator PlayMappedMotion(Constants.CharacterClass charClass, string motionName, Animator animator)
    {
        currentGraph?.Destroy();

        if (!classAnimMap.TryGetValue(charClass, out var map))
        {
            Debug.LogError($"[AnimLoad] ❗️ Unknown class: {charClass}");
            yield break; // 🔁 중요
        }

        var path = $"Animation/Player/{map.folder}/{map.prefix}{motionName}";
        var clip = Resources.Load<AnimationClip>(path);

        if (!clip)
        {
            Debug.LogError($"[AnimLoad] ❌ Animation not found at path: {path}");
            yield break; // 🔁 중요
        }

        var graph = PlayableGraph.Create("VictorySequence");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        output.SetSourcePlayable(clipPlayable);

        graph.Play();
        currentGraph = graph;

        yield return new WaitForSecondsRealtime(clip.length); // 클립 길이만큼 대기
        // graph.Destroy(); // 자동 해제
    }

    private IEnumerator PlaySequence(Constants.CharacterClass charClass, Animator anim)
    {
        // PlayMappedMotion(charClass, "Attack3", anim);
        // yield return new WaitForSecondsRealtime(1f);

        // PlayMappedMotion(charClass, "MoveSkill", anim);
        // yield return new WaitForSecondsRealtime(0.3f);
        yield return PlayMappedMotion(charClass, "Idle", anim);
    }

    public IEnumerator PopIn(Transform target, float duration = 0.5f, float scaleMultiplier = 1f)
    {
        var start = Vector3.zero;
        var end = Vector3.one * scaleMultiplier;
        var time = 0f;

        target.localScale = start;
        target.gameObject.SetActive(true);

        while (time < duration)
        {
            var t = time / duration;
            // EaseOutBack 느낌 주기
            var eased = 1f - Mathf.Pow(1f - t, 3);
            target.localScale = Vector3.LerpUnclamped(start, end, eased);
            time += Time.deltaTime;
            yield return null;
        }

        target.localScale = end;
    }

    private void OnClickReturnToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost(); // 서버와 클라이언트 모두 종료
        else if (NetworkClient.isConnected) NetworkManager.singleton.StopClient(); // 클라이언트만 종료
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        var time = 0f;
        cg.alpha = from;
        while (time < duration)
        {
            cg.alpha = Mathf.Lerp(from, to, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        cg.alpha = to;
    }

    private void OnDestroy()
    {
        currentGraph?.Destroy(); // 씬 종료 시 혹시 남아 있는 그래프도 해제
    }
}