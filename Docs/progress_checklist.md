# 진행 체크리스트

## 현재 구현 상태

- [x] Unity 2D 프로젝트 구조 생성
- [x] 2D URP 프로젝트 설정
- [x] `Assets/Scripts`, `Assets/Editor`, `Docs`, `ProjectSettings`, `Packages` 구성
- [x] Match 씬 자동 생성
- [x] MainMenu 씬 자동 생성
- [x] 코트, 네트, 플레이어, AI, 공 배치
- [x] 플레이어 좌우 이동
- [x] 점프
- [x] 스윙 입력
- [x] 스윙 범위 공 반사
- [x] 공 `Rigidbody2D` 이동
- [x] 벽/바닥/네트 Collider 설정
- [x] 네트 충돌 감속 반사
- [x] 좌우 코트 바닥 득점 판정
- [x] 화면 밖 공 처리
- [x] 5점제 승리/패배
- [x] 점수 UI
- [x] AI 이동
- [x] AI 스윙 판정
- [x] AI 난이도 파라미터
- [x] 3라운드 토너먼트
- [x] 승리 시 다음 라운드
- [x] 결승 승리 시 우승 화면
- [x] 패배 시 게임오버
- [x] Day 4 공 물리와 득점 규칙 안정화
- [x] Day 5 AI 난이도와 추적 밸런싱
- [x] 연속형 J 힘·각도 조절
- [x] K 3회 성공 강스파이크
- [x] MainMenu 시작 버튼과 결과 흐름
- [x] 플레이어·AI 스프라이트 애니메이션
- [x] 코트 배경, 공, 네트 아트 적용
- [x] 젤리 타격·득점·버튼·승패 효과음
- [x] Player / Opponent / Ball / Net / CourtBackground 프리팹

## 다음 우선순위

- [ ] 실제 Play 모드에서 5판 연속 테스트
- [x] 공 속도 상한 추가
- [x] 공 끼임 방지 보정
- [x] 득점 중복 방지 코드 적용
- [x] 스윙 범위/쿨타임 조정
- [x] 점프 높이와 이동속도 조정
- [x] 공 반사 각도 조정
- [x] Round 1~3 AI 밸런싱 기록
- [x] 사운드 에셋 추가
- [x] 고양이/코트 이미지 에셋 교체
- [ ] Day 6 알파 실제 Play 모드에서 5판 연속 테스트
- [ ] WebGL Build Support 설치
- [ ] WebGL 빌드
- [ ] itch.io 업로드

## 테스트 기록

| 날짜 | 빌드/상태 | 테스트 내용 | 결과 | 다음 수정 |
| --- | --- | --- | --- | --- |
| 2026-06-16 | MVP scene generation | Unity batchmode scene generation | 성공 | WebGL 모듈 설치 후 빌드 |
| 2026-06-16 | Day 1 | Match 씬, 플레이어, AI, 공, 바닥, 네트, 벽 Collider 생성 확인 | 성공 | Day 2 스윙/득점 Play 모드 테스트 |
| 2026-06-16 | 2D URP fix | 프로젝트 기본 모드 2D, URP Asset, 2D Renderer, 씬 재생성 확인 | 성공 | Unity에서 `Assets/Scenes/MainMenu.unity` 직접 열어 Play 확인 |
