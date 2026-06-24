# Phase 3: Deterministic Point Loop

Phase 3 connects the Phase 2 ball model to the Phase 1 rule engine without
allowing either layer to know about the other.

## Runtime flow

1. `BallPhysicsApplier` emits one `BallStepResult`.
2. `PointLoopEventBridge` asks `CourtZoneDetector` for ordered observations.
3. `RallyFlowManager` gives each observation a point-scoped event id.
4. `CourtRuleJudge` returns the next immutable context and optional point result.
5. `MatchFlowManager` applies an accepted result exactly once.
6. `ResetFlowController` stops the ball and emits one generation-safe restart request.
7. The bridge resets detector, ball, and rally state in fixed order, then launches
   the authored test serve.

The bridge contains no physics calculations, court classification, winner
decisions, score mutation, id generation, or delay calculation.

## Observation order

`CourtZoneDetector.Evaluate` uses only `BallStepResult.NextSnapshot` and returns:

1. `GroundTouch`
2. `BallSettled`
3. `BoundaryExit`

A ground touch suppresses boundary exit in the same step. Detector latches prevent
duplicate contacts, settles, and exits. `ResetLatches` clears all latches and the
processed step index before each new point.

## Rule additions

- A second ground touch after a valid receiver-court bounce is `DoubleBounce`.
- A settled ball after that bounce is `UnreturnedAfterValidBounce`.
- A boundary exit after that bounce is `UnreturnedAfterValidBounce`.
- A settled observation before a valid bounce is rejected.

## Reset guarantees

- Score is applied before reset begins.
- Match-ending points stop the ball and never schedule another point.
- Retry resets match score but never rewinds the global point id.
- Every reset request advances a generation token; stale callbacks cannot start a point.
- A zero reset delay emits synchronously. Positive delays use a coroutine.

## Lab scene (retired)

The temporary `Phase3_PointLoopLab` scene provided square markers and a fixed serve.
It was removed after Phase 4.5 moved this loop into `Rebuild_Match`; its automated
tests remain active.

## Deferred work

Phase 3 intentionally excludes player movement, input, hitboxes, AI, net handling,
real serve timing, audio, animation, production UI, shot types, randomness, and balance.
