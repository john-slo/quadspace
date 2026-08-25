# Feature: levels-lives-gameover

## Metadata
- **Feature:** levels-lives-gameover
- **Slug:** levels-lives-gameover
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T11:00:00Z
- **Updated:** 2026-08-25T11:05:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T11:00:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 6 + brief |
| 2026-08-25T11:00:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T11:04:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | levels/intro, lives/invuln, extra-life, life-spheres, game over + name entry |
| 2026-08-25T11:04:30Z | IMPLEMENT | test-author | Claude Opus 4.8 | level-up, extra-life, life-sphere, collision, invuln, game-over, intro tests |
| 2026-08-25T11:05:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 42 tests pass, format clean, game.js node --check ok |
| 2026-08-25T11:05:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- Level progression: level N requires destroying `N * spheresPerLevelMultiplier` (8·N) spheres; the
  sphere spawn rate is `N * spawnRatePerLevelPerSecond` per second. Each level is introduced by a brief
  "LEVEL N" banner during which spawning is paused.
- Lives: colliding with a sphere costs one life, briefly makes the ship invulnerable, and destroys the
  colliding sphere. The run ends when lives reach zero.
- The player gains +1 life every `extraLifeEveryLevels` (8) levels and by destroying a rare, marked
  life-sphere (spawn probability `lifeSphereSpawnChance`); lives are capped at `maxLives`.
- On game over, the player can enter a name (≤50 chars); the score is saved via `POST /api/scores`,
  then the player returns to the home leaderboard.
- All new simulation lives in the pure `GameEngine` and is unit-tested with an injected RNG.

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`.
- Level 1 also shows an intro banner (intro at construction). During intro the ship can move but no
  spheres spawn; on level-up the field is cleared for a clean start.
- Only shots count toward level progress and score; a sphere destroyed by ship contact does not score
  or advance the level.
- A life-sphere destroyed by a shot both scores 8 and grants +1 life (capped).
- Game over is surfaced to the UI by JS calling `[JSInvokable] EndGame()` once; the Blazor component
  then shows a name-entry overlay that posts to the score API and navigates home.

## Constraints & Risks
- The intro-pauses-spawning change altered earlier sphere-spawn tests; their config now uses
  `introSeconds: 0` to test mechanics in the playing state.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** Destroying `level * spheresPerLevelMultiplier` spheres advances the level, resets the
  per-level counter, clears the field, and starts a level-intro banner; spawn rate scales with level.
- **AC2:** A level whose number is a multiple of `extraLifeEveryLevels` grants +1 life (capped at
  `maxLives`).
- **AC3:** Destroying a life-sphere grants +1 life (capped at `maxLives`).
- **AC4:** A ship–sphere collision costs one life, sets invulnerability for
  `invulnerabilitySeconds`, and destroys the colliding sphere; while invulnerable no further life is
  lost.
- **AC5:** When lives reach zero the game is over and the simulation stops advancing; the UI shows a
  name-entry overlay that posts the score (name ≤50) and returns to the home leaderboard.
- **AC6:** During a level intro no spheres spawn; spawning resumes afterward.
- **AC7:** xUnit tests cover level-up, extra-life cadence, life-sphere, ship collision, invulnerability,
  game over, and intro pause; build warning-free, tests pass, format clean.

## Design
Extend `GameEngine` with level/lives state: `SpheresDestroyedThisLevel`, `SpheresRequiredThisLevel`
(`level * multiplier`), `LevelIntroRemaining`/`IsLevelIntro`, `InvulnerabilityRemaining`/
`IsShipInvulnerable`, and `IsGameOver`. `Update` moves the ship, ticks the invuln/intro timers, spawns
(only when not in intro), moves projectiles/spheres, resolves projectile hits (score + level count +
life-sphere), resolves ship collisions when vulnerable (life loss + invuln + destroy + game over), and
checks level-up (advance, clear field, intro, extra-life cadence). `Sphere` gains `IsLifeSphere`; spawn
marks it by `lifeSphereSpawnChance`.

`Game.razor.cs` `Tick` returns the expanded model (intro/invuln/gameover flags, per-sphere life flag);
`EndGame` (JSInvokable) shows the name-entry overlay which posts a `ScoreSubmissionRequest` and
navigates home. `game.js` draws life-spheres green, blinks the ship while invulnerable, shows the
"LEVEL N" banner, and on game over stops the loop and calls `EndGame`.

## Open Questions / Out of Scope
- Audio (feature 7) and gamepad (feature 8).
- Showing the achieved rank on the game-over screen — out of scope (home board reflects it).

## Task Checklist
- [x] **T1** — Add `IsLifeSphere` to `Sphere`; extend `GameEngine` with level/lives/intro/invuln/game-over
      state and logic. _(AC: AC1–AC6)_
- [x] **T2** — Expand `Tick`/`RenderModel` (flags + life flag) and add `EndGame`; add the name-entry
      overlay + submit-to-API + navigate home in `Game.razor`/`.cs`. _(AC: AC5)_
- [x] **T3** — Update `game.js`: life-sphere colour, invuln blink, level-intro banner, game-over call.
      _(AC: AC1, AC3, AC4, AC5)_
- [x] **T4** — Add game-over overlay styles. _(AC: AC5)_
- [x] **T5** — Add xUnit tests for level-up, extra-life, life-sphere, collision, invuln, game over,
      intro. _(AC: AC7)_
- [x] **T6** — Verify warning-free build, passing tests, clean format; manual browser check. _(AC: AC1–AC7)_

## Test Plan
- **AC1** → destroying the required spheres advances Level, clears the field, resets the counter.
- **AC2** → reaching a level multiple of `extraLifeEveryLevels` increments Lives.
- **AC3/AC4** → destroying a forced life-sphere grants a life (and respects `maxLives`); a ship–sphere
  overlap drops a life, sets invulnerability, destroys the sphere, and blocks a second immediate loss.
- **AC5** → at zero lives `IsGameOver` is set and further `Update` calls are no-ops.
- **AC6** → during the intro no spheres spawn; after it, they do.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T11:05:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T11:05:10Z | dotnet test quadspace.sln -c Release | PASS | 42 passed, 0 failed |
| 2026-08-25T11:05:20Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T11:05:30Z | node --check game.js | PASS | JS module parses cleanly |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 10 | 40 | ✅ |
| New files | 2 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~760 | 2000 | ✅ |

## Decisions / Escalations
- None.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
