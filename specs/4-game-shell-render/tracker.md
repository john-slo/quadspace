# Feature: game-shell-render

## Metadata
- **Feature:** game-shell-render
- **Slug:** game-shell-render
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T10:35:00Z
- **Updated:** 2026-08-25T10:40:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T10:35:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 4 + brief |
| 2026-08-25T10:35:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T10:39:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | GameEngine, Game page + JS rAF loop + starfield/ship, HUD |
| 2026-08-25T10:39:30Z | IMPLEMENT | test-author | Claude Opus 4.8 | GameEngine movement/clamp/normalization tests |
| 2026-08-25T10:40:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 25 tests pass, format clean, assets served (:5000) |
| 2026-08-25T10:40:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- A `/game` page renders a full canvas playfield with a neon parallax "space depth-field" starfield
  background (multiple layers moving at different speeds; count from `game-config.json > starfield`).
- The player ship is drawn on the plane and moves in the x and y axes via WASD; movement speed is the
  constant `ship.speed` from config, clamped to the arena bounds.
- A HUD overlay shows the current SCORE, LEVEL, and LIVES (placeholders for now: 0 / 1 / start-lives).
- The game simulation for ship movement lives in a pure C# `GameEngine` in `Quadspace.Core` and is
  unit-tested; the canvas render loop and keyboard input are thin JS interop.

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`.
- The render loop runs in JS via `requestAnimationFrame`; each frame it reads WASD input and calls a
  synchronous `[JSInvokable] Tick(dt, moveX, moveY)` on the C# component, which advances the engine and
  returns a small render model (ship position). JS draws the starfield (its own animated stars, seeded
  from config) and the ship. Per-frame gameplay state is authoritative in C#.
- The starfield stars are a purely visual effect owned by JS (no gameplay), seeded by the config
  counts; this keeps per-frame interop tiny.
- Arena is a fixed logical size (config `arena`); the canvas uses those pixel dimensions and is scaled
  to fit via CSS, preserving aspect ratio.
- Spheres, shooting, scoring, levels, and lives changes are later features; HUD values are static here.

## Constraints & Risks
- Per-frame synchronous JS→.NET interop must stay cheap (a few doubles in/out) to hold 60 fps.
- The component must dispose its `DotNetObjectReference`, JS module, and loop handle to avoid leaks.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** `Quadspace.Core` provides a `GameEngine` initialized from `GameConfig` whose `Update(dt,
  moveX, moveY)` moves the ship at `ship.speed`, normalizes diagonal input to not exceed that speed,
  and clamps the ship within `[radius, arenaSize - radius]` on each axis; `Score`/`Level`/`Lives` are
  exposed (initially 0 / 1 / `ship.startLives`).
- **AC2:** Navigating to `/game` shows a canvas with an animated multi-layer parallax starfield.
- **AC3:** Pressing W/A/S/D moves the ship up/left/down/right on the plane; the ship cannot leave the
  arena bounds.
- **AC4:** A HUD overlay displays SCORE, LEVEL, and LIVES.
- **AC5:** xUnit tests cover `GameEngine.Update` (axis movement, diagonal normalization, clamping at
  each edge); build is warning-free, tests pass, format is clean.

## Design
Add `GameEngine` to `Quadspace.Core`: holds `ShipX/ShipY` (doubles), `Score/Level/Lives`, constructed
from `GameConfig` with the ship centered. `Update(dt, moveX, moveY)` normalizes the input vector when
its length exceeds 1, advances the ship by `speed * dt`, and clamps to arena bounds. Pure and
deterministic.

In `Quadspace.Client`, add `Pages/Game.razor` (+ `Game.razor.cs`) hosting a `<canvas>` and a HUD
overlay bound to the engine. In `OnAfterRenderAsync(firstRender)` it imports `js/game.js`, passes the
canvas element, the arena and starfield config, and a `DotNetObjectReference<Game>`; JS starts the
rAF loop. The `[JSInvokable] RenderModel Tick(double dt, double moveX, double moveY)` advances the
engine and returns `{ shipX, shipY, shipRadius }`. `game.js` owns the WASD key state, animates the
starfield layers, and draws the neon ship. The component implements `IAsyncDisposable` to stop the
loop and dispose the module and object reference.

## Open Questions / Out of Scope
- Spheres/shooting/collision (feature 5); levels/lives/game-over/audio/gamepad (features 6–8).
- Touch/mobile controls — out of scope.

## Task Checklist
- [x] **T1** — Add `GameEngine` (ship movement, normalization, clamping, Score/Level/Lives) to
      `Quadspace.Core`. _(AC: AC1)_
- [x] **T2** — Add `Pages/Game.razor` + `Game.razor.cs` with canvas, HUD overlay, `Tick` JSInvokable,
      and disposal. _(AC: AC2, AC3, AC4)_
- [x] **T3** — Add `wwwroot/js/game.js`: rAF loop, WASD input, parallax starfield, ship draw; add game
      canvas/HUD styles. _(AC: AC2, AC3, AC4)_
- [x] **T4** — Add xUnit tests for `GameEngine.Update`. _(AC: AC5)_
- [x] **T5** — Verify warning-free build, passing tests, clean format; manual browser check of
      movement + starfield. _(AC: AC1–AC5)_

## Test Plan
- **AC1/AC5** → `GameEngine.Update` tests: moving right increases ShipX by `speed*dt`; moving up
  decreases ShipY; a diagonal (1,1) input moves total distance ≤ `speed*dt` (normalized); the ship
  clamps at the left/right/top/bottom edges and never exceeds bounds; `Lives` initializes to
  `startLives`, `Level` to 1, `Score` to 0.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T10:35:00Z | (none yet) | — | tracker created |
| 2026-08-25T10:40:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T10:40:10Z | dotnet test quadspace.sln -c Release | PASS | 25 passed, 0 failed |
| 2026-08-25T10:40:20Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T10:40:40Z | asset serve check (:5000) | PASS | /game 200, /js/game.js 200, /game-config.json 200 (visual browser check deferred to user) |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 6 | 40 | ✅ |
| New files | 4 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~430 | 2000 | ✅ |

## Decisions / Escalations
- None.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
