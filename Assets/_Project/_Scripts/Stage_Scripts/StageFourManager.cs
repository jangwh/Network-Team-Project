using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Stage 4 전용 경기 매니저
/// - 물에 빠진 플레이어를 DNF로 처리
/// - 마지막 생존자 1명이 남으면 경기 종료
/// - StageFour 전용 DNF 동기화 관리
/// </summary>
public class StageFourManager : MonoBehaviour, IStageManager
{
    #region Singleton
    public static StageFourManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
    #endregion

    #region Camera & Environment
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;           // 경기 시작 전 연출용 카메라
    [SerializeField] private List<Transform> pathPoints;  // 카메라 이동 경로
    [SerializeField] private float camMoveSpeed = 3f;     // 카메라 이동 속도
    #endregion

    #region Spawn
    [Header("Start Positions")]
    public Transform[] StartPosGroup;     // 시작 지점
    [HideInInspector] public Transform spawnPos; // 현재 스폰 위치
    #endregion

    #region Timer & UI
    [Header("Time Limit")]
    public float limitTime = 180f;               // 제한 시간
    private float gameStartTime = -1f;           // 경기 시작 시각
    public TextMeshProUGUI playersText;          // 생존/탈락 인원 UI

    /// <summary>
    /// 현재까지 경과한 시간 (게임 시작 후)
    /// </summary>
    public float ElapsedTime => gameStartTime < 0 ? 0f : Time.time - gameStartTime;

    Camera IStageManager.mainCamera
    {
        get => mainCamera;
        set => mainCamera = value;
    }
    #endregion

    #region Game Rule
    [Header("통과 가능 인원")]
    public int passPlayer = 1;           // 마지막 생존자 수
    [Header("테스트 모드 여부")]
    public bool isTest = false;          // 카메라 인트로 스킵용
    #endregion


    #region Unity Flow
    private void Start()
    {
        ResetCheckpoints();

        if (!isTest)
        {
            // 카메라 인트로 연출 후 스폰
            StartCoroutine(CameraIntroRoutine());
        }
        else
        {
            SpawnPlayerStageFour();
        }

        StartGameTimer();
    }

    private void Update()
    {
        PlayerCountUI();
        SyncStageFourDNF();

        // 마스터만 경기 종료 로직 검사
        if (!PhotonNetwork.IsMasterClient) return;
        if (RaceManager.Instance.raceEnded) return;

        // 생존자 수 계산
        int aliveCount = PhotonNetwork.PlayerList.Length - RaceManager.Instance.stageFourDNFActors.Count;

        // 마지막 1인만 생존 시 즉시 경기 종료
        if (aliveCount == 1)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (RaceManager.Instance.stageFourDNFActors.Contains(player.ActorNumber)) continue;

                // 아직 완주 기록이 없다면 완주로 등록
                if (!RaceManager.Instance.finishOrder.Contains(player.ActorNumber))
                {
                    RaceManager.Instance.RegisterFinish(player.ActorNumber, ElapsedTime);
                }
            }

            Debug.Log("[StageFourManager] Last survivor detected → Race ending.");
            RaceManager.Instance.FinalizeRaceAndMoveScene("ResultScene", true);
        }

        // 제한시간 초과 시 강제 종료 (안전 장치)
        if (ElapsedTime > limitTime && !RaceManager.Instance.raceEnded)
        {
            Debug.Log("[StageFourManager] Time limit exceeded → Forcing race end.");
            RaceManager.Instance.FinalizeRaceAndMoveScene("ResultScene", true);
        }
    }
    #endregion


    #region Player Spawn
    /// <summary>
    /// StageFour 전용 플레이어 스폰
    /// </summary>
    public GameObject SpawnPlayerStageFour()
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogError("[StageFourManager] SpawnPlayerStageFour() failed - No local player found.");
            return null;
        }

        // Actor 번호 기반 스폰 위치 계산
        int actorIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        int spawnIndex = actorIndex % StartPosGroup.Length;
        spawnPos = StartPosGroup[spawnIndex];

        Quaternion spawnRot = spawnPos.rotation;

        // 플레이어 생성
        GameObject newPlayer = PhotonNetwork.Instantiate("LocalPlayer", spawnPos.position, spawnRot);
        PhotonNetwork.LocalPlayer.TagObject = newPlayer.GetComponent<PlayerController>();

        // 카메라 세팅
        SetupCameraForPlayer(newPlayer);
        return newPlayer;
    }

    /// <summary>
    /// 카메라 설정 (로컬/리모트 구분)
    /// </summary>
    private void SetupCameraForPlayer(GameObject newPlayer)
    {
        if (newPlayer == null) return;

        PhotonView pv = newPlayer.GetComponent<PhotonView>();
        PlayerController controller = newPlayer.GetComponent<PlayerController>();

        if (pv != null && pv.IsMine)
        {
            SpectatorManager.Instance.AddTarget(newPlayer.transform);
            SpectatorManager.Instance.SetTarget(newPlayer.transform);

            controller.camPivot.gameObject.SetActive(true);
            controller.Camera.enabled = true;
            mainCamera = controller.Camera;
        }
        else
        {
            SpectatorManager.Instance.AddTarget(newPlayer.transform);
            controller.Camera.enabled = false;
        }
    }
    #endregion


    #region Checkpoint & Timer
    /// <summary>
    /// 모든 플레이어의 체크포인트 인덱스를 초기화한다.
    /// </summary>
    public void ResetCheckpoints()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.TagObject is PlayerController pc)
            {
                pc.checkpointIndex = -1;
            }
        }
    }

    /// <summary>
    /// 게임 시작 시간 기록
    /// </summary>
    public void StartGameTimer()
    {
        if (gameStartTime < 0)
            gameStartTime = Time.time;
    }
    #endregion


    #region Camera Intro
    /// <summary>
    /// 시작 시 카메라가 지정된 경로를 따라 이동 후 플레이어를 스폰한다.
    /// </summary>
    private IEnumerator CameraIntroRoutine()
    {
        if (mainCamera == null || pathPoints.Count == 0)
        {
            Debug.LogWarning("[StageFourManager] CameraIntroRoutine skipped - Missing camera or path points.");
            yield break;
        }

        foreach (Transform target in pathPoints)
        {
            while (Vector3.Distance(mainCamera.transform.position, target.position) > 0.1f)
            {
                mainCamera.transform.position = Vector3.MoveTowards(
                    mainCamera.transform.position,
                    target.position,
                    camMoveSpeed * Time.deltaTime
                );
                mainCamera.transform.LookAt(target);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        SpawnPlayerStageFour();
    }
    #endregion


    #region UI & Sync
    /// <summary>
    /// 생존/탈락 인원 수 UI 표시
    /// </summary>
    private void PlayerCountUI()
    {
        int totalPlayers = PhotonNetwork.PlayerList.Length;
        int dnfCount = RaceManager.Instance.stageFourDNFActors?.Count ?? 0;

        playersText.text = $"생존자: {totalPlayers - dnfCount} / {totalPlayers}";
    }

    /// <summary>
    /// StageFourDNF 목록을 Photon CustomProperty에서 동기화
    /// </summary>
    private void SyncStageFourDNF()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("StageFourDNF", out object value))
        {
            string csv = value as string;
            if (!string.IsNullOrEmpty(csv))
            {
                RaceManager.Instance.stageFourDNFActors = csv
                    .Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(int.Parse)
                    .ToList();
            }
        }
    }
    #endregion
}