# Don't Nudge Me
<img width="1194" height="663" alt="Image" src="https://github.com/user-attachments/assets/8c2e990f-8b7b-480e-9d14-6f149e5ab50c" />

#  Don't Nudge Me
5인 개발 네트워크 프로젝트 

[돈넛지미 플레이 영상](https://youtu.be/tT7jEDP2Mpo)

## 1. 프로젝트 개요

폴가이즈를 레퍼런스로 만든 3D 플랫포머 파티 게임입니다.

Unity를 활용하여 3D로 제작하였습니다

개발기간 : 2025.09.22 ~ 2025.10.14

## 2. 주요 기능
   
장우형 개발파트

### 2.1 네트워크 (Photon PUN)

* 본 프로젝트는 Photon PUN을 기반으로 멀티플레이를 구성했으며, 룸 커스텀 프로퍼티 + RPC + PhotonNetwork 흐름 제어를 통해 로비, 맵 선택, UI, 게임 시작을 안정적으로 동기화합니다.

### [MapChangePanel.cs](Assets/_Project/_Scripts/Main/MapChangePanel.cs)

#### 💡 역할

* 방 내 스테이지(맵) 선택 UI 제공

* 선택된 맵 정보를 Room Custom Properties에 저장

* RPC를 통해 다른 클라이언트 UI 즉시 동기화

#### 🌐 적용된 네트워크 요소

* 룸 커스텀 프로퍼티: 맵 정보를 방 단위 상태로 저장하여 Late Join에도 자동 반영

* RPC: 현재 접속자 UI 즉시 동기화

* PhotonNetwork 조건 처리: 마스터 클라이언트 권한 기반 제어

#### 📌 주요 메서드

* OnMapSelected(StageInfo stage) : 선택된 맵 정보를 Room Custom Property에 저장, RoomPanel UI 즉시 반영

* RPC로 타 클라이언트 RoomPanel 동기화

### [RoomPanel.cs](Assets/_Project/_Scripts/Main/RoomPanel.cs)

#### 💡 역할

* 로비(대기방) UI 전반 관리

* 룸 정보, 플레이어 리스트, 준비 상태, 맵 정보 표시

* Room Custom Properties 변경 실시간 반영

#### 🌐 적용된 네트워크 요소

* Photon 콜백: 룸 상태 변경 감지 및 UI 자동 갱신

* 룸 커스텀 프로퍼티: 맵 이미지/이름 동기화 기준 데이터

* 플레이어 준비 상태 관리: 개별 Custom Property 관리, 시작 권한은 마스터만 보유

* RPC 보조 동기화: 즉각적인 UI 반영 목적

#### 📌 주요 메서드

* SetStageImageByKey(string imageKey): StageList에서 썸네일 탐색 후 적용

* OnRoomPropertiesUpdate(Hashtable changedProps): 맵 정보 변경 감지 및 UI 갱신

* RPC_UpdateStageImage(string imageKey): RPC 기반 맵 이미지 동기화

* RPC_UpdateStageName(string stageName): RPC 기반 맵 이름 동기화

### 2.2 인게임 시스템

#### 2.2.1 인게임 플로우

### [StageManager.cs](Assets/_Project/_Scripts/Stage_Scripts/StageManager.cs)

#### 💡 역할

* 스테이지 단위 경기 흐름 총괄

* 카메라 인트로 → 플레이어 스폰 → 게임 타이머 시작

* 제한 시간/완주 인원 기반 경기 종료 판정

* 체크포인트 기반 스폰 위치 결정

#### 📌 주요 메서드

* SpawnPlayer() : ActorNumber 기반 스폰 위치 결정, 체크포인트 존재 시 해당 위치로 재스폰, PhotonNetwork.Instantiate로 로컬 플레이어 생성

* SetupCameraForPlayer(GameObject newPlayer) : 로컬이면 개인 카메라 활성화, 원격이면 카메라 비활성화 및 관전 대상만 등록


### [RaceManager.cs](Assets/_Project/_Scripts/Stage_Scripts/RaceManager.cs)

#### 💡 역할

* 경기 결과 데이터 관리 (마스터 단일 판정)

* 완주/탈락(DNF) 기록 수집

* 결과 데이터를 Room Custom Properties에 저장

* ResultScene 이동 트리거

#### 📌 주요 메서드

* RegisterFinish(int actorNumber, float finishTime): 완주 순위/시간 기록

* RegisterDNF(int actorNumber, bool isStageFour): 탈락 처리 및 분기 관리

* FinalizeRaceAndMoveScene(string sceneName, bool force): 결과 확정 및 결과 씬 이동


### [ResultSceneManager.cs](Assets/_Project/_Scripts/Stage_Scripts/ResultSceneManager.cs)

#### 💡 역할

* 경기 결과 씬 전용 매니저

* RaceManager 결과 JSON 파싱

* 결과 UI 및 캐릭터 스폰 처리

* 일정 시간 후 로비 복귀

#### 📌 주요 메서드

* OnRoomPropertiesUpdate(Hashtable changed): 결과 데이터 변경 감지

* TryReadResults(): JSON 역직렬화 및 결과 UI 생성

#### 2.2.2 인게임 관전 시스템

### [SpectatorManager.cs](Assets/_Project/_Scripts/Stage_Scripts/SpectatorManager.cs)

#### 💡 역할

* 완주/탈락 후 관전 모드 전환 관리

* 관전 대상 순환 및 카메라 전환

* StageManager와 연계하여 메인 카메라 재지정

### 2.3 맵 시스템

### [CheckPoint.cs](Assets/_Project/_Scripts/Stage_Scripts/CheckPoint.cs)

#### 💡 역할

* 플레이어 진행 지점 저장

* 리스폰 시 복귀 위치 기준 제공

### [FinishLine.cs](Assets/_Project/_Scripts/Stage_Scripts/FinishLine.cs)

#### 💡 역할

* 플레이어 완주 판정 트리거

* 마스터 클라이언트에 완주 정보 신고

* 관전 모드 전환 처리

#### 📌 주요 메서드

* RPC_ReportFinish(int actorNumber, double finishTime): 마스터에서 완주 기록 처리

### [FlowObstacle.cs](Assets/_Project/_Scripts/Stage_Scripts/Obstacle/FlowObstacle.cs)

#### 💡 역할

* 지속적인 이동 힘을 주는 지형 장애물

### [WaterDoTween.cs](Assets/_Project/_Scripts/Stage_Scripts/Obstacle/WaterDoTween.cs)

#### 💡 역할

* DOTween을 활용한 맵 연출 오브젝트 제어

### 2.4 데이터 관리

### [StageInfo.cs](Assets/_Project/_Scripts/Stage_Scripts/StageInfo.cs)

#### 💡 역할

* 스테이지 표시 정보와 실행 정보 묶음

* UI, 룸 정보 동기화, 씬 로딩 기준 데이터

### [StageList.cs](Assets/_Project/_Scripts/Stage_Scripts/StageList.cs)

#### 💡 역할

* 여러 StageInfo를 배열로 관리하는 컨테이너

* 맵 선택 UI 및 룸 패널의 단일 참조 포인트


-----------------------------------------------------------
## 3. 플로우 차트 및 클래스 다이어그램

3.1 플로우차트

<img width="722" height="282" alt="Image" src="https://github.com/user-attachments/assets/74548f75-e67f-494d-bff0-8168de64e45f" />

--------------------------------------------------------------------------------------------------------------------
3.2 클래스 다이어그램


네트워크-다이어그램


<img width="499" height="306" alt="Image" src="https://github.com/user-attachments/assets/0a21f822-0659-4867-beb6-213904c1fb42" />

--------------------------------------------------------------------------------------------------------------------

<img width="669" height="305" alt="Image" src="https://github.com/user-attachments/assets/ad02e14c-0c34-43c2-b52a-f9ac4b96bafc" />

--------------------------------------------------------------------------------------------------------------------


인게임시스템-다이어그램


<img width="754" height="275" alt="Image" src="https://github.com/user-attachments/assets/0ad05927-cafb-41d0-ae90-be1d386a1ba4" />

--------------------------------------------------------------------------------------------------------------------

<img width="681" height="287" alt="Image" src="https://github.com/user-attachments/assets/e6737f7e-4dec-4f81-8ea1-a48cb3ff96d9" />

--------------------------------------------------------------------------------------------------------------------


맵시스템-다이어그램


<img width="528" height="306" alt="Image" src="https://github.com/user-attachments/assets/6813b325-9356-4b1f-a580-6b04c8bf6d6e" />

--------------------------------------------------------------------------------------------------------------------


데이터-다이어그램


<img width="474" height="275" alt="Image" src="https://github.com/user-attachments/assets/eeabae0d-7e78-48b6-90b4-5a8d14de423d" />

--------------------------------------------------------------------------------------------------------------------

## 4. 기술 스택
   
* C#
* Unity
* Fork + Github(형상 관리)

기술파트
* 포톤과 Json을 활용하여 결과 처리
* 포톤의 룸커스텀프로퍼티를 활용
-----------------------------------------------------------
