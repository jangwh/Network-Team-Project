using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    private IStageManager stageManager;

    private readonly List<Transform> targets = new List<Transform>();
    private int currentIndex = 0;
    private Transform currentTarget;

    private bool isSpectating = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            DestroyImmediate(Instance);
        }
    }

    void Start()
    {
        if (StageFourManager.Instance != null)
            stageManager = StageFourManager.Instance;
        else if (StageManager.Instance != null)
            stageManager = StageManager.Instance;
    }

    void Update()
    {
        CleanUpTargets();

        if (!isSpectating) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            NextTarget();
        }
    }

    //외부에서 호출할 메서드
    public void AddTarget(Transform target)
    {
        if (!targets.Contains(target))
            targets.Add(target);

        // 관전 중이 아니면 현재타겟 자동 설정 X
        if (!isSpectating && currentTarget == null)
            SetTarget(target);
    }

    public void RemoveTarget(Transform target)
    {
        targets.Remove(target);
        if (currentTarget == target)
        {
            currentTarget = null;
            if (targets.Count > 0) SetTarget(targets[0]);
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        currentIndex = targets.IndexOf(target);
    }

    public void NextTarget()
    {
        CleanUpTargets();
        if (targets.Count == 0) return;
        currentIndex = (currentIndex + 1) % targets.Count;
        currentTarget = targets[currentIndex];

        SwitchToCurrentCamera();
    }

    public void EnterSpectatorMode()
    {
        if (isSpectating) return;
        isSpectating = true;

        if (targets.Count > 0)
        {
            currentIndex = 0;
            SwitchToCurrentCamera();
        }
    }
    private void CleanUpTargets()
    {
        // Destroy된 Transform 제거
        targets.RemoveAll(t => t == null);
        // currentTarget이 사라졌다면 null 처리
        if (currentTarget == null && targets.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, targets.Count - 1);
            currentTarget = targets[currentIndex];
        }
    }
    private void SwitchToCurrentCamera()
    {
        CleanUpTargets();
        for (int i = 0; i < targets.Count; i++)
        {
            var pc = targets[i].GetComponent<PlayerController>();
            if (pc?.Camera != null)
            {
                bool isCurrent = (i == currentIndex);
                pc.Camera.enabled = isCurrent;            // enabled 토글
                pc.Camera.gameObject.SetActive(isCurrent);// 필요 시 GameObject 활성화
            }
        }

        currentTarget = targets[currentIndex];
        if (stageManager != null)
        {
            stageManager.mainCamera = currentTarget.GetComponent<PlayerController>().Camera;
        }
    }
}
