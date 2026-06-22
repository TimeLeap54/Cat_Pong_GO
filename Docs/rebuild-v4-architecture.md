# Cat Tennis V4 Rebuild Architecture

## Purpose

This branch is a clean script-side foundation. It intentionally preserves only the project configuration, UI art, Galmuri fonts, TextMesh Pro resources, and three PCM sound effects. No legacy gameplay script, generated prefab, scene, collider setup, or balancing value is reused.

## Dependency Direction

Dependencies must point inward toward data and rules:

1. `State` and immutable request/result types contain data only.
2. `Config` contains authored tuning data only.
3. `Shot` and `Rules` calculate results without scene, UI, animation, or audio ownership.
4. `Ball` and `Cat` adapt Unity components to domain requests and approved results.
5. `Flow` coordinates systems but does not calculate physics or duplicate rules.
6. `Feedback`, `UI`, `Audio`, and `Debug` observe semantic events and never decide gameplay outcomes.

## Non-Negotiable Ownership

- Ball motion is applied only by `BallPhysicsApplier`.
- Point outcomes are decided only by `CourtRuleJudge`.
- Score state is changed only by `ScoreSystem`.
- Match and rally transitions are coordinated only by the matching flow manager.
- Animation, UI, audio, and feedback cannot mutate rules or physics.
- Input adapters cannot apply movement or shots directly.
- Runtime tuning values belong in config assets, not duplicated component fields.

## Rebuild Sequence

1. Define state, requests, results, and tests.
2. Implement deterministic ball motion in isolation.
3. Implement court observations and one authoritative rule judge.
4. Add rally and reset flow.
5. Add player input, movement, and hit detection.
6. Add shot resolution and opponent decisions.
7. Connect animation and feedback.
8. Connect the preserved UI and three sound effects.
9. Add balancing data only after the complete loop is stable.

## Current Branch State

All V4 scripts are deliberately empty compatibility-free shells. Neither rebuild scene has a custom gameplay component attached. This keeps the first commit compilable and makes every later dependency explicit.
