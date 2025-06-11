# UpTwo
---
인간 대신 인공지능과 기계들이 가득한 도시.

“도시 가장 꼭대기, 하늘을 찌르는 타워의 끝에… 마지막 우주선이 있다.”

꼭대기로 올라가 도시를 탈출하라!

### 프로젝트 개요
---
* **프로젝트명** : UpTwo
* **개발 기간** : 2025-03-05 ~ 2025-06-10
* **개발 엔진 및 언어** : Unity & C#
* **장르** : 2인 협동 2.5D Platformer

### 프로젝트 팀원
---
* **윤승언** : Unity 기반 게임 클라이언트 개발
* **유석** : 2인 협동 게임 Server 개발
* **정지은** : 게임 아트 컨셉 기획 및 디자인

### 주요 기능
---
* **3D지만 2D 같은**
  
  플레이어는 카메라 회전을 통해 시점을 90°로 전환할 수 있습니다. 이를 통해 특정 시점에서는 보이지 않던 길이 다른 시점에서 보이게 됩니다. 특정 발판이나 구조물을 이용해 위로 올라갈 수 있는 길을 탐색해서 끝까지 올라가세요!
  <img style="height: 100%; width: 49%; float:left;" src="https://github.com/user-attachments/assets/cbea6185-f005-4b5f-8a25-4b980e5e6b06" />
  <img style="height: 100%; width: 49%; float:right;" src="https://github.com/user-attachments/assets/a3ada043-8517-4eda-a501-ea3d06486929" />


* **친구와 함께**

  본 게임은 2인 협동 플레이를 제공합니다. 매치메이킹이나 방을 생성하여 스테이지를 시작할 수 있습니다. 자신에게 아이템을 사용하거나 상대에게 체력을 나누어주면서 함께 길을 탐색하고 끝까지 올라가세요! 2명이 함께 꼭대기에 올라야 스테이지가 클리어됩니다.
  ![Image](https://github.com/user-attachments/assets/bad9be12-efda-4ff4-a030-c5d49eac6c3d)

* **몬스터 주의!**
  
  플레이를 하다보면 플레이어를 공격하는 몬스터를 마주칠 수 있습니다. 몬스터와 접촉하면 데미지를 입으면서 뒤로 튕겨지기 때문에 플레이어는 몬스터를 피하거나 처치해서 이동합니다. 몬스터 처치 시 일정량의 재화를 획득할 수 있습니다.
  <img src="https://github.com/user-attachments/assets/da293568-7553-4fde-87b2-1d268df6d979" />

* **게임 클리어를 위한 업그레이드**
  
  플레이를 통해 재화가 모였다면 상점에서 플레이에 도움을 주는 아이템을 구매할 수 있습니다. 구매한 아이템은 다음 플레이부터 사용할 수 있습니다. 적절한 아이템 사용을 통해 끝까지 올라가세요!
  ![Image](https://github.com/user-attachments/assets/5db6b6c8-bf89-4bb2-bf99-71f3d0b51681)
  
### 기술 스택
---
<img src="https://github.com/user-attachments/assets/66fa0793-e330-444e-a69d-12ffcedf5ffe">

### 📁 프로젝트 폴더 구조
---
```
ProjectRoot/
├── Assets/ # Unity 프로젝트 주요 리소스
│ ├── Animation/ # 애니메이션 리소스
│ ├── GoldCoin/ # 골드 코인 관련 리소스
│ ├── Materials/ # 머티리얼 리소스
│ ├── Photon/ # Photon 네트워킹 리소스
│ ├── PlayFabEditorExtensions/ # PlayFab 에디터 확장 기능
│ ├── PlayFabSDK/ # PlayFab SDK
│ ├── Prefabs/ # 프리팹
│ ├── Resources/ # 프리팹2
│ ├── Rolling_Balls-Sci-fi_Pack/ # 무료 에셋
│ ├── Scenes/ # Unity 씬 파일
│ ├── Scripts/ # 게임 로직 스크립트
│ ├── Settings/ # 게임 설정 관련 파일
│ ├── Sprites/ # 스프라이트 이미지
│ ├── TextMesh Pro/ # TextMesh Pro 관련 리소스
│ ├── TutorialInfo/ # Unity 튜토리얼 정보
│ └── UI Images/ # UI에 사용되는 이미지
├── Packages/ # Unity 패키지 정의
│ └── com.unity.multiplayer.tools/ # Unity Multiplayer Tools 패키지
├── ProjectSettings/ # Unity 프로젝트 설정 (에디터 환경, 입력 설정 등)
├── .gitignore # Git 무시할 파일 목록
├── .vsconfig # Visual Studio 환경 설정
└── README.md # 프로젝트 설명 문서
```

### 플레이 방법
---
* **플레이어 이동** : AWSD or 방향키(←↑↓→)
* **플레이어 점프** : Space bar
* **플레이어 공격** : 마우스 좌클릭
* **상대 플레이어 회복** : F
* **아이템 줍기** : Z
* **카메라 회전** : Q(시계방향), E(반시계방향)
* **버튼 클릭** : 마우스 좌클릭
* **아이템 사용** : <img src="https://github.com/user-attachments/assets/bd8fb9d6-9f66-41d4-9128-9c89f95c9877">
* **이모티콘** : <img src="https://github.com/user-attachments/assets/b8a0e993-da85-4218-9600-45a570d24945">
* **로비에서 원하는 문으로 이동** : W or ↑
  
### 관련 링크
---
* **프로젝트 개발 과정** :
  * 프로젝트 제안서 : https://handy-mango-559.notion.site/1b0461ec02208014bb7dd3dd8736dd18
  * 프로젝트 백로그 : https://handy-mango-559.notion.site/1b5461ec022080cd96b4e313991fcdae?v=1b5461ec022080e58931000c51fe27f8
  * 기술 스택 및 아키텍처 다이어그램 : https://handy-mango-559.notion.site/1bf461ec02208096b990d3ca6724e0f9?pvs=74
  * 스프린트 1 회고록 : https://handy-mango-559.notion.site/_01-1ce461ec02208044912fec236ffc600e?pvs=74
  * 스프린트 2 회고록 : https://handy-mango-559.notion.site/_02-1db461ec022080f29660e8a0c8fdce25?pvs=74
  * 스프린트 3 회고록 : https://handy-mango-559.notion.site/_03-1eb461ec022080fab624ce68d028a92b?pvs=74
  * 스프린트 4 회고록 : https://aeolian-trout-620.notion.site/_04-200f922e1a7a807193aef50d43a23452?pvs=74
  * 최종 프로젝트 보고서 :
* **Demo Video** : https://youtu.be/rlQRbWROKNg
