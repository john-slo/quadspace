# Feature: mobile-support

## Metadata
- **Feature:** mobile-support
- **Slug:** mobile-support
- **IssueNumber:** 5
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/5
- **Branch:** feature/10-mobile-support
- **Worktree:** (in-place branch)
- **WorktreeMode:** branch
- **Phase:** DONE
- **ActivePrompt:** orchestrator
- **Status:** DONE
- **Created:** 2026-08-27T10:20:00Z
- **Updated:** 2026-08-27T12:05:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-27T10:15:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from user request; 7 clarifying questions answered; scope locked |
| 2026-08-27T10:20:00Z | REGISTER | orchestrator | Claude Opus 4.8 | issue #5 created; backlog row 10 added (Decision) |
| 2026-08-27T10:20:00Z | WORKTREE | orchestrator | Claude Opus 4.8 | branch feature/10-mobile-support created in place |
| 2026-08-27T10:20:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-27T10:55:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | ControlsConfig; touch detect + adaptive arena; joystick/fire/toggle overlay + CSS; game.js pointer wiring |
| 2026-08-27T11:05:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 49 tests pass, format clean, node --check ok; headless touch+desktop smoke PASS (no console/404 errors) |
| 2026-08-27T11:10:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |
| 2026-08-27T11:59:00Z | PR | orchestrator | Claude Opus 4.8 | PR #6; 4 Copilot review rounds addressed (default single-source, gesture blocking, rotate hint, aspect ratio); CI green; Copilot 🟢 approved |
| 2026-08-27T12:05:00Z | DONE | orchestrator | Claude Opus 4.8 | squash-merged to main (#6); branch deleted; issue #5 closed |

## Requirements
<!-- FROZEN after scope lock. -->
- On touch-capable devices, quadspace is fully playable without a keyboard; keyboard input still works
  everywhere and desktop (non-touch) behaviour is unchanged.
- On touch devices the play-field adapts to the actual screen size/aspect (canvas fills the viewport);
  on desktop the fixed 1280x720 field is retained.
- Touch movement is via a left-side virtual joystick; firing is via four right-side directional
  buttons; music and SFX are toggled via on-screen icon buttons.
- Portrait orientation shows a non-blocking "rotate to landscape" hint; both orientations stay playable.

## Assumptions
- **WorktreeMode:** `branch` (in-place `feature/10-mobile-support`), matching prior features.
- "Touch device" = the browser reports touch capability (`navigator.maxTouchPoints > 0 ||
  'ontouchstart' in window`). This covers phones, tablets, and touch laptops; keyboard remains active.
- Adaptive arena is achieved by cloning the immutable `GameConfig` record with the measured viewport
  dimensions (`Config with { Arena = Config.Arena with { Width = w, Height = h } }`) before
  constructing the `GameEngine`. The engine already reads all bounds from `Config.Arena`, so **no
  engine/core logic changes and no new engine tests are required**. Absolute px/sec speeds remain
  unchanged, so a larger/smaller field simply changes available play space.
- Touch control DOM (joystick, fire buttons, toggles, orientation hint) is rendered by `Game.razor`
  (Blazor-idiomatic, styled in `app.css`) and wired to pointer events inside `game.js`, keeping input
  logic consolidated in `game.js` alongside the existing keyboard handling. The same `move.x/move.y`
  and `Fire(dx,dy)` interop already used by the keyboard is reused (no new .NET interop surface).
- A single new Core config record field is added for a small tunable (touch UI enable + arena source);
  it is optional with a safe default so existing `game-config.json` stays valid.

## Constraints & Risks
- Must not regress desktop keyboard play or the existing published/Docker fingerprint serving.
- `game.js` already owns audio/input; adding a second JS module would be rewritten by the
  static-web-asset import map and fail to load, so all new touch input lives in `game.js`.
- Pointer events must not scroll/zoom the page (use `touch-action: none` and `preventDefault`), and
  must not conflict with the audio "first gesture" resume.
- Measuring the viewport must happen after first render (JS interop unavailable during OnInitialized).

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** On a touch device the touch controls are shown and the keyboard controls legend is hidden;
  on a non-touch desktop the keyboard legend is shown and no touch controls appear. Keyboard still
  works on touch devices that have one.
- **AC2:** On a touch device the canvas/play-field fills the viewport and its dimensions match the
  screen size/aspect; on desktop the field remains fixed 1280x720. The `GameEngine` receives the
  arena dims actually used (spheres spawn from the visible edges, ship clamps to visible bounds).
- **AC3:** A left-side virtual joystick moves the ship in any direction proportional to the drag; the
  ship stops when the thumbstick is released.
- **AC4:** Four right-side buttons fire up/down/left/right (single shot per tap) and trigger the fire
  SFX, matching keyboard behaviour.
- **AC5:** On-screen music (♪) and SFX icon buttons toggle music and sound effects respectively, with
  their state reflected on the button; state persists (same localStorage keys as M/N).
- **AC6:** In portrait orientation a "rotate to landscape" hint overlay is visible; it disappears in
  landscape. The game remains playable in both orientations.
- **AC7:** Build is warning-free, all existing tests still pass (plus any new config test),
  `dotnet format` is clean, `node --check game.js` passes, and a Docker container serves the app with
  no console/404 errors.

## Design
**Touch detection & arena (Client/Blazor).** Add an exported `isTouchDevice()` and a
`measureViewport()` helper to `game.js`. In `Game.razor.cs`, defer engine construction: on first
render, call `isTouchDevice()`. If touch, call `measureViewport()` to get the available width/height,
clone the config arena to those dims, set `_width/_height`, and construct the `GameEngine` from the
cloned config; otherwise keep the current fixed path. A `_isTouch` flag drives conditional rendering.

**Touch UI (Game.razor + app.css).** When `_isTouch`, render an overlay layer over the canvas:
a joystick base + knob (bottom-left), a 4-button fire cluster (bottom-right, up/down/left/right), and
small ♪/SFX toggle icon buttons (top corner). Hide the keyboard `controls-legend` on touch. Add a
`rotate-hint` overlay shown only in portrait via a CSS orientation media query. All controls use
`touch-action: none` and are absolutely positioned within `.game-shell` with `pointer-events` only on
the controls.

**Input wiring (game.js).** Extend `start()` to accept the touch flag and, when set, attach pointer
handlers to the control elements (looked up within the canvas's shell):
- Joystick: `pointerdown/move/up` on the base compute a normalized (-1..1) vector written to
  `move.x/move.y` (clamped), released to 0 on `pointerup`/`pointercancel`.
- Fire buttons: `pointerdown` calls `dotNetRef.invokeMethod('Fire', dx, dy)` + `sfx.fire()` (one shot
  per press, mirroring `!e.repeat` keyboard behaviour).
- Toggle buttons: `pointerdown` calls `beat.toggleMuted()` / `sfx.toggle()`; both call `beat.start()`
  first to satisfy the audio autoplay gesture. Button visuals updated via the existing
  `reflectState()` indicators (extended to also update the icon buttons).
- `stop()` removes the added listeners.

**Config.** Add an optional `Controls` record to `GameConfig` (e.g. `joystickDeadZone`,
`adaptArenaToScreenOnTouch`) with defaults, plus a `game-config.json` entry, kept minimal.

## Open Questions / Out of Scope
- Gamepad support — feature 8 (separate PR).
- Haptics/vibration feedback — out of scope.
- Portrait-specific control relayout beyond the rotate hint — out of scope (both orientations play,
  hint encourages landscape).

## Task Checklist
- [x] **T1** — Add `Controls` config record (deadzone + adaptArenaOnTouch) to `GameConfig.cs` with
      defaults; add entry to `game-config.json`; add a small config test. _(AC: AC2, AC5)_
- [x] **T2** — Add `isTouchDevice()` + `measureViewport()` exports to `game.js`; defer/adapt engine
      construction in `Game.razor.cs` (clone arena on touch); add `_isTouch` flag. _(AC: AC1, AC2)_
- [x] **T3** — Render touch overlay (joystick, 4 fire buttons, ♪/SFX toggles, rotate hint) in
      `Game.razor`; hide keyboard legend on touch; add styles + portrait media query to `app.css`.
      _(AC: AC1, AC3, AC4, AC5, AC6)_
- [x] **T4** — Wire pointer handlers in `game.js` `start()`: joystick→move, fire buttons→Fire+sfx,
      toggles→mute/sfx; extend `reflectState()` to update icon buttons; clean up in `stop()`.
      _(AC: AC3, AC4, AC5)_
- [x] **T5** — Verify: warning-free build, all tests pass, `dotnet format` clean, `node --check
      game.js`, and a headless touch/desktop smoke test with no console/404 errors. _(AC: AC7)_

## Test Plan
- Automated: existing 47 engine/API tests must stay green; add a `GameConfig` test asserting the new
  `Controls` record parses with defaults and from JSON. Build warning-free; `dotnet format` clean;
  `node --check game.js`.
- Manual/headless: emulate a touch device (Puppeteer touch/mobile viewport) — verify touch controls
  render, joystick moves the ship, fire buttons shoot, toggles mute music/SFX, portrait shows the
  rotate hint; verify desktop still shows the keyboard legend and plays via keyboard. Docker container
  boots with no console/404 errors.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-27T10:20:00Z | (none yet) | — | tracker created |
| 2026-08-27T11:04:00Z | node --check game.js | PASS | parses cleanly |
| 2026-08-27T11:05:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-27T11:06:00Z | dotnet test quadspace.sln -c Release | PASS | 49 passed, 0 failed (47 + 2 new config tests) |
| 2026-08-27T11:07:00Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-27T11:09:00Z | Puppeteer smoke (published Host, touch + desktop viewports) | PASS | touch: joystick+4 fire+♪/SFX shown, legend hidden, shell.touch, canvas 812×375; desktop: legend, canvas 1280×720; 0 console errors, 0 failed/404 requests |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 9 | 40 | ✅ |
| New files | 1 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~607 | 2000 | ✅ |

## Decisions / Escalations
- 2026-08-27: Backlog row 10 (mobile-support) added after the human checkpoint, approved by the human
  owner during INTAKE (see backlog Decisions). Kept as its own tracker/PR.
- Adaptive arena limited to touch devices (human choice); desktop keeps the fixed 1280x720 field to
  avoid changing existing desktop gameplay.
- Concurrent-edit incident: a concurrent edit to `Game.razor` (adding a game-over `◄ BACK` link and
  `open="true"`) also dropped the restored `rotate-hint` markup, which a `git add -A` swept into a
  fix-up commit. Copilot review caught the missing hint; it was restored and the concurrent BACK-link
  additions were preserved.
- Review rounds: Copilot raised (1) hard-coded default constants — resolved with
  `GameConfig.ControlsOrDefault` as the single source of truth; (2) browser gestures reaching the
  full-screen canvas — resolved with `touch-action/overscroll-behavior: none`; (3) missing rotate
  hint — restored; (4) `adaptArenaToScreenOnTouch:false` still stretching the canvas — resolved with
  `object-fit: contain` + black letterbox. Final review 🟢 approved.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/6
- **State:** MERGED (squash) — CI green, Copilot 🟢 approved
