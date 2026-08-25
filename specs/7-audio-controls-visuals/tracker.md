# Feature: audio-controls-visuals

## Metadata
- **Feature:** audio-controls-visuals
- **Slug:** audio-controls-visuals
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T12:20:00Z
- **Updated:** 2026-08-25T12:29:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T12:20:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 7 + user request (controls legend + richer visuals) |
| 2026-08-25T12:20:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T12:28:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | Web Audio beat + M mute + legend + richer ship/sphere/bullet visuals |
| 2026-08-25T12:29:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 42 tests pass, format clean, node --check ok; user-confirmed in browser |
| 2026-08-25T12:29:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- A procedurally generated (Web Audio API) background beat plays during the game, toggleable on/off;
  the mute state persists across the session (localStorage). No copyrighted audio assets.
- The game page shows an on-screen controls legend: MOVE = WASD, FIRE = arrow keys, SOUND = M (with a
  live ON/OFF indicator).
- The ship, spheres, and projectiles are rendered with more visual detail (neon ship with thruster,
  metallic spheres with specular highlight/rim, glowing projectiles with a tail).

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`. This folds the planned "audio-toggle"
  feature (backlog row 7) together with two user-requested enhancements (controls legend + visual
  polish) — recorded as a Decision below.
- Audio lives inside the existing `game.js` module (not a separate JS module) because a second module
  would be rewritten by the static-web-asset import map and fail to load (same issue fixed for
  game.js). The AudioContext is created/resumed on the first user gesture (key press) per browser
  autoplay policy.
- `M` toggles sound; the legend shows the current ON/OFF state. No change to the C# engine, so no new
  unit tests — verified by build/format, `node --check`, and manual browser check.

## Constraints & Risks
- Browser autoplay policy: audio only starts after a user gesture; handled by starting on first keydown.
- Keep the beat lightweight (scheduled oscillators) to avoid audio glitches.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** A procedural background beat plays on the game page after the first key press; pressing `M`
  toggles it off/on and the choice persists across a page reload (localStorage).
- **AC2:** The game page displays a controls legend (MOVE = WASD, FIRE = arrows, SOUND = M) with a live
  ON/OFF sound indicator.
- **AC3:** The ship, spheres, and projectiles are visibly more detailed than the previous flat shapes
  (thruster/cockpit on the ship; specular highlight + rim on spheres; glowing core + tail on shots).
- **AC4:** Build is warning-free, existing tests still pass, `dotnet format` is clean, and
  `node --check game.js` passes.

## Design
Extend `game.js` with a small Web Audio beat engine: a lazily-created `AudioContext`, a lookahead
scheduler (`setInterval`) that sequences a kick plus a bass/arp pattern through a master gain, and
mute state persisted in `localStorage['quadspace-muted']`. The rAF `start()` wires the first keydown
to resume/kick off audio and handles `KeyM` to toggle mute (updating a `#sound-state` DOM indicator);
the returned `stop()` also tears down audio.

Enhance the draw functions: `drawShip` gains a thruster flame and cockpit; `drawSphere` gains a
specular highlight and darker rim for a metallic read; `drawProjectile` gains a bright core and a short
motion tail. Add an HTML controls-legend overlay to `Game.razor` with a `#sound-state` span, and legend
styles to `app.css`.

## Open Questions / Out of Scope
- Gamepad support — feature 8 (separate PR).
- Per-event sound effects (shot/explosion) — out of scope (background beat only).

## Task Checklist
- [x] **T1** — Add the Web Audio beat + mute (localStorage) + `M` toggle and first-gesture start to
      `game.js`; update a `#sound-state` indicator. _(AC: AC1, AC2)_
- [x] **T2** — Add the controls-legend overlay (with sound indicator) to `Game.razor` and styles to
      `app.css`. _(AC: AC2)_
- [x] **T3** — Enrich `drawShip`, `drawSphere`, `drawProjectile` visuals. _(AC: AC3)_
- [x] **T4** — Verify warning-free build, tests pass, format clean, `node --check game.js`; manual
      browser check of audio + legend + visuals. _(AC: AC4)_

## Test Plan
- Automated coverage unchanged (browser-layer only): build, `dotnet format`, `node --check game.js`,
  and the existing 42 engine tests must stay green.
- Manual: beat starts on first key, `M` mutes/unmutes and survives reload; legend shows correct state;
  ship/sphere/bullet visuals are clearly richer.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T12:20:00Z | (none yet) | — | tracker created |
| 2026-08-25T12:29:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T12:29:10Z | dotnet test quadspace.sln -c Release | PASS | 42 passed, 0 failed |
| 2026-08-25T12:29:20Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T12:29:30Z | node --check game.js | PASS | parses cleanly; user confirmed audio/legend/visuals in browser |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 6 | 40 | ✅ |
| New files | 1 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~360 | 2000 | ✅ |

## Decisions / Escalations
- **Scope grouping (user-directed).** Backlog row 7 was "audio-toggle"; the user asked to also add an
  on-screen controls legend and richer ship/sphere/projectile visuals, and to do all three on the
  initial-game branch before merge. Grouped here as one small browser-layer feature (no engine/API
  change). Backlog row 7 outcome updated accordingly.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
