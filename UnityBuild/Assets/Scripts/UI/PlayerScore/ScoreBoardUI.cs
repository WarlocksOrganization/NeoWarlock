using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using GameManagement;
using DataSystem.Database;
using Mirror;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private PlayerScorePanel[] scorePanels;
    [SerializeField] private RectTransform[] panelPositions; // 1등 ~ 6등 위치

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
    [SerializeField] Vector3 cameraOffset = new Vector3(-0.8f, 0.9f, -2.4f);
    [SerializeField] Vector3 cameraLookOffset = new Vector3(0f, 1.8f, 0f);
    private readonly Dictionary<Constants.CharacterClass, (string folder, string prefix)> classAnimMap =
    new()
    {
        { Constants.CharacterClass.Necromancer, ("Necro", "N") },
        { Constants.CharacterClass.Warrior, ("Warrior", "W") },
        { Constants.CharacterClass.Mage, ("Magician", "M") },
        { Constants.CharacterClass.Archer, ("Archer", "A") },
        { Constants.CharacterClass.Priest, ("Priest", "P") },
    };
    private PlayableGraph? currentGraph = null;

    private bool isBestPlayerSpawned  = false;

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
        float delay = 20f;
        yield return new WaitForSeconds(delay);

        // ✅ 버튼을 누르지 않았을 경우에만 자동 복귀 실행
        if (returnToLobbyButton.gameObject.activeSelf)
        {
            Debug.Log("[ScoreBoardUI] 201초가 지나 자동으로 로비로 돌아갑니다.");
            OnClickReturnToLobby();
        }
    }

    public void ShowScoreBoard(Constants.PlayerRecord[] records, int roundIndex)
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowRankingFlow(records, roundIndex)); // ✅ records 사용
    }

    private IEnumerator ShowRankingFlow(Constants.PlayerRecord[] records, int roundIndex)
    {
        // 1. 현재 라운드 결과 표시
        roundText.text = $"{roundIndex + 1} 라운드 결과";

        var roundSorted = records
            .OrderByDescending(r => GameManager.Instance.GetScoreAtRound(r, roundIndex))
            .ToList();

        for (int i = 0; i < roundSorted.Count; i++)
        {
            var record = roundSorted[i];
            var stats = record.roundStatsList[roundIndex];

        var panel = scorePanels[record.playerId];
        panel.gameObject.SetActive(true);
        
        int localPlayerId = PlayerSetting.PlayerId;

        panel.SetupWithScore(record, GameManager.Instance.GetScoreAtRound(record, roundIndex), roundIndex, includeCurrentRound: false, localPlayerId );
        panel.SetRoundRanks(record.roundStatsList.Take(roundIndex).Select(r => r.rank).ToList());
        panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
    }

        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.5f));

        // 2. 누적 점수 (반영 전)
        roundText.text = $"{roundIndex + 1} 라운드 통계";

        var preSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex - 1))
            .ToList();

    for (int i = 0; i < preSorted.Count; i++)
    {
        var panel = scorePanels[preSorted[i].playerId];
        int preScore = preSorted[i].GetTotalScoreUpToRound(roundIndex - 1);
        int localPlayerId = PlayerSetting.PlayerId;
        panel.SetupWithScore(preSorted[i], preScore, roundIndex - 1, includeCurrentRound: true, localPlayerId);
        panel.GetComponent<RectTransform>().anchoredPosition = panelPositions[i].anchoredPosition;
    }

        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.5f));
        yield return new WaitForSeconds(1f);

        AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_ScoreCount);

        var finalSorted = records
            .OrderByDescending(r => r.GetTotalScoreUpToRound(roundIndex))
            .ToList();

    for (int i = 0; i < finalSorted.Count; i++)
    {
        var panel = scorePanels.First(p => p.id == finalSorted[i].playerId);
        int before = preSorted.First(p => p.playerId == finalSorted[i].playerId).GetTotalScoreUpToRound(roundIndex - 1);
        int after = finalSorted[i].GetTotalScoreUpToRound(roundIndex);
        int localPlayerId = PlayerSetting.PlayerId;
        panel.SetupWithScore(finalSorted[i], after, roundIndex, includeCurrentRound: true, localPlayerId);
        panel.AnimateScore(before, after);
        panel.MoveTo(panelPositions[i].anchoredPosition);
        
        // ✅ 데미지 애니메이션 추가
        int prevDamage = 0;
        for (int r = 0; r <= roundIndex - 1; r++)
        {
            if (r < finalSorted[i].roundStatsList.Count)
                prevDamage += finalSorted[i].roundStatsList[r].damageDone;
        }

            int finalDamage = prevDamage;
            if (roundIndex < finalSorted[i].roundStatsList.Count)
                finalDamage += finalSorted[i].roundStatsList[roundIndex].damageDone;

            panel.AnimateDamage(prevDamage, finalDamage);
        }

        int totalRounds = 3;
        if (roundIndex >= totalRounds - 1)
        {
            yield return new WaitForSeconds(3f); // 연출 간격

            var ResultBoard = this.transform.Find("ResultBoard").gameObject;
            var ScoreBoard = this.transform.Find("ScoreBoard").gameObject;
            var ToggleButton = this.transform.Find("ToggleButton").GetComponent<Button>();
            var LobbyButton = this.transform.Find("LobbyButton").GetComponent<Button>();


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
                bool showResult = !ResultBoard.activeSelf;
                ResultBoard.SetActive(showResult);
                ScoreBoard.SetActive(!showResult);
                toggleButtonText.text = showResult ? "상세 보기" : "결과 보기";
            });
            LobbyButton.onClick.AddListener(() => OnClickReturnToLobby());

        }
    }

    private void SetResultBoardData(Constants.PlayerRecord[] records, out Constants.PlayerRecord bestPlayer)
    {
    Debug.Log("[ResultBoard] SetResultBoardData 진입");

    bestPlayer = records
        .OrderByDescending(r => r.GetTotalScoreUpToRound(GameManager.Instance.currentRound - 1))
        .FirstOrDefault();

    var bestKill = records
        .OrderByDescending(r => r.roundStatsList.Sum(rs => rs.kills + rs.outKills))     // 1차 기준: 킬 수
        .ThenByDescending(r => r.GetTotalScoreUpToRound(GameManager.Instance.currentRound - 1)) // 2차 기준: 점수
        .First();

    var bestDamage = records
        .OrderByDescending(r => r.roundStatsList.Sum(rs => rs.damageDone))
        .FirstOrDefault();

        Debug.Log($"[ResultBoard] bestPlayer = {bestPlayer?.nickname}, bestKill = {bestKill?.nickname}, bestDamage = {bestDamage?.nickname}");

        if (bestPlayer == null || bestKill == null || bestDamage == null)
        {
            Debug.LogError("❗ GetTopXXXPlayer 중 null 반환됨");
            return;
        }

        int myId = PlayerSetting.PlayerId;
        string ColorizeName(string name, int id)
        {
            return id == myId ? $"<color=yellow>{name}</color>" : name;
        }

        bestPlayerName.text = ColorizeName(bestPlayer.nickname, bestPlayer.playerId);
        bestPlayerStat.text = $"점수 합계 : {bestPlayer.GetTotalScoreUpToRound(2)}";
        bestPlayerIcon.sprite = Database.GetCharacterClassData(bestPlayer.characterClass).CharacterIcon;

        bestKillName.text = ColorizeName(bestKill.nickname, bestKill.playerId);
        int bestKillCounts = bestKill.roundStatsList.Sum(rs => rs.kills + rs.outKills);
        bestKillStat.text = $"누적 처치 : {bestKillCounts}";
        bestKillIcon.sprite = Database.GetCharacterClassData(bestKill.characterClass).CharacterIcon;

        bestDamageName.text = ColorizeName(bestDamage.nickname, bestDamage.playerId);
        int bestDamageDone = bestDamage.roundStatsList.Sum(rs => rs.damageDone);
        bestDamageStat.text = $"누적 데미지 : {bestDamageDone}";
        bestDamageIcon.sprite = Database.GetCharacterClassData(bestDamage.characterClass).CharacterIcon;
    }

    private void showBestPlayer(Constants.PlayerRecord bestPlayer)
    {
        if (isBestPlayerSpawned )
        {
            return;
        }

        isBestPlayerSpawned  = true;
        
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

        Animator anim = bestPlayerInstance.transform
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

        Vector3 camPos = camTarget.position + cameraOffset;
        Vector3 lookAt = camTarget.position + cameraLookOffset; // 더 높은 지점 응시

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

        string path = $"Animation/Player/{map.folder}/{map.prefix}{motionName}";
        AnimationClip clip = Resources.Load<AnimationClip>(path);

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
    
    IEnumerator PlaySequence(Constants.CharacterClass charClass, Animator anim)
    {
        // PlayMappedMotion(charClass, "Attack3", anim);
        // yield return new WaitForSecondsRealtime(1f);

        // PlayMappedMotion(charClass, "MoveSkill", anim);
        // yield return new WaitForSecondsRealtime(0.3f);
        yield return PlayMappedMotion(charClass, "Idle", anim);
    }
    
    public IEnumerator PopIn(Transform target, float duration = 0.5f, float scaleMultiplier = 1f)
    {
        Vector3 start = Vector3.zero;
        Vector3 end = Vector3.one * scaleMultiplier;
        float time = 0f;

        target.localScale = start;
        target.gameObject.SetActive(true);

        while (time < duration)
        {
            float t = time / duration;
            // EaseOutBack 느낌 주기
            float eased = 1f - Mathf.Pow(1f - t, 3);
            target.localScale = Vector3.LerpUnclamped(start, end, eased);
            time += Time.deltaTime;
            yield return null;
        }

        target.localScale = end;
    }
    private void OnClickReturnToLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost(); // 서버와 클라이언트 모두 종료
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient(); // 클라이언트만 종료
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float time = 0f;
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