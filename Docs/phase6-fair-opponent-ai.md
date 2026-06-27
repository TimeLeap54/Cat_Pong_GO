# Phase 6 Fair Opponent AI

Phase 6 completes the five-point match with an opponent that uses the same movement,
hit-zone, shot model, rule registration, and ball launch path as the player.

## Fairness boundary

- `DelayedBallObserver` is the AI's only observation source.
- `BallArrivalPredictor` returns time-ordered physical candidates using the Phase 2 model.
- `AIInterceptPlanner` may only choose candidates reachable at configured movement speed.
- `AISwingPlan` schedules movement, jump, and swing; it never authorizes a hit.
- `OpponentHitDetector` creates `HitContact` only during an active swing with the live ball
  inside the shared hit zone.
- `ShotExecutionController` validates point, step, hitter+swing identity, trajectory, and
  rule registration before launching the ball.

The opponent never writes ball position or velocity, never teleports, and can miss an
unreachable shot normally.

## Difficulty

Rookie, Dojo, and Master assets vary movement speed, reaction delay, and deterministic
shot-selection weights. They do not change hit zones, ball physics, or tennis rules.

## Reset

Every point reset clears delayed observations, arrival candidates, movement target,
jump/swing plan, consumed contact, action state, and Rigidbody velocity before the next
serve begins.
