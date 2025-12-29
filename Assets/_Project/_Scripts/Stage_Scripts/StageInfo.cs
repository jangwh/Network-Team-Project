using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/StageInfo")]
public class StageInfo : ScriptableObject
{
    [Header("플레이어가 보는 이름")]
    public string displayName;        // UI에 보일 이름 (예: "바닷가")

    [Header("빌드 세팅에 등록된 씬 이름")]
    public string sceneName;          // 실제 로드할 씬 이름 (예: "StageOneScene")

    [Header("썸네일 이미지")]
    public Sprite thumbnail;          // UI에 보여줄 이미지
}
