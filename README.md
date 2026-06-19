# CatPong

Unity 2022.3.62f3 기반 2D URP 사이드뷰 테니스/핑퐁 MVP입니다.

## Day 1

- 2D URP 프로젝트 확인
- `Assets/Scenes/Match.unity` 생성
- 코트, 네트, 플레이어, 상대, 공 배치
- 보이지 않는 상/좌/우 경계 Collider 배치
- `PlayerController.cs` 좌우 이동 구현
- `BallController.cs` 공 초기 이동 구현

## Day 2

- 점프 구현
- `J` 또는 마우스 클릭 스윙 입력 구현
- 스윙 범위 안 공 반사 구현
- 좌우 코트 바닥 득점 Zone 구현
- `MatchManager.cs` 생성
- 5점제 승리/패배 구현
- 공 리셋 구현
- 점수 UI 텍스트 연결

## Day 3

- `OpponentAI.cs` 생성
- AI 공 추적 이동 구현
- AI 스윙 판정 구현
- Rookie / Dojo / Master 난이도 파라미터 추가
- `TournamentManager.cs` 생성
- Round 1, Round 2, Final 구성
- 승리 시 다음 라운드 이동
- 패배 시 게임오버
- 결승 승리 시 우승 화면
- 바닥 득점 Zone 범위 보정

## Day 4

- 공 속도 제한과 끼임 복구
- 아웃라인, 벽 충돌, 투 터치 득점 규칙
- 이동·점프·스윙 판정 안정화

## Day 5

- Rookie / Dojo / Master AI 밸런싱
- AI 깊은 로브 추적과 점프 수비
- J 홀드 시간과 W/S 입력을 이용한 연속 힘·각도 조절
- K 연속 성공 타격 기반 강스파이크

## Day 6 Alpha

- MainMenu / Match / 결과 / 재시작 흐름
- 플레이어와 AI 스프라이트 애니메이션
- 코트 배경, 공, 네트 아트 적용
- 젤리 타격 효과음과 벽 스텝 효과음
- Player / Opponent / Ball / Net / CourtBackground 프리팹

## 실행

1. Unity Hub에서 `C:\Users\minil\GameMaking\CatPong`을 엽니다.
2. `Assets/Scenes/Match.unity`를 엽니다.
3. Play를 누릅니다.

## 조작

- `A/D` 또는 방향키: 일정한 속도의 좌우 이동
- `A/A`, `D/D` 빠르게 두 번 또는 `Shift + A/D`: 뒤·앞 대시
- `Space`: 1회 점프, 외곽 벽에서 벽 스텝
- `J` 또는 마우스 클릭: 홀드 시간으로 타격 힘 조절
- `J + W/S`: 높은 로브와 낮은 샷 사이의 각도 조절
- `K`: 스매시, 6초 안에 세 번 성공하면 세 번째 강스파이크

## 문서

- [게임 기획서](Docs/game_design_v0.1.md)
- [14일 개발 일정](Docs/development_schedule_v0.1.md)
- [진행 체크리스트](Docs/progress_checklist.md)
- [Git 마일스톤과 복습 방법](Docs/milestones.md)
- [Day 5 AI 밸런스](Docs/Day5AIBalance.md)
