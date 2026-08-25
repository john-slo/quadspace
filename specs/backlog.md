<!--
  Agentic SDD product backlog - the single source of truth for the whole product/epic.
  Location: specs/backlog.md  (exactly one per repo)
  Produced by the product-architect prompt in the BLUEPRINT/BACKLOG phases (see governance/PLANNING.md).
  Machine-parsed: the "## Backlog" table rows and "- **Key:** value" metadata lines are read by the
  orchestrator. Keep those shapes intact. Feature ids are sequential; slugs are kebab-case.
  Rows are executed in order once their dependsOn ids are all DONE. Do NOT create side documents -
  the per-feature spec is written in that feature's specs/<id>-<slug>/tracker.md during SPEC.
-->
# Product Backlog: quadspace

## Metadata
- **Product:** quadspace
- **Created:** 2026-08-25T09:27:00Z
- **Updated:** 2026-08-25T09:27:00Z
- **Blueprint:** governance/PRODUCT.md + governance/ARCHITECTURE.md
- **Status:** READY

## Blueprint Summary
<!-- One paragraph: what the product is and the architecture chosen in BLUEPRINT. Full detail lives
     in governance/PRODUCT.md and governance/ARCHITECTURE.md - summarize, do not duplicate. -->
quadspace is a neon 80s browser arcade shooter: pilot a ship on a 2D plane, shoot metallic spheres
that spawn from the edges and bounce off walls, survive with limited lives across escalating levels,
and post a high score. It is a .NET 10 Blazor WebAssembly client (all game simulation in C#, thin JS
interop for canvas/audio/input) served by an ASP.NET Core host that exposes a minimal JSON score API
persisting scores as server-side files (daily files + a single top-100 file). All gameplay tuning
lives in a central `game-config.json`.

## Backlog
<!--
  One row per PR-sized feature. Columns:
    Id        - sequential integer (1, 2, 3, ...)
    Slug      - kebab-case; becomes specs/<id>-<slug>/ and feature/<id>-<slug>
    Priority  - P0 (must) | P1 (should) | P2 (could)
    DependsOn - comma-separated ids that must be DONE first, or "-"
    Outcome   - one line describing the user-visible result (seeds INTAKE)
    Status    - DRAFT | READY | IN_PROGRESS | DONE | BLOCKED
  A feature that will not fit one tracker + one change budget must be split into more rows.
-->
| Id | Slug | Priority | DependsOn | Outcome | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | project-scaffold | P0 | - | Solution with Shared/Client/Host/Tests projects (net10.0), `game-config.json`, updated config + CI; the empty neon app builds, tests run, and `dotnet run` serves a page in the browser. | IN_PROGRESS |
| 2 | score-persistence | P0 | 1 | File-based score service (per-date daily JSON + insert-in-order top-100 JSON) and minimal host API (`GET /api/scores/top`, `POST /api/scores`), fully xUnit-tested. | IN_PROGRESS |
| 3 | home-highscores | P0 | 2 | Retro 80s arcade home/attract screen listing the top 10 high scores with an "insert coin"/start prompt. | IN_PROGRESS |
| 4 | game-shell-render | P0 | 1 | Canvas render loop with parallax starfield depth-field background, the ship rendered and moving via WASD, and a HUD showing score/level/lives. | READY |
| 5 | spheres-shooting-scoring | P0 | 4 | Metallic spheres spawn from random edges with constant velocity and 90° wall bounce; tap-fire 4-way shots; a hit shrinks/destroys the sphere and adds 8 points. | READY |
| 6 | levels-lives-gameover | P0 | 5, 2 | Level progression (8×level spheres, spawn rate = level/sec) with level intro; lives with brief invulnerability, +1 life every 8 levels, and rare life-spheres; game over triggers name entry (≤50 chars) and saves via the score API. | READY |
| 7 | audio-toggle | P1 | 4 | Procedurally generated Web Audio background beat with an on/off toggle persisted across the session. | READY |
| 8 | gamepad-support | P1 | 5 | Gamepad support: left stick moves the ship, ABXY buttons fire in the four directions. | READY |

## Run Log
<!-- Append-only. The orchestrator writes one row each time EXECUTE starts/stops or a row transitions. -->
| Timestamp (UTC) | Event | Feature | Note |
| --- | --- | --- | --- |
| 2026-08-25T09:27:00Z | backlog created | - | drafted during BACKLOG; approved by human checkpoint |
| 2026-08-25T09:30:00Z | feature started | 1-project-scaffold | on shared branch feature/initial-game (epic issue #1) |
| 2026-08-25T10:24:00Z | feature verified | 2-score-persistence | score API + file store, 18 tests; on PR #2 |
| 2026-08-25T10:30:00Z | feature verified | 3-home-highscores | retro top-10 leaderboard home; on PR #2 |

## Decisions
<!-- Required to add, remove, or re-scope a backlog row after the human checkpoint. Otherwise "None". -->
- Feature 1 (project-scaffold) will create ~4 projects, exceeding `changeBudget.maxNewProjects` (1).
  This is expected greenfield foundation work; the overage will be recorded as a Decision/Escalation
  in that feature's tracker per GUARDRAILS.md.
