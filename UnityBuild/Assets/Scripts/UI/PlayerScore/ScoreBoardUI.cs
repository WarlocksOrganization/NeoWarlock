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
    [SerializeField] private GameObject rankPanelPrefab;
    [SerializeField] private Transform rankPanelParent;
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

    private List<PlayerScorePanel> scorePanels = new();
    private List<RectTransform> panelPositions = new();
    private PlayableGraph? currentGraph;
    private bool isBestPlayerSpawned;

    private readonly Dictionary<Constants.CharacterClass, (string folder, string prefix)> classAnimMap = new()
    {
        { Constants.CharacterClass.Necromancer, ("Necro", "N") },
        { Constants.CharacterClass.Warrior, ("Warrior", "W") },
        { Constants.CharacterClass.Mage, ("Magician", "M") },
        { Constants.CharacterClass.Archer, ("Archer", "A") },
        { Constants.CharacterClass.Priest, ("Priest", "P") }
    };

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
        yield return new WaitForSeconds(20f);
        if (returnToLobbyButton.gameObject.activeSelf)
            OnClickReturnToLobby();
    }

    public void ShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        foreach (var r in records)
            Debug.Log($"[ScoreBoardUI] {r.nickname} score@{roundIndex} = {r.GetScoreAtRound(roundIndex)}");

        teamResult.gameObject.SetActive(false);
        gameObject.SetActive(true);

        CreateRankPanels(records.Length);
        CreateScorePanels(records);

        scorePanelParent.gameObject.SetActive(false);
        StartCoroutine(DelayedRankingFlow(records, roundIndex));
    }

    private void CreateRankPanels(int count)
    {
        foreach (Transform child in rankPanelParent)
            Destroy(child.gameObject);
        panelPositions.Clear();

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(rankPanelPrefab, rankPanelParent);
            panelPositions.Add(go.GetComponent<RectTransform>());
            go.GetComponent<RankPanel>()?.Init(i + 1);
        }
    }

    private void CreateScorePanels(Constants.PlayerRecord[] records)
    {
        foreach (Transform child in scorePanelParent)
            Destroy(child.gameObject);

        scorePanels.Clear();
        foreach (var record in records.OrderBy(r => r.playerId))
        {
            var panel = Instantiate(scorePanelPrefab, scorePanelParent).GetComponent<PlayerScorePanel>();
            panel.id = record.playerId;
            scorePanels.Add(panel);
        }
    }

    private IEnumerator DelayedRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        scorePanelParent.gameObject.SetActive(true);
        StartCoroutine(ShowRankingFlow(records, roundIndex));
    }

    private IEnumerator ShowRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        roundText.text = $"{roundIndex + 1} 라운드 결과";

        var roundSorted = records
            .OrderByDescending(r => r.GetScoreAtRound(roundIndex))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < roundSorted.Count; i++)
        {
            var record = roundSorted[i];
            var panel = scorePanels.FirstOrDefault(p => p.id == record.playerId);
            if (panel != null)
            {
                panel.SetupWithScore(
                    record,
                    record.GetScoreAtRound(roundIndex),
                    roundIndex,
                    includeCurrentRound: false,
                    localPlayerId: GamePlayer.GetLocalPlayerId()
                );
            }
            else
            {
                Debug.LogWarning($"[ScoreBoard] playerId {record.playerId}에 해당하는 패널이 없습니다.");
            }
            panel.gameObject.SetActive(true);

            panel.SetupWithScore(record, record.GetScoreAtRound(roundIndex), roundIndex, false, GamePlayer.GetLocalPlayerId());
            panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
            panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
        }

        yield return new WaitForSeconds(3f);
        yield return FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f);

        roundText.text = $"{roundIndex + 1} 라운드 통계";
        var preSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex - 1))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < preSorted.Count; i++)
        {
            var record = preSorted[i];
            
            var panel = scorePanels.FirstOrDefault(p => p.id == record.playerId);
            if (panel != null)
            {
                panel.SetupWithScore(
                    record,
                    record.GetScoreAtRound(roundIndex),
                    roundIndex,
                    includeCurrentRound: false,
                    localPlayerId: GamePlayer.GetLocalPlayerId()
                );
            }
            else
            {
                Debug.LogWarning($"[ScoreBoard] playerId {record.playerId}에 해당하는 패널이 없습니다.");
            }

            var score = record.GetTotalScoreUpToRound(roundIndex - 1);
            panel.SetupWithScore(record, score, roundIndex - 1, true, GamePlayer.GetLocalPlayerId());
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
                
                string result = $"<color=#{colorA}>Team A</color> : {teamAScoreBefore}점\n<color=#{colorB}>Team B</color> : {teamBScoreBefore}점";
                teamResultText.text = result;
                teamResult.gameObject.SetActive(true);
            }
        }

        yield return FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f);
        yield return new WaitForSeconds(1f);

        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_ScoreCount);

        var finalSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex))
            .ThenByDescending(r => r.roundStatsList[roundIndex].kills + r.roundStatsList[roundIndex].outKills)
            .ThenBy(r => r.playerId)
            .ToList();

        for (int i = 0; i < finalSorted.Count; i++)
        {
            var record = finalSorted[i];
            var panel = scorePanels.First(p => p.id == record.playerId);
            int before = preSorted.FirstOrDefault(p => p.playerId == record.playerId)?.GetTotalScoreUpToRound(roundIndex - 1) ?? 0;
            int after = record.GetTotalScoreUpToRound(roundIndex);

            panel.SetupWithScore(record, after, roundIndex, true, GamePlayer.GetLocalPlayerId());
            panel.AnimateScore(before, after);
            panel.MoveTo(panelPositions[i].anchoredPosition);
            
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
            StartCoroutine(FadeCanvasGroup(ResultBoard.GetComponent<CanvasGroup>(), 0f, 1f, 0.5f));
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

            teamResultText.text = $"<color=#{colorA}>Team A</color> : {scoreA}점\n<color=#{colorB}>Team B</color> : {scoreB}점";
            time += Time.deltaTime;
            yield return null;
        }

        // 마지막 값 보정
        teamResultText.text = $"<color=#{colorA}>Team A</color> : {toA}점\n<color=#{colorB}>Team B</color> : {toB}점";
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
            .OrderByDescending(r => r.GetTotalScoreUpToRound(2))
            .FirstOrDefault();

        var bestKill = records
            .OrderByDescending(r => r.roundStatsList.Sum(rs => rs.kills + rs.outKills)) // 1차 기준: 킬 수
            .ThenByDescending(r => r.GetTotalScoreUpToRound(2)) // 2차 기준: 점수
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

        var myId = GamePlayer.GetLocalPlayerId();;

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

    private void OnClickReturnToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }

    private void OnDestroy()
    {
        teamResult.SetActive(false);
        currentGraph?.Destroy();
    }
}