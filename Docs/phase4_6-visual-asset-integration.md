# Phase 4.6 Visual Asset Integration

Phase 4.6 restores the production art without restoring the legacy gameplay stack.

## Ownership boundary

- Actor root objects own physics, input, hit detection, and gameplay state.
- `VisualRoot` children own only `SpriteRenderer`, `Animator`, and `CatAnimationPresenter`.
- Ball, net, and background visual prefabs own only `SpriteRenderer`.
- Legacy controllers, AI scripts, colliders, rigidbodies, and rule scripts are not imported.

## Imported art

- Player and opponent sprite sheets
- Player and opponent animation clips and Animator Controllers
- Ball, net, and court background sprites

## Runtime presentation

`CatAnimationPresenter` translates the existing deterministic player action frame into
the Animator parameters `MoveX`, `Grounded`, `Jump`, `JSwing`, and `KSmash`. It never
changes movement, physics, hit results, rally state, or score.

The opponent remains a static visual actor until the dedicated AI phase. Its Animator
stays in the controller's idle state.

The 16:9 court background is scaled from its imported sprite bounds to a 20-unit world
width. This preserves aspect ratio and avoids coupling presentation to source resolution.

## Safety checks

`MatchSceneValidator` requires both character visual roots, the ball visual, net visual,
and background visual. Character visual roots must have a sprite, Animator Controller,
and presenter. No visual root may own a Rigidbody2D or Collider2D.
