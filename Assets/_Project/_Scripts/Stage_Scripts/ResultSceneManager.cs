using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// 결과 씬 매니저
/// - RaceManager가 남긴 결과 JSON을 읽고 UI/모델로 표시
/// - StageFour에서는 생존/탈락 표시로 전환
/// - 일정 시간 후 로비(MainScene)로 복귀
/// </summary>
public class ResultSceneManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static ResultSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion

    #region UI & Prefabs
    [Header("포디움 위치 (3D 캐릭터 표시용)")]
    public Transform podiumPositions;

    [Header("Resources/Characters 내 기본 프리팹 (없을 경우 대체용)")]
    public GameObject defaultPrefab;

    [Header("랭킹 / 상태 UI")]
    public Transform rankingPanel;       // ScrollView Content
    public GameObject rankRowPrefab;     // UI 프리팹 (TextMeshProUGUI)
    public GameObject titleRowPrefab;     // UI 프리팹 (TextMeshProUGUI)
    #endregion

    #region 내부 상태 관리
    private bool resultsDisplayed = false;           // 중복 표시 방지
    private readonly HashSet<int> spawnedActors = new();      // StageFour 스폰 추적
    private readonly HashSet<int> spawnedPodiumActors = new(); // 일반 포디움 스폰 추적
    private readonly HashSet<int> spawnedUIActors = new();     // UI 중복 방지
    #endregion


    #region Unity Flow
    private void Start()
    {
        spawnedActors.Clear();
        spawnedPodiumActors.Clear();
        spawnedUIActors.Clear();

        //LSH테스트
        ResetInventory.ResetPlayerInventory();
        //

        // 결과 JSON 동기화 시까지 재시도 루프
        StartCoroutine(TryReadResultsDelayed());

        // 7초 후 로비 복귀
        StartCoroutine(GoLobby());
    }
    #endregion


    #region Photon Property 업데이트 감지
    /// <summary>
    /// Room CustomProperty 변경 시 자동 호출
    /// StageFourDNF 또는 RaceResultsJson 변경을 감지
    /// </summary>
    public override void OnRoomPropertiesUpdate(PhotonHashtable changed)
    {
        // StageFour DNF 목록 갱신
        if (changed.ContainsKey("StageFourDNF"))
        {
            string csv = changed["StageFourDNF"] as string;
            RaceManager.Instance.stageFourDNFActors = csv?
                .Split(',')
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(int.Parse)
                .ToList() ?? new List<int>();
        }

        // 결과 JSON 변경 감지
        if (changed.ContainsKey("RaceResultsJson") && !resultsDisplayed)
        {
            Debug.Log("[ResultScene] OnRoomPropertiesUpdate - RaceResultsJson detected.");
            TryReadResults();
        }
    }
    #endregion


    #region 결과 읽기 및 UI 생성
    /// <summary>
    /// RaceResultsJson을 읽어 결과 UI 및 캐릭터를 생성
    /// </summary>
    private bool TryReadResults()
    {
        if (resultsDisplayed) return true;
        if (!PhotonNetwork.InRoom) return false;

        // CustomProperty에서 결과 JSON 가져오기
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RaceResultsJson", out object obj) && obj != null)
        {
            string json = obj.ToString();
            var wrapper = JsonUtility.FromJson<FinishWrapper>(json);
            if (wrapper?.results == null) return false;

            resultsDisplayed = true; // 중복 표시 방지

            bool isStageFour = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StageFourDNF");

            // StageFour일 경우 생존/탈락 UI
            if (isStageFour)
            {
                ShowStageFourStatusUI(wrapper.results);
                foreach (var info in wrapper.results)
                    SpawnStageFourActor(info.actorNumber, info.finishState);
            }
            // 일반 스테이지 결과
            else
            {
                ShowRankingUI(wrapper.results);
                for (int i = 0; i < wrapper.results.Count; i++)
                {
                    var info = wrapper.results[i];
                    SpawnPodiumActor(info.actorNumber, info.finishTime, info.finishState, i);
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 결과가 아직 안 들어왔을 경우 일정 시간 재시도
    /// </summary>
    private IEnumerator TryReadResultsDelayed()
    {
        float timeout = 5f;
        float timer = 0f;

        while (timer < timeout)
        {
            if (TryReadResults()) yield break;
            timer += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogWarning("[ResultScene] RaceResultsJson not received within timeout.");
    }
    #endregion


    #region 캐릭터 스폰 로직
    /// <summary>
    /// 일반 스테이지 결과 — 포디움 캐릭터 생성
    /// </summary>
    private void SpawnPodiumActor(int actorNumber, float finishTime, string state, int rankIndex)
    {
        if (spawnedPodiumActors.Contains(actorNumber)) return;
        spawnedPodiumActors.Add(actorNumber);

        // DNF는 포디움에 표시하지 않음
        if (state == "DNF")
        {
            _ = UserData.Local.GainExp(500f);
            return;
        }
        else
        {
            _ = UserData.Local.GainExp(900f);
        }
        // 로컬 플레이어만 스폰
        if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;

        Player p = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        string prefabName = p?.CustomProperties.TryGetValue("CharacterPrefab", out object cp) == true
            ? cp?.ToString() ?? "LocalPlayer"
            : "LocalPlayer";

        Vector3 pos = podiumPositions.position;
        Quaternion rot = podiumPositions.rotation;

        PhotonNetwork.Instantiate(prefabName, pos, rot);
        Debug.Log($"[ResultScene] Podium Actor Spawned - Rank {rankIndex + 1}, Actor {actorNumber}, Time {finishTime}, State {state}");
    }

    /// <summary>
    /// StageFour 결과 — 생존/탈락 캐릭터 생성
    /// </summary>
    private void SpawnStageFourActor(int actorNumber, string state)
    {
        if (spawnedActors.Contains(actorNumber)) return;
        spawnedActors.Add(actorNumber);

        // StageFourDNF 목록에 없는 DNF는 무시
        if (state == "DNF" && !RaceManager.Instance.stageFourDNFActors.Contains(actorNumber))
            return;

        if (actorNumber != PhotonNetwork.LocalPlayer.ActorNumber) return;

        Player p = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        string prefabName = p?.CustomProperties.TryGetValue("CharacterPrefab", out object cp) == true
            ? cp?.ToString() ?? "LocalPlayer"
            : "LocalPlayer";
        
        Vector3 pos = podiumPositions.position;
        Quaternion rot = podiumPositions.rotation;

        PhotonNetwork.Instantiate(prefabName, pos, rot);

        Debug.Log($"[ResultScene] StageFour Actor Spawned - Actor {actorNumber}, State {state}");
    }
    #endregion


    #region UI 생성 로직
    /// <summary>
    /// 일반 스테이지 결과 UI (랭킹 순)
    /// </summary>
    private void ShowRankingUI(List<FinishInfo> list)
    {
        foreach (Transform child in rankingPanel)
            Destroy(child.gameObject);

        spawnedUIActors.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            var info = list[i];
            if (spawnedUIActors.Contains(info.actorNumber)) continue;
            spawnedUIActors.Add(info.actorNumber);

            Player p = PhotonNetwork.CurrentRoom.GetPlayer(info.actorNumber);
            string title = p?.CustomProperties.TryGetValue("userTitle", out object cp) == true
            ? cp?.ToString() ?? ""
            : "";

            

            var go = Instantiate(rankRowPrefab, rankingPanel);
            var text = go.GetComponent<TextMeshProUGUI>();

            if (!string.IsNullOrEmpty(title))
            {
                var titlego = Instantiate(titleRowPrefab, go.transform);
                var titleText = titlego.GetComponent<TextMeshProUGUI>();
                titleText.text = $"[{title}]";
            }
            else
            {
                var titlego = Instantiate(titleRowPrefab, go.transform);
                var titleText = titlego.GetComponent<TextMeshProUGUI>();
                titleText.text = $"[칭호없음]";
            }

            string nick = PhotonNetwork.CurrentRoom.GetPlayer(info.actorNumber)?.NickName ?? $"Player{info.actorNumber}";
            string timeStr = info.finishTime >= 0 ? $"{info.finishTime:F2}s" : "DNF";

            text.text = $"{i + 1}위  {nick}  {timeStr}";
        }
    }

    /// <summary>
    /// StageFour 결과 UI (생존 / 탈락)
    /// </summary>
    private void ShowStageFourStatusUI(List<FinishInfo> list)
    {
        foreach (Transform child in rankingPanel)
            Destroy(child.gameObject);

        spawnedUIActors.Clear();

        List<int> dnfActors = new();

        // PhotonRoom에서 DNF 목록 가져오기
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("StageFourDNF", out object value) && value != null)
        {
            string csv = value as string;
            if (!string.IsNullOrEmpty(csv))
                dnfActors = csv.Split(',').Select(int.Parse).ToList();
        }

        // 플레이어별 상태 UI 표시
        foreach (var info in list)
        {
            if (spawnedUIActors.Contains(info.actorNumber)) continue;
            spawnedUIActors.Add(info.actorNumber);

            bool isStageFourDNF = RaceManager.Instance.stageFourDNFActors.Contains(info.actorNumber);
            string nick = PhotonNetwork.CurrentRoom.GetPlayer(info.actorNumber)?.NickName ?? $"Player{info.actorNumber}";
            string status = isStageFourDNF ? "탈락" : "생존";

            Player p = PhotonNetwork.CurrentRoom.GetPlayer(info.actorNumber);
            string title = p?.CustomProperties.TryGetValue("userTitle", out object cp) == true
            ? cp?.ToString() ?? ""
            : "";


            if (status == "탈락")
            {
                _ = UserData.Local.GainExp(500f);
            }
            else if(status == "생존")
            {
                _ = UserData.Local.GainExp(900f);
            }

            var go = Instantiate(rankRowPrefab, rankingPanel);
            go.GetComponent<TextMeshProUGUI>().text = $"[{nick}  :  {status}";

            if (!string.IsNullOrEmpty(title))
            {
                var titlego = Instantiate(titleRowPrefab, go.transform);
                var titleText = titlego.GetComponent<TextMeshProUGUI>();
                titleText.text = $"[{title}]";
            }
            else
            {
                var titlego = Instantiate(titleRowPrefab, go.transform);
                var titleText = titlego.GetComponent<TextMeshProUGUI>();
                titleText.text = $"[칭호없음]";
            }
        }
    }
    #endregion

    

    #region 로비 복귀 루틴
    /// <summary>
    /// 7초 후 RaceManager 데이터 초기화 + 로비(MainScene) 이동
    /// </summary>
    private IEnumerator GoLobby()
    {
        yield return new WaitForSeconds(7f);

        // RaceManager 상태 초기화
        RaceManager.Instance.ResetRaceData();

        // 커서 잠금 해제 및 표시 복원
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PhotonNetwork.LoadLevel("MainScene");
    }
    #endregion
}