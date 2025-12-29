# Don't Nudge Me
<img width="1194" height="663" alt="Image" src="https://github.com/user-attachments/assets/8c2e990f-8b7b-480e-9d14-6f149e5ab50c" />

#  Don't Nudge Me
[5인 개발 네트워크 프로젝트] 

[돈넛지미 플레이 영상](https://youtu.be/tT7jEDP2Mpo)

## 1. 프로젝트 개요

폴가이즈를 레퍼런스로 만든 3D 플랫포머 파티 게임입니다.

Unity를 활용하여 3D로 제작하였습니다

개발기간 : 2025.09.22 ~ 2025.10.14

## 2. 주요 기능
   
-----------------------------------------------------------
장우형 개발파트

### 2.1 네트워크

* 본 프로젝트는 Photon PUN을 기반으로 멀티플레이 환경을 구성하였으며,
룸 커스텀 프로퍼티 + RPC + PhotonNetwork 흐름 제어를 통해
로비, 맵 선택, UI, 게임 시작을 안정적으로 동기화합니다.

📌 MapChangePanel


🔗 Class

[MapChangePanel.cs](Assets/_Project/_Scripts/Main/MapChangePanel.cs)

#### 역할

* 방 내 스테이지(맵) 선택 UI

* 선택된 맵 정보를 Room Custom Properties에 저장

* 다른 클라이언트 UI를 RPC로 즉시 동기화

#### 적용된 네트워크 요소
▸ 룸 커스텀 프로퍼티

* 맵 관련 정보를 방 단위 상태 데이터로 저장

* Late Join 유저도 현재 선택된 맵 정보 자동 반영

▸ RPC (UI 즉시 반영용)

* 현재 접속자 + 이후 접속자 모두 동일 UI 유지

▸ PhotonNetwork 조건 처리

📌 RoomPanel


🔗 Class

[RoomPanel.cs](Assets/_Project/_Scripts/Main/RoomPanel.cs)

#### 역할

* 로비(대기방) UI 전반 관리

* 룸 정보, 플레이어 리스트, 준비 상태, 맵 정보 표시

* Room Custom Properties 변경을 실시간 반영

#### 적용된 네트워크 요소
▸ Photon 콜백 기반 동기화

* Photon 콜백을 통해 룸 상태 변경 감지

* 맵 이미지, 맵 이름 변경 시 자동 UI 갱신

▸ 룸 커스텀 프로퍼티 반영

* MapChangePanel에서 저장한 값을 기준으로 UI 동기화

▸ 플레이어 준비 상태 

* 각 플레이어의 준비 상태를 개별 프로퍼티로 관리

* 마스터 클라이언트가 시작 가능 여부 판단

* 방 공개 여부, 게임 시작 버튼 제어는 마스터만 가능

* 권한 충돌 방지

▸ RPC 기반 보조 동기화

* Room Custom Properties와 병행

* 즉각적인 UI 반영 용도

### 2.2 인게임 시스템

#### 2.2.1 인게임 플로우

📌 InGame Flow Manager

🔗 Class

[StageManager.cs](Assets/_Project/_Scripts/Stage Scripts/StageManager.cs)

[RaceManager.cs](Assets/_Project/_Scripts/Stage Scripts/RaceManager.cs)

[ResultSceneManager.cs](Assets/_Project/_Scripts/Stage Scripts/ResultSceneManager.cs)

📌 StageManager

🔗 Class

[StageManager.cs](Assets/_Project/_Scripts/Stage Scripts/StageManager.cs)

역할

스테이지 단위 경기 흐름 총괄 관리

카메라 인트로 → 플레이어 스폰 → 게임 타이머 시작

제한 시간 및 완주 인원 기준으로 경기 종료 조건 판단

핵심 책임

플레이어 스폰 위치 결정 (ActorNumber + 체크포인트 기준)

게임 제한 시간 관리

완주 인원 수 UI 갱신

마스터 클라이언트 기준 경기 종료 트리거

인게임 흐름 요약

스테이지 진입 시 모든 플레이어 체크포인트 초기화

(비테스트 모드)

카메라 인트로 연출 수행

연출 종료 후 플레이어 스폰

일정 시간 후 게임 타이머 시작

Update 루프에서 경기 종료 조건 체크 (마스터 전용)

제한 시간 초과

완주 인원 ≥ 통과 인원(passPlayer)

📌 RaceManager

🔗 Class
RaceManager.cs

역할

경기 전반의 결과 데이터 관리 서버 역할

완주/탈락(DNF) 기록 수집

결과 데이터를 Room Custom Properties에 저장

ResultScene으로 씬 전환

핵심 책임

완주 순서 및 완주 시간 기록

DNF 처리 및 StageFour 전용 탈락 이벤트 처리

결과 JSON 직렬화 및 룸 프로퍼티 저장

마스터 클라이언트 단일 판정 구조 유지

네트워크 핵심 포인트

마스터 클라이언트 전용 기록

FinishedCount → UI 및 StageManager와 연계

RaceResultsJson → ResultSceneManager에서 소비

📌 ResultSceneManager

🔗 Class
ResultSceneManager.cs

역할

경기 결과 씬 전용 매니저

RaceManager가 남긴 결과 데이터를 기반으로

결과 UI 생성

캐릭터 스폰 (포디움 / 생존·탈락)

핵심 책임

Room Custom Properties 변경 감지

결과 JSON 파싱

스테이지 타입에 따른 UI 분기 처리

일정 시간 후 로비(MainScene) 복귀

결과 처리 흐름

RaceResultsJson 감지

결과 데이터 파싱

스테이지 타입 판별

일반 스테이지 → 랭킹 / 포디움

StageFour → 생존 / 탈락

결과 UI 및 캐릭터 표시

일정 시간 후 RaceManager 초기화 및 로비 이동

2.2.2 인게임 관전 시스템

📌 Spectator System


🔗 Class
SpectatorManager.cs

역할

경기 중 또는 완주 후 관전 모드 전환 관리

플레이어 카메라를 순차적으로 전환

현재 관전 타겟에 따라 메인 카메라 재지정

핵심 기능
▸ 관전 대상 등록 / 제거
AddTarget(Transform target)
RemoveTarget(Transform target)


플레이어 스폰 시 자동 등록

파괴된 오브젝트는 자동 정리

▸ 관전 모드 진입
EnterSpectatorMode()


관전 상태 진입 시 첫 타겟 설정

이후 Tab 키로 관전 대상 순환

▸ 관전 대상 전환
NextTarget()


Tab 입력 시 다음 플레이어로 카메라 전환

현재 타겟의 카메라만 활성화

▸ StageManager 연동
stageManager.mainCamera = currentTargetCamera;


관전 중인 카메라를 인게임 메인 카메라로 재지정

StageManager / StageFourManager 공통 인터페이스 사용

2.3 맵 시스템 
* 장애물 설정
* 체크포인트
* 결승선

2.4 데이터 
* 맵 정보 S/O 화
* Json 활용

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
