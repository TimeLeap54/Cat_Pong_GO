# Phase 5 Shot and Serve

Phase 5 adds deterministic player shot intent and a two-step player serve while keeping
the Phase 1 rule judge and Phase 2 ball physics unchanged.

## Shot pipeline

Input intent is captured at swing startup. Ball position, velocity, and step index are
captured only when the active hit zone contacts the ball. `ShotModel` resolves a target
trajectory, then `PointLoopEventBridge` registers the hit before any launch is applied.

J mappings are neutral SafeReturn, forward Deep, backward Drop, and up Lob. K requests
Smash. Vertical intent has priority over horizontal intent and facing direction mirrors
forward/backward decisions.

## Trajectory model

SafeReturn, Deep, Drop, Lob, and Serve use target landing plus apex height. Smash uses a
target landing plus flight time. Every result reports velocity, predicted landing,
flight time, apex, net-cross height, validity, and failure reason. No random failure is
used.

## Serve isolation

The ball has `Inactive`, `ServeToss`, and `Rally` play modes. A first J launches a physical
toss without registering a tennis hit. During `ServeToss`, court observations never reach
the rule layer. A second J inside the timing window resolves deterministic
Perfect/Good/Normal/Weak quality, registers the hit, switches to Rally, and only then
launches. A missed toss falls and settles under normal physics before a safe retry.
