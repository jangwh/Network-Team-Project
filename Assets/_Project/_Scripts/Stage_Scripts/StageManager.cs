using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// 일반 스테이지에서 경기 진행을 관리하는 매니저
/// - 카메라 인트로 연출
/// - 플레이어 스폰
/// - 타이머 및 제한 시간 체크
/// - 완주 인원에 따른 경기 종료 처리
/// </summary>
public class StageManager : MonoBehaviour, IStageManager
{
    #region Singleton
    public static StageManager Instance { get; private set; }

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

    #region Spawn & Checkpoint
    [Header("Spawn & Goal")]
    public Transform[] StartPosGroup;     // 시작 지점 배열
    public Transform[] checkpointPos;     // 체크포인트 배열
    [HideInInspector] public Transform spawnPos; // 현재 스폰 위치

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
    #endregion

    #region Timer & UI
    [Header("Time Limit")]
    public float limitTime = 60f;                // 제한 시간
    private float gameStartTime = -1f;           // 경기 시작 시각
    public TextMeshProUGUI timeText;             // 타이머 UI
    public TextMeshProUGUI playersText;          // 완주 인원 UI

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
    public int passPlayer = 2;       // 완주 후 다음 라운드로 진출할 수 있는 최대 인원 수

    [Header("테스트 모드 여부")]
    public bool isTest = false;      // 카메라 인트로 스킵용
    #endregion


    #region Unity Flow
    private void Start()
    {
        ResetCheckpoints(); // 모든 플레이어 체크포인트 초기화

        if (!isTest)
        {
            // 카메라 인트로 연출 후 플레이어 스폰
            StartCoroutine(CameraIntroRoutine());
            // 10초 뒤에 StartGameTimer() 실행
            DOVirtual.DelayedCall(10f, () =>
            {
                StartGameTimer();
            });
        }
        else
        {
            // 테스트 모드일 경우 즉시 스폰
            SpawnPlayer();
            StartGameTimer();
        }

    }

    private void Update()
    {
        TimeCheckUI();
        PlayerCountUI();

        // 마스터만 게임 종료 조건 체크
        if (!PhotonNetwork.IsMasterClient) return;

        int totalPlayers = PhotonNetwork.PlayerList.Length;
        int finishedPlayers = RaceManager.Instance.finishOrder.Count;

        // 제한시간 초과 시 강제 종료
        if (ElapsedTime > limitTime && !RaceManager.Instance.raceEnded)
        {
            Debug.Log("[StageManager] Time limit exceeded → Forcing race end.");
            RaceManager.Instance.FinalizeRaceAndMoveScene("ResultScene", true);
        }
        // 통과 인원 충족 시 정상 종료
        else if (finishedPlayers >= Mathf.Min(passPlayer, totalPlayers) && !RaceManager.Instance.raceEnded)
        {
            Debug.Log("[StageManager] Required finishers reached → Ending race.");
            RaceManager.Instance.FinalizeRaceAndMoveScene("ResultScene", true);
        }
    }
    #endregion


    #region Player Spawn
    /// <summary>
    /// 로컬 플레이어를 스폰하고 카메라를 설정한다.
    /// </summary>
    public GameObject SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogError("[StageManager] SpawnPlayer() failed - No local player found.");
            return null;
        }

        // Actor 번호 기반으로 스폰 위치 계산
        int actorIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1; // 1부터 시작
        int spawnIndex = actorIndex % StartPosGroup.Length;

        int checkIdx = -1;
        if (PhotonNetwork.LocalPlayer.TagObject is PlayerController existingPlayer)
        {
            checkIdx = existingPlayer.checkpointIndex;
        }

        // 체크포인트 여부에 따라 스폰 위치 결정
        switch (checkIdx)
        {
            case -1: spawnPos = StartPosGroup[spawnIndex]; break;
            case 0: spawnPos = checkpointPos[0]; break;
            case 1: spawnPos = checkpointPos[1]; break;
            case 2: spawnPos = checkpointPos[2]; break;
            case 3: spawnPos = checkpointPos[3]; break;
            default: spawnPos = StartPosGroup[spawnIndex]; break; // 안전장치
        }

        Quaternion spawnRot = StartPosGroup[spawnIndex].rotation;

        // 플레이어 인스턴스 생성
        GameObject newPlayer = PhotonNetwork.Instantiate("LocalPlayer", spawnPos.position, spawnRot);
        PhotonNetwork.LocalPlayer.TagObject = newPlayer.GetComponent<PlayerController>();
        newPlayer.GetComponent<PlayerController>().checkpointIndex = checkIdx;
        // 카메라 세팅
        SetupCameraForPlayer(newPlayer);
        return newPlayer;
    }

    /// <summary>
    /// 스폰된 플레이어의 카메라를 설정 (SpectatorManager 포함)
    /// </summary>
    private void SetupCameraForPlayer(GameObject newPlayer)
    {
        if (newPlayer == null) return;

        PhotonView pv = newPlayer.GetComponent<PhotonView>();
        PlayerController controller = newPlayer.GetComponent<PlayerController>();

        if (pv != null && pv.IsMine)
        {
            // 로컬 플레이어 카메라 활성화
            SpectatorManager.Instance.AddTarget(newPlayer.transform);
            SpectatorManager.Instance.SetTarget(newPlayer.transform);

            controller.camPivot.gameObject.SetActive(true);
            controller.Camera.enabled = true;
            mainCamera = controller.Camera;
        }
        else
        {
            // 원격 플레이어 카메라 비활성화
            SpectatorManager.Instance.AddTarget(newPlayer.transform);
            controller.Camera.enabled = false;
        }
    }
    #endregion


    #region Camera Intro
    /// <summary>
    /// 시작 시 카메라가 지정된 경로를 따라 이동한 후 플레이어를 스폰한다.
    /// </summary>
    private IEnumerator CameraIntroRoutine()
    {
        if (mainCamera == null || pathPoints.Count == 0)
        {
            Debug.LogWarning("[StageManager] CameraIntroRoutine skipped - Missing camera or path points.");
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

            yield return new WaitForSeconds(0.2f); // 포인트 사이 대기
        }

        // 카메라 연출이 끝나면 플레이어 스폰
        SpawnPlayer();
    }
    #endregion


    #region Timer & UI
    /// <summary>
    /// 게임 시작 시간을 기록
    /// </summary>
    public void StartGameTimer()
    {
        if (gameStartTime < 0)
            gameStartTime = Time.time;
    }

    /// <summary>
    /// 타이머 UI 업데이트
    /// </summary>
    private void TimeCheckUI()
    {
        float elapsed = Mathf.Min(ElapsedTime, limitTime);
        timeText.text = $"{elapsed:F2} / {limitTime}";
    }

    /// <summary>
    /// 현재 완주 인원 수 UI 업데이트
    /// </summary>
    private void PlayerCountUI()
    {
        int finished = 0;
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("FinishedCount", out object finishedObj))
        {
            finished = (int)finishedObj;
        }

        playersText.text = $"{finished} / {PhotonNetwork.PlayerList.Length}";
    }
    #endregion
}