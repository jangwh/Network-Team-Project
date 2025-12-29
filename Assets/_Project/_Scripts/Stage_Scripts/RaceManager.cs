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
using PhotonHash = ExitGames.Client.Photon.Hashtable;

#region 데이터 구조
/// <summary>
/// 각 플레이어의 완주 정보 (결과 기록용)
/// </summary>
[Serializable]
public class FinishInfo
{
    public int actorNumber;     // 플레이어 고유 Actor 번호
    public float finishTime;    // 완주 시간 (DNF 시 -1)
    public string finishState;  // "FIN" 또는 "DNF"
}

/// <summary>
/// JSON 직렬화를 위한 래퍼 클래스
/// </summary>
[Serializable]
public class FinishWrapper
{
    public List<FinishInfo> results;
}
#endregion


/// <summary>
/// 경기 전반을 관리하는 중앙 Race 매니저
/// - 완주/탈락(DNF) 기록 관리
/// - CustomProperty에 결과 기록
/// - ResultScene으로 전환
/// </summary>
public class RaceManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static RaceManager Instance { get; private set; }

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

    #region 변수
    public List<int> finishOrder = new List<int>();             // 완주 순서 기록
    public Dictionary<int, float> finishTimes = new Dictionary<int, float>(); // 완주 시간 기록
    public bool raceEnded = false;                              // 경기 종료 여부

    public const byte EV_DNF = 101; // Photon 커스텀 이벤트 코드 (StageFour DNF 통신용)

    // StageFour 한정 - DNF 플레이어 목록
    public List<int> stageFourDNFActors = new List<int>();
    #endregion

    #region Photon Event 구독
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
    }
    #endregion

    #region 완주 기록 관련
    /// <summary>
    /// 완주한 플레이어를 등록 (마스터 전용)
    /// </summary>
    public void RegisterFinish(int actorNumber, float finishTime)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 기록
        if (finishOrder.Contains(actorNumber)) return; // 중복 방지

        finishOrder.Add(actorNumber);
        finishTimes[actorNumber] = finishTime;

        Debug.Log($"[RaceManager] Finish recorded: actor {actorNumber}, time {finishTime}");

        // 완주 수를 CustomProperty로 동기화
        var props = new PhotonHash { ["FinishedCount"] = finishOrder.Count };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    /// <summary>
    /// DNF(탈락) 플레이어를 등록 (StageFour 포함)
    /// </summary>
    public void RegisterDNF(int actorNumber, bool isStageFour = false)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (finishOrder.Contains(actorNumber)) return;

        finishOrder.Add(actorNumber);
        finishTimes[actorNumber] = -1f; // DNF 표시용 시간

        Debug.Log($"[RaceManager] Player {actorNumber} DNF registered (StageFour={isStageFour})");

        // StageFour 전용 DNF 처리
        if (isStageFour)
        {
            if (!stageFourDNFActors.Contains(actorNumber))
                stageFourDNFActors.Add(actorNumber);

            // 문자열 형태로 변환해 CustomProperty에 저장
            var dnfProps = new PhotonHash { ["StageFourDNF"] = string.Join(",", stageFourDNFActors) };
            PhotonNetwork.CurrentRoom.SetCustomProperties(dnfProps);
        }

        // 전체 완료 인원 수 갱신
        var finishedProps = new PhotonHash { ["FinishedCount"] = finishOrder.Count };
        PhotonNetwork.CurrentRoom.SetCustomProperties(finishedProps);
    }
    #endregion

    #region 경기 종료 및 결과 전송
    /// <summary>
    /// 경기 종료 후 ResultScene으로 이동
    /// - 모든 플레이어가 완주했거나 강제 종료 시 호출
    /// </summary>
    public void FinalizeRaceAndMoveScene(string sceneName, bool force = false)
    {
        if (!PhotonNetwork.IsMasterClient || raceEnded) return;

        int total = PhotonNetwork.PlayerList.Length;
        if (!force && finishOrder.Count < total)
        {
            Debug.LogWarning("[RaceManager] Attempted to finalize before all players finished.");
            return;
        }

        raceEnded = true;

        List<FinishInfo> results = new List<FinishInfo>();

        // 1. finishOrder 순서대로 결과 기록
        foreach (int actor in finishOrder)
        {
            results.Add(new FinishInfo
            {
                actorNumber = actor,
                finishTime = finishTimes.GetValueOrDefault(actor, -1f),
                finishState = stageFourDNFActors.Contains(actor) ? "DNF" : "FIN"
            });
        }

        // 2. 완주하지 않은 플레이어 자동 DNF 처리
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (results.Any(r => r.actorNumber == p.ActorNumber)) continue;

            bool isStageFourDNF = stageFourDNFActors.Contains(p.ActorNumber);
            results.Add(new FinishInfo
            {
                actorNumber = p.ActorNumber,
                finishTime = -1f,
                finishState = isStageFourDNF ? "DNF" : "DNF"
            });
        }

        // 3. 결과 JSON 직렬화 및 CustomProperty로 저장
        string json = JsonUtility.ToJson(new FinishWrapper { results = results });
        PhotonNetwork.CurrentRoom.SetCustomProperties(new PhotonHash { ["RaceResultsJson"] = json });

        Debug.Log($"[RaceManager] RaceResultsJson set successfully: {json}");

        // 4. 결과 씬으로 전환
        StartCoroutine(LoadResultSceneDelayed(sceneName, 3f));
    }

    /// <summary>
    /// ResultScene으로 전환 전 대기 시간
    /// </summary>
    private IEnumerator LoadResultSceneDelayed(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.LoadLevel(sceneName);
    }
    #endregion

    #region 데이터 초기화
    /// <summary>
    /// 레이스 관련 데이터 초기화 (씬 이동 시 호출)
    /// </summary>
    public void ResetRaceData()
    {
        finishOrder.Clear();
        finishTimes.Clear();
        stageFourDNFActors.Clear();
        raceEnded = false;

        var clearProps = new PhotonHash
        {
            ["RaceResultsJson"] = null,
            ["StageFourDNF"] = null,
            ["FinishedCount"] = 0
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(clearProps);

        Debug.Log("[RaceManager] Race data cleared successfully.");
    }
    #endregion

    #region Photon 이벤트 처리
    /// <summary>
    /// StageFour DNF 이벤트 수신 처리 (클라이언트 → 마스터)
    /// </summary>
    private void OnEventReceived(EventData photonEvent)
    {
        if (photonEvent?.Code != EV_DNF) return;

        try
        {
            int actorNumber = Convert.ToInt32(photonEvent.CustomData);

            // 마스터만 처리 (중복 방지)
            if (!PhotonNetwork.IsMasterClient) return;

            RegisterDNF(actorNumber, true); // StageFour DNF 등록
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RaceManager] OnEventReceived EV_DNF parse error: {ex}");
        }
    }
    #endregion
}