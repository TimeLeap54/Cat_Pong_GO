# Phase 4.5: Production Match QA Integration

Phase 4.5 moves the Phase 1-4 runtime out of per-phase lab scenes and into the
production `Rebuild_Match` scene.

## Production scenes

- `Rebuild_MainMenu`: minimal Start navigation for QA.
- `Rebuild_Match`: visible court, ground, net, player, opponent, ball, score/debug
  controls, and the complete deterministic point loop.

Only these scenes are enabled in Build Settings. Phase 3 and Phase 4 lab scenes,
their generators, and their dedicated config assets were removed after integration
tests passed.

## Composition and validation

`MatchBootstrapper` is the only production composition root. It validates configs,
injects `groundY`, initializes player and ball adapters, connects event bridges, and
starts the first point in a fixed order. It contains no scoring or physics formulas.

`MatchSceneValidator` rejects duplicate managers, missing references, incorrect
Rigidbody settings, layer mismatches, player/ball collision, lab config references,
and a ground collider whose physical top differs from `CourtGeometryConfig.GroundY`.

## Runtime lifecycle

`PointLifecycleController` guards these execution states:

`Uninitialized -> StartingPoint -> RallyActive -> ResetPending -> MatchEnded`

Retry cancels the previous reset generation, resets match score, starts a fresh
global point id, resets the player to Grounded/Ready, and launches one new ball.

## Physics ownership

- Player: Dynamic Rigidbody2D with Unity gravity and frozen rotation.
- Ball: Kinematic Rigidbody2D with zero Unity gravity.
- Ball gravity and bounce: Phase 2 deterministic model only.
- Player collider excludes the TennisBall layer.
- Ball collider is a trigger and cannot physically bounce from Ground or Player.
- Net is visual-only until a later net observation phase.

## QA coverage

EditMode validates contracts, production scene composition, build settings, and
three immediate retry cycles. PlayMode runs three complete five-point matches,
retries between them, returns to MainMenu during the flow, starts Match again, and
verifies fresh score and lifecycle state. Unexpected warnings and errors fail the
test run.
