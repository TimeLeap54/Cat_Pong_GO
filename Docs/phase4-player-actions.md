# Phase 4: Player Actions and Deterministic Hits

Phase 4 connects keyboard debug input to a fixed-tick player action model and the
Phase 3 rally loop. Correct zone contact never uses randomness.

## Responsibility flow

1. `PlayerInputReader` gathers keyboard edges in `Update` and exposes one consumed frame.
2. `PlayerActionStateMachine` independently tracks locomotion and swing state.
3. `PlayerCatController` applies movement and jump output to its `Rigidbody2D`.
4. `PlayerHitDetector` tests the current ball snapshot against an explicit local-space zone.
5. `ShotModel` deterministically resolves `SafeReturn` or `Smash` velocity.
6. `PointLoopEventBridge` registers the hit with the rule engine before launching the ball.

No player component decides winners or mutates score.

## State model

Locomotion is `Grounded` or `Airborne`. Swing state is independently `Ready`,
`Startup`, `Active`, or `Recovery` for Normal and Smash. This permits jumping and
swinging at the same time without multiplying combined enum states.

Swing timing is measured in fixed integer ticks. A swing id increments only when a
new swing is accepted. Recovery input is ignored and does not consume an id. K wins
when J and K are pressed together.

## Hit guarantees

- Only Active ticks may hit.
- Normal and Smash use separate authored zones.
- Zone boundaries are inclusive and evaluated without trigger callback ordering.
- Normal rejects balls behind the player.
- One swing id can create at most one request.
- The request captures the exact ball step index.
- Stale requests, rejected rule hits, and point-ending hits never launch the ball.
- Player body collision excludes the TennisBall layer through its own collider filter.

## Lab scene (retired)

The temporary `Phase4_PlayerHitLab` scene provided deterministic opponent serves
toward a controllable square player. It was removed after Phase 4.5 integrated the
same A/D, Space, J, and K controls into `Rebuild_Match`; its automated tests remain
active.

## Deferred work

Mobile input, AI returns, real serve timing, directional shot intent, hit quality,
random error, animations, audio, production UI, and balance remain outside Phase 4.
