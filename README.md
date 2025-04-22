![image](https://github.com/user-attachments/assets/0c5b6b45-6ca1-4276-af48-e7e9a12824b5) <br>
> 누구나 쉽게 즐기는 귀여운 캐릭터들의 전략 스킬 배틀로얄 게임 **Smash Up!**

> [게임 소개 영상](https://youtu.be/YDjhizPPxTc) <br>
> [협동전 - 용의 둥지 업데이트 영상](https://youtu.be/K3M_rw6I-HU) <br>
> [협동전 - 심해의 괴물 업데이트 영상](https://youtu.be/wsJfSZi5GUk)

# **1. 프로젝트 개요**
![image](https://github.com/user-attachments/assets/e18ca190-3ae3-479b-825d-8566c4ce38cf)
<Smash Up!>은 빠르고 전략적인 전투가 펼쳐지는 쿼터뷰 액션 배틀 게임입니다.
개인전, 팀전, 보스 레이드 협동전까지 다양한 모드에서 실력을 겨루며, 매 라운드마다 제공되는 강화 카드로 나만의 캐릭터를 만들어가는 재미를 제공합니다.

**게임 플레이 흐름**

1. **라운드 진행**
- 게임은 총 3라운드로 구성되며, 매 라운드가 끝나면 생존 여부, 킬 수, 낙사 유도 킬, 딜량 등을 종합해 점수가 계산됩니다.
- 스코어보드에 순위가 표시되고, 최종 라운드 종료 시 MVP 플레이어가 선정됩니다.

2. **강화 카드 선택 시스템**
- 각 라운드 시작 시, 3장의 강화 카드가 제시되며 각각 한 번씩 리롤(다시 뽑기)이 가능합니다.
- 카드 효과는 공격력/체력 같은 기본 스탯부터, 스킬 데미지·쿨타임·사정거리·범위·속도 강화 등 다양하게 구성되어 있어 매 라운드마다 전략적인 빌드업이 가능합니다.

3. **다양한 모드 지원**
- 개인전: 모든 플레이어가 적이 되는 생존 경쟁
- 팀전: 전략적인 팀 구성과 협력 플레이
- 레이드 모드: 거대한 보스를 상대로 팀원이 힘을 모아 협동 전투 수행

4. **캐릭터 & 맵 구성**
- 총 5종의 개성 있는 캐릭터, 각각 1개의 이동 스킬, 3개의 기본 스킬, 3개의 강화 스킬 보유
- 개인전/팀전 맵 4종, 보스 레이드 맵 2종 제공


# 2. 개발 기간
- 25.02.24~25.04.11 (7주)

# 3. 시스템 아키텍쳐
![image](https://github.com/user-attachments/assets/3fec87bd-3e96-44f7-b61b-e1efa87b5746)

# 4. 기술 스택
- 클라이언트 : Unity C#
- 네트워크 : Mirror(Unity), AWS EC2, Kubernetes(K8s), ArgoCD
- 서버 : C++ (Boost), PostgreSQL, Spring Boot
- 로그/분석 : Kafka, Logstash, ElasticSearch, Kibana
- 협업 툴 : GitLab, Jira

# 5. 멤버 구성
- 팀장 : 구본관 - Unity 클라이언트 + Mirror 데디케이트 서버 구현 및 네트워크 동기화
- 팀원 : 이승록 - 서버와 클라이언트 연결(소켓서버와 로그서버 연결)
- 팀원 : 김승우 - 사용자의 플레이 흐름을 고려한 UI/UX 기획 및 디자인
- 팀원 : 전상혁 - Boost.asio 서버와 PostgreSQL로 사용자 인증 및 매칭 서버 구현
- 팀원 : 손정찬 - Kafka, LogStash, elasticSearch, Kibana로 로그 수집 및 시각화 + 인프라 관리
- 팀원 : 김성일 - 전이 행렬 기반 통계 분석으로 강화 카드 추천 알고리즘 설계 및 시스템 개발

# 6. 주요 기능

### 6.1 로그인 및 사용자 인증
![image](https://github.com/user-attachments/assets/0c5b6b45-6ca1-4276-af48-e7e9a12824b5)
- **SSAFY OAuth2 로그인**, **Google 계정 로그인**, **LAN 모드 접속**을 지원합니다.
- 로그인 성공 시 세션 토큰을 발급받고, 자동으로 온라인 UI로 전환됩니다.

---

### 6.2 방 만들기 및 참가
<p align="center">
  <img src="https://github.com/user-attachments/assets/15d5e022-a003-400e-9f64-39349ae96113" width="45%" />
  <img src="https://github.com/user-attachments/assets/7bf750f0-0ab9-48f9-b245-26aa6270b4cd" width="45%" />
</p>

- 게임 내에서 방을 생성하거나 참가할 수 있는 기능을 제공합니다.  
- 개인전 / 팀전 모드, 최대 인원 수 설정이 가능합니다.  
- 방에 입장하면 Mirror 기반의 네트워크 로비 UI로 전환됩니다.

---

### 6.3 캐릭터 선택 및 스킬 테스트
<p align="center">
  <img src="https://github.com/user-attachments/assets/540cf787-5e66-489e-bd8b-873acb158240" width="45%" />
  <img src="https://github.com/user-attachments/assets/9d3899cf-5385-4541-8998-c37c813c2190" width="45%" />
</p>

- 대기방에서 총 5종의 캐릭터 중 자유롭게 선택할 수 있습니다.  
- 각 캐릭터는 3개의 기본 스킬과 1개의 아이템 스킬을 사용할 수 있으며, 테스트 공간에서 자유롭게 실험할 수 있습니다.  
- 사망하더라도 부활이 가능하여 게임 시작 전까지 원하는 캐릭터를 자유롭게 체험해볼 수 있습니다.

---

### 6.4 유령 시스템
![image](https://github.com/user-attachments/assets/ab16c23d-d46d-4370-8e23-fdcd39ee79c0)

- 플레이어가 사망하면 유령 상태로 전환되어 게임에 계속 관여할 수 있습니다.  
- 유령 상태에서는 살아있는 플레이어를 밀쳐내는 물리적 상호작용이 가능하며, 마지막까지 몰입도 있는 플레이가 가능하도록 구성했습니다.

---

### 6.5 강화 카드 시스템
![image](https://github.com/user-attachments/assets/52746f5f-0615-4a85-b86c-b5d8c6f50e3a)

- 매 라운드 시작 시 3장의 강화 카드를 무작위로 획득하며, 각 카드는 1회 리롤할 수 있습니다.  
- 총 3라운드에 걸쳐 캐릭터의 스탯 및 스킬을 점진적으로 강화할 수 있습니다.  
- 카드 효과는 공격력, 체력, 쿨타임 감소, 데미지 증가, 사거리 증가, 투사체 속도 향상 등 다양하며, 일부 카드는 스킬을 고유한 강화 스킬로 교체할 수 있습니다.

---

### 6.6 점수 및 순위 시스템
<p align="center">
  <img src="https://github.com/user-attachments/assets/48d32bc7-9a73-4575-b50c-1ce20de2ca0c" width="45%" />
  <img src="https://github.com/user-attachments/assets/666442d3-077e-458e-9970-50200685c946" width="45%" />
</p>

- 각 라운드 종료 시 킬 수, 낙사킬, 딜량, 생존 시간을 기반으로 점수를 계산합니다.  
- 누적 점수를 기준으로 개인전 / 팀전 순위를 산정하며, 결과 화면을 제공합니다.  
- 최종 MVP를 선정하여 화면에 표시하는 기능도 포함되어 있습니다.

---

### 6.7 보스 레이드 (협동전)
<p align="center">
  <img src="https://github.com/user-attachments/assets/abc0d8da-486d-4f47-aae4-4112c724361f" width="45%" />
  <img src="https://github.com/user-attachments/assets/4b300428-77dc-4c91-8e6a-6dd205d8fb0b" width="45%" />
</p>

- 협동전 모드에서는 보스를 상대하는 레이드 전투를 플레이할 수 있습니다.  
- 팀원들과 협력하여 보스의 공격 패턴을 분석하고 전략적으로 대응해야 합니다.  
- 보스는 일정 주기로 일반 공격 외에도 맵 기믹을 활용한 강력한 패턴을 사용합니다.

---

