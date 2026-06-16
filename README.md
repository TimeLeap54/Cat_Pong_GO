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

## 실행

1. Unity Hub에서 `C:\Users\minil\GameMaking\CatPong`을 엽니다.
2. `Assets/Scenes/Match.unity`를 엽니다.
3. Play를 누릅니다.

## 조작

- `A/D` 또는 방향키: 좌우 이동
- 서브 전 `WASD`: 손 앞 공 위치 조정
- `Space`: 점프
- `J` 또는 마우스 클릭: 아래에서 위로 치는 서브/스윙
- `K`: 위에서 아래로 찍는 서브/스윙
