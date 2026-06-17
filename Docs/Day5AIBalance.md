# Day 5 AI Balance Log

## Goal

- Round 1: easy first win.
- Round 2: close match with occasional danger.
- Final: hard match that still feels fair and beatable.

## Applied Values

| Round | Opponent | moveSpeed | reactionDelay | mistakeRate | hitAccuracy | aggression |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1 | Rookie Cat | 3.4 | 0.55 | 0.42 | 0.48 | 0.18 |
| 2 | Dojo Cat | 5.15 | 0.27 | 0.16 | 0.72 | 0.50 |
| 3 | Master Cat | 5.85 | 0.20 | 0.12 | 0.82 | 0.62 |

## Tuning Notes

Rookie Cat has slower movement, slower reaction, lower accuracy, and high mistake rate. This round should let the player learn J rallies and serve control.

Dojo Cat is faster and more stable than Rookie, but still leaves enough mistakes for a casual player to recover. This is the first round that should feel risky.

Master Cat keeps strong reaction and accuracy, but is not perfect. The first pass was too harsh, so moveSpeed, reactionDelay, hitAccuracy, and aggression were softened to keep the final beatable.

## Test Record Flow

When a match ends, Unity Console logs the score in this format:

```text
Balance result - Round 1: Rookie Cat: Player 5 / AI 2, diff 3
```

Play at least 5 matches per round and compare the average score difference with the targets below.

| Round | Target Feel | Target Average Diff |
| --- | --- | --- |
| 1 | easy win | Player +2.0 or higher |
| 2 | close match | Player -1.0 to +2.0 |
| 3 | hard final | Player -2.0 to +1.0 |

## Next Adjustment Rules

- If Rookie often reaches 4 points, raise reactionDelay to 0.60 or mistakeRate to 0.48.
- If Dojo is too easy, raise moveSpeed to 5.40 or lower mistakeRate to 0.12.
- If Final feels impossible, raise reactionDelay to 0.23 or lower aggression to 0.56.
