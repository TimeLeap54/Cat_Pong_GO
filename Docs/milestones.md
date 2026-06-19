# Git 마일스톤 안내

각 Day 브랜치는 해당 작업일의 마지막 안정 상태를 보존한다. 세부 시행착오는 브랜치 사이의 커밋 기록에서 순서대로 확인할 수 있다.

| 단계 | 브랜치 | 태그 | 주요 상태 |
| --- | --- | --- | --- |
| Day 1 | `catpong-day1` | `day1-complete` | 이동, 공 물리, 코트 충돌 |
| Day 2 | `catpong-day2` | `day2-complete` | 점프, 스윙, 득점, 5점제 |
| Day 3 | `catpong-day3` | `day3-stable` | AI 토너먼트와 깊은 공 수비 안정화 |
| Day 4 | `catpong-day4` | `day4-complete` | 공 물리, 아웃, 투 터치 안정화 |
| Day 5 | `catpong-day5` | `day5-complete` | AI 난이도와 J 스윙 밸런싱 |
| Day 6 | `catpong-day6` | `day6-alpha` | 메뉴, 아트, 애니메이션, 사운드, 느린 템포 알파 |

`main`은 현재 플레이 가능한 최신 안정본을 가리킨다. 프로젝트 재생성 전의 구 `main`은 `archive/pre-rebuild-main`에 보관한다.

## 복습 방법

1. Day 브랜치끼리 비교해 하루 단위 결과를 확인한다.
2. 두 마일스톤 사이의 커밋을 오래된 순서로 읽어 문제와 수정 흐름을 확인한다.
3. 배포 가능한 시점은 브랜치보다 태그를 기준으로 체크아웃한다.

예시:

```powershell
git log --reverse day4-complete..day5-complete
git diff day5-complete..day6-alpha -- Assets/Scripts
```
