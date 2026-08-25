# Feature: home-highscores

## Metadata
- **Feature:** home-highscores
- **Slug:** home-highscores
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T10:28:00Z
- **Updated:** 2026-08-25T10:30:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T10:28:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 3 + brief |
| 2026-08-25T10:28:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T10:29:30Z | IMPLEMENT | implementer | Claude Opus 4.8 | leaderboard home page + retro scoreboard styles |
| 2026-08-25T10:30:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, format clean, shell+API smoke-tested |
| 2026-08-25T10:30:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- The home page is a retro 80s arcade attract screen that lists the **top 10** high scores fetched
  from `GET /api/scores/top?count=10`.
- Each row shows rank, player name, and score, in descending score order.
- When there are no scores, show a retro "no scores yet" message rather than an empty table.
- The screen shows the QUADSPACE title, an attract "INSERT COIN" element, and a "PRESS START" control
  that navigates to the game route (`/game`).
- Neon 80s styling consistent with the existing theme.

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`.
- The `/game` route does not exist yet (added in feature 4); PRESS START links to it now and becomes
  live then. This is acceptable for this feature.
- This is a UI feature with no new pure business logic; it is verified by build/format and a manual
  smoke test against the score API. No bUnit dependency is added (not in the allowlist; component
  rendering has no reasonable unit test here).
- The client's existing scoped `HttpClient` (base = host) is used to call the API.

## Constraints & Risks
- If the API call fails, the page degrades to the empty-state message (no crash).

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** On load, the home page calls `GET /api/scores/top?count=10` and renders up to 10 entries as
  a leaderboard (rank, name, score) in descending order.
- **AC2:** With no scores, the page shows a retro "no scores yet" message instead of an empty list.
- **AC3:** The page shows the QUADSPACE title, a blinking "INSERT COIN" attract element, and a
  "PRESS START" control linking to `/game`.
- **AC4:** Neon 80s arcade styling is applied to the leaderboard; the build is warning-free and
  `dotnet format` is clean.

## Design
Rewrite `Pages/Home.razor` to inject the scoped `HttpClient`, fetch the top 10 in
`OnInitializedAsync` via `GetFromJsonAsync<List<ScoreEntry>>("api/scores/top?count=10")`, and render a
loading state, an empty state, or an ordered leaderboard list. Add `Quadspace.Core.Scoring` to
`_Imports.razor`. Keep the existing neon title/attract elements and add a "PRESS START" anchor to
`game`. Extend `wwwroot/css/app.css` with scoreboard styles (neon rows, monospace columns) — no new
files beyond the tracker.

## Open Questions / Out of Scope
- The actual game at `/game` — feature 4. Submitting scores — feature 6.
- Auto-refresh/polling of the board — out of scope (loads once on navigation).

## Task Checklist
- [x] **T1** — Rewrite `Home.razor` to fetch and render the top-10 leaderboard with loading/empty
      states and a PRESS START link to `/game`; add the scoring `@using`. _(AC: AC1, AC2, AC3)_
- [x] **T2** — Add retro leaderboard styles to `app.css`. _(AC: AC4)_
- [x] **T3** — Verify warning-free build + clean format; manual smoke test (seed a score, see it
      listed; empty state with none). _(AC: AC1–AC4)_

## Test Plan
- Manual: with an empty store the board shows the empty-state message; after `POST /api/scores`, the
  entry appears with correct rank/name/score on reload. Automated coverage is unchanged (UI-only).

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T10:28:00Z | (none yet) | — | tracker created |
| 2026-08-25T10:30:00Z | dotnet build src/Quadspace.Client -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T10:30:10Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T10:30:30Z | shell + API smoke (:5179) | PASS | home shell 200; seeded scores return ordered (NOVA 1500, ZED 900, PIX 300); /game shell 200 |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 5 | 40 | ✅ |
| New files | 1 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~150 | 2000 | ✅ |

## Decisions / Escalations
- None.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
