# Feature: spheres-shooting-scoring

## Metadata
- **Feature:** spheres-shooting-scoring
- **Slug:** spheres-shooting-scoring
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T10:45:00Z
- **Updated:** 2026-08-25T10:52:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T10:45:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 5 + brief |
| 2026-08-25T10:45:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T10:51:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | Sphere/Projectile entities, engine spawn/bounce/fire/collision, JS draw+fire |
| 2026-08-25T10:51:30Z | IMPLEMENT | test-author | Claude Opus 4.8 | spawn/bounce/fire/cull/collision tests (InternalsVisibleTo) |
| 2026-08-25T10:52:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 34 tests pass, format clean, game.js node --check ok |
| 2026-08-25T10:52:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- Metallic spheres (~ship-sized) spawn from random edges of the plane with a constant velocity and
  persist until destroyed, bouncing off the walls at a 90° angle of incidence.
- The ship fires shots up/down/left/right (arrow keys), one shot per key press (tap), with multiple
  shots allowed on screen (capped by `projectile.maxOnScreen`).
- A shot that contacts a sphere destroys it with a fast shrink animation and awards `pointsPerSphere`
  (8) points; the score updates live in the HUD.
- Spheres spawn continuously at rate `level * spawnRatePerLevelPerSecond` per second (level is 1 here;
  full level progression is feature 6).
- All new simulation (spawn, movement, bounce, firing, collision, scoring, shrink) lives in the pure
  `GameEngine` and is unit-tested with an injected RNG for determinism.

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`.
- The ship does not yet lose lives on contact with a sphere (that is feature 6); spheres pass the ship
  for now.
- Firing uses a separate `[JSInvokable] Fire(dirX, dirY)` invoked on a non-repeating arrow keydown, so
  taps map to exactly one projectile. Movement continues via the per-frame `Tick`.
- The HUD moves onto the canvas (drawn in JS from the render model) so SCORE/LEVEL/LIVES update live
  without per-frame Blazor re-renders; the static HTML HUD from feature 4 is replaced.
- `sphere.shrinkSeconds` is added to `game-config.json` for the shrink animation duration.
- Spawn velocity direction is randomized (via injected `Random`) but flipped to head into the arena
  from the spawn edge.

## Constraints & Risks
- Per-frame interop now returns sphere/projectile arrays; counts are modest (tens), acceptable for 60 fps.
- Determinism for tests requires the engine to accept a seeded `Random`.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** `GameEngine` spawns spheres from a random edge at rate `level * spawnRatePerLevelPerSecond`;
  with a seeded RNG, advancing time produces the expected sphere count.
- **AC2:** Spheres move at constant `sphere.speed` and bounce off each wall by inverting the
  perpendicular velocity component (90° reflection), staying within bounds.
- **AC3:** `GameEngine.Fire(dirX, dirY)` adds one projectile from the ship in that (axis) direction at
  `projectile.speed`; it is ignored for a zero direction and when `projectile.maxOnScreen` is reached.
  Projectiles move and are culled when they leave the arena.
- **AC4:** A projectile overlapping a live sphere destroys it (begins shrinking, removed after
  `shrinkSeconds`), removes the projectile, and adds `pointsPerSphere` to the score exactly once.
- **AC5:** On `/game`, arrow keys fire in four directions, spheres render (metallic) and bounce, hits
  shrink/destroy spheres, and the HUD score increases by 8 per destroyed sphere.
- **AC6:** xUnit tests cover spawn count, wall bounce, firing (add/cap/zero), projectile cull, and
  collision+scoring+shrink removal; build warning-free, tests pass, format clean.

## Design
Add mutable `Sphere` and `Projectile` entity classes to `Quadspace.Core.Engine` (public read props,
internal mutation). Extend `GameEngine` with `_spheres`, `_projectiles`, an injected `Random` (default
`new Random()`), and a spawn accumulator. `Update` now: moves the ship, accumulates and spawns spheres,
advances projectiles (culling out-of-bounds), advances spheres (wall reflection + shrink timers), then
resolves projectile↔sphere collisions (destroy + score). `Fire(dirX, dirY)` normalizes an axis
direction and adds a projectile at the ship, respecting the on-screen cap. `Spheres`/`Projectiles` are
exposed as read-only lists.

`Game.razor.cs` `Tick` returns an expanded render model (ship, spheres with current shrunk radius,
projectiles, score/level/lives). `game.js` draws metallic spheres (radial gradient), neon projectiles,
the ship, and a canvas HUD; it invokes `Fire` on non-repeating arrow keydown. The static HTML HUD is
removed in favor of the canvas HUD.

## Open Questions / Out of Scope
- Ship–sphere collision, life loss, invulnerability, level progression, level intro, extra lives,
  life-spheres, game over, name entry — feature 6.
- Audio and gamepad — features 7–8.

## Task Checklist
- [x] **T1** — Add `Sphere` and `Projectile` entities to `Quadspace.Core.Engine`. _(AC: AC2, AC3)_
- [x] **T2** — Extend `GameEngine`: injected RNG, spawn accumulator + `SpawnSphere`, sphere movement +
      90° wall bounce + shrink, projectile movement + cull, `Fire`, and collision + scoring. _(AC: AC1–AC4)_
- [x] **T3** — Expand `Tick`/`RenderModel` with spheres/projectiles/score/level/lives; move HUD to the
      canvas; add `Fire` JSInvokable. _(AC: AC5)_
- [x] **T4** — Update `game.js`: draw metallic spheres, neon projectiles, canvas HUD; fire on arrow
      keydown; remove the static HTML HUD from `Game.razor`. _(AC: AC5)_
- [x] **T5** — Add xUnit tests for spawn, bounce, fire (add/cap/zero), cull, and collision/scoring/shrink.
      _(AC: AC6)_
- [x] **T6** — Verify warning-free build, passing tests, clean format; manual browser check. _(AC: AC1–AC6)_

## Test Plan
- **AC1** → seeded engine, `Update(1.0, 0, 0)` at level 1 spawns 1 sphere; `Update` totaling 3s spawns 3.
- **AC2** → a sphere placed at the right wall moving right has its X velocity inverted and stays in bounds.
- **AC3** → `Fire(1,0)` adds a projectile with +X velocity = `projectile.speed`; `Fire(0,0)` adds none;
  firing past `maxOnScreen` adds none; a projectile leaving the arena is culled by `Update`.
- **AC4** → a projectile overlapping a sphere: after `Update`, score increases by 8 exactly once, the
  projectile is gone, the sphere is dying; after `shrinkSeconds` elapse it is removed.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T10:45:00Z | (none yet) | — | tracker created |
| 2026-08-25T10:52:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T10:52:10Z | dotnet test quadspace.sln -c Release | PASS | 34 passed, 0 failed |
| 2026-08-25T10:52:20Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T10:52:30Z | node --check game.js | PASS | JS module parses cleanly |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 14 | 40 | ✅ |
| New files | 4 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~620 | 2000 | ✅ |

## Decisions / Escalations
- None.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
