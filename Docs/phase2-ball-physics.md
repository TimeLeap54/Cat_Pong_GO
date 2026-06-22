# Phase 2 Ball Physics

`CatTennis.BallPhysics.Core` is a deterministic, fixed-step ball simulation with no Unity or gameplay-rule dependency.

## Fixed Step

1. Validate settings, snapshot, and step input.
2. Return an inactive snapshot unchanged.
3. Apply gravity.
4. Clamp horizontal, rise, and fall velocity.
5. Integrate position.
6. Correct ground penetration.
7. Bounce or settle on downward ground contact.
8. Clamp post-bounce velocity and increment the step index.

## Ground Contract

- Downward contact uses `ballBottom <= groundY + groundSkin`.
- Correction always places the center at `groundY + ballRadius`.
- Upward penetration corrects position without bouncing or changing velocity.
- Upward skin proximity without penetration is ignored.
- `impactSpeed` is the absolute pre-bounce downward speed, including settle results.
- `contactY` is meaningful only when `hadGroundContact` is true.

## Ownership

- `BallPhysicsModel` calculates motion only.
- `BallStateTracker` owns previous and current immutable snapshots.
- `BallPhysicsApplier` mirrors snapshots to a kinematic Rigidbody2D and never reads Rigidbody velocity back into the model.
- `BallController` exposes reset, launch, and stop commands only.
- Net, court side, out, score, serve, shot type, spin, and randomness remain outside Phase 2.
