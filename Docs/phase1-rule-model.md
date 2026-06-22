# Phase 1 Rule Model

`CourtRuleJudge` is a pure state machine. It receives an immutable `RallyContext` and one ordered `RuleEvent`, then returns a `RuleTransition`. It never moves the ball or changes score.

## Terminal Rules

- A repeated hit by the last hitter loses by `DoubleTouch`.
- A first ground touch on the receiver court is legal.
- A first ground touch on the hitter court loses by `FailedToClear`.
- Ground out or boundary exit before a valid receiver bounce loses by `OutBeforeValidBounce`.
- Any second ground touch after a valid receiver bounce loses by `DoubleBounce`.
- Boundary exit after a valid receiver bounce loses by `UnreturnedAfterValidBounce`.
- `NetContact` records contact and never ends a point.
- `NetStopped` makes the last hitter lose before a valid bounce, but makes the receiver lose after one.

## Integration Contract

- Serve toss events never enter the rule engine. The first approved serve hit is a normal `Hit`.
- An out-ground contact emits only `GroundTouch(Out)`.
- An airborne exit emits only `BoundaryExit`.
- Ground contact takes precedence over `BoundaryExit` and `NetStopped` in the same physics step.
- Only an approved paw or smash contact emits `Hit`; body collisions do not.
- `RallyFlowManager` must issue monotonically increasing point and event IDs.
- Only `ScoreSystem` may mutate score.
