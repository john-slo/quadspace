# Feature: score-persistence

## Metadata
- **Feature:** score-persistence
- **Slug:** score-persistence
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** PR
- **ActivePrompt:** orchestrator
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T10:20:00Z
- **Updated:** 2026-08-25T10:24:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T10:20:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope from backlog row 2 + brief |
| 2026-08-25T10:20:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T10:23:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | Core scoring types, Host file store + API endpoints |
| 2026-08-25T10:23:30Z | IMPLEMENT | test-author | Claude Opus 4.8 | leaderboard, validation, and file-store tests |
| 2026-08-25T10:24:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 18 tests pass, format clean, API smoke-tested |
| 2026-08-25T10:24:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- Persist high scores server-side as JSON files: a per-date **daily file**
  (`scores/daily/YYYY-MM-DD.json`) recording every submission that day, and a single
  **`scores/top100.json`** holding the 100 highest scores in descending order.
- A submitted score is inserted into the top-100 in rank order; it is kept only if the list has fewer
  than 100 entries or the score beats the current lowest.
- Expose a minimal host API: `GET /api/scores/top?count=N` (default 10) returns the top N;
  `POST /api/scores` accepts a name (≤50 chars) and score, persists it, and returns the placement.
- The server stamps the achievement time (UTC); clients do not supply it.
- The ranking/insertion logic lives in pure C# in `Quadspace.Core` and is unit-tested.

## Assumptions
- **WorktreeMode:** same-branch on `feature/initial-game`.
- Scores directory defaults to `<host content root>/scores`; it is created on demand. Files are UTF-8
  JSON arrays of score entries. Not committed to git (added to `.gitignore`).
- Tie-break for equal scores: earlier achievement time ranks higher (stable, existing entries keep
  their place over a later equal submission).
- Concurrency is guarded by an in-process async lock in a singleton store (single-instance host).
- Name is trimmed; empty/whitespace names and names > 50 chars are rejected with HTTP 400; negative
  scores are rejected with HTTP 400.
- The client is not wired to this API in this feature (features 3 and 6 consume it).

## Constraints & Risks
- File I/O is inherently side-effecting; per CONVENTIONS, unit tests target the **pure** ranking logic
  in Core rather than disk. The thin file store is minimal glue exercised via manual verification.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** `Quadspace.Core` provides a `ScoreEntry` record and a pure `Leaderboard.Insert` that
  returns the new capped (≤100), descending-ordered list plus the 1-based rank (or "not placed"),
  correctly handling: empty list, ordering, tie-break, cap at 100, and rejecting a score below the
  lowest when full.
- **AC2:** `GET /api/scores/top?count=N` returns up to N entries (default 10, bounded to ≤100) from
  `top100.json` as JSON in descending score order; an absent file yields an empty list.
- **AC3:** `POST /api/scores` with `{ "name": "...", "score": 123 }` appends the entry (with a
  server UTC timestamp) to today's daily file and updates `top100.json` via `Leaderboard.Insert`,
  returning the placement (rank or not-placed) and the resulting top list.
- **AC4:** Validation — names are trimmed; empty/whitespace or > 50 chars → HTTP 400; negative score
  → HTTP 400.
- **AC5:** xUnit tests cover `Leaderboard.Insert` (empty, ordering, tie-break, cap-at-100,
  below-lowest-rejected, placed-in-middle) and the name/score validation helper; coverage ≥ 70% of the
  new testable logic.

## Design
Add pure scoring types to `Quadspace.Core`: `ScoreEntry(string Name, int Score, DateTimeOffset
AchievedAtUtc)` and a static `Leaderboard` with
`Insert(IReadOnlyList<ScoreEntry> current, ScoreEntry candidate, int cap)` returning a
`LeaderboardInsertResult(IReadOnlyList<ScoreEntry> Entries, int? Rank)`. Ordering is by `Score`
descending, then `AchievedAtUtc` ascending; the list is truncated to `cap`; `Rank` is the 1-based
index of the candidate if it made the cut, else `null`. Also add a pure
`ScoreSubmission.Validate(name, score)` helper returning a normalized name or an error.

In `Quadspace.Host`, add a singleton `FileScoreStore` that owns the `scores/` directory, reads/writes
`top100.json` and appends to `scores/daily/{date}.json`, serializing with `System.Text.Json` (web
options) under a `SemaphoreSlim` lock. Register it in DI and map two minimal-API endpoints in
`Program.cs`: `GET /api/scores/top` and `POST /api/scores`, delegating ranking to `Leaderboard` and
validation to `ScoreSubmission`. Requests/responses use small records in Core
(`ScoreSubmissionRequest`, `ScoreSubmissionResponse`).

## Open Questions / Out of Scope
- Wiring the client UI to these endpoints — features 3 (display) and 6 (submit).
- Any pagination beyond a simple `count`, auth, or rate-limiting — out of scope (no auth by design).

## Task Checklist
- [x] **T1** — Add `ScoreEntry`, `Leaderboard.Insert` (+ result record), and `ScoreSubmission.Validate`
      to `Quadspace.Core`. _(AC: AC1, AC4)_
- [x] **T2** — Add `FileScoreStore` to `Quadspace.Host` (daily append + top100 update via
      `Leaderboard`, async-locked file I/O). _(AC: AC3)_
- [x] **T3** — Map `GET /api/scores/top` and `POST /api/scores` minimal-API endpoints with validation
      and DI registration; add `scores/` to `.gitignore`. _(AC: AC2, AC3, AC4)_
- [x] **T4** — Add xUnit tests for `Leaderboard.Insert` and `ScoreSubmission.Validate`. _(AC: AC5)_
- [x] **T5** — Verify warning-free build, passing tests, clean format. _(AC: AC1–AC5)_

## Test Plan
- **AC1/AC5** → `Leaderboard.Insert` tests: empty list places at rank 1; higher score ranks above
  lower; equal score tie-breaks by earlier time; 101st distinct score above others evicts the lowest
  and caps at 100; a score below the lowest when full is not placed (`Rank == null`); a mid score gets
  the correct rank.
- **AC4** → `ScoreSubmission.Validate` tests: trims surrounding whitespace; rejects empty/whitespace;
  rejects > 50 chars; rejects negative score; accepts a valid entry.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T10:20:00Z | (none yet) | — | tracker created |
| 2026-08-25T10:24:00Z | dotnet build quadspace.sln -c Release | PASS | 0 Warning(s), 0 Error(s) |
| 2026-08-25T10:24:10Z | dotnet test quadspace.sln -c Release | PASS | 18 passed, 0 failed |
| 2026-08-25T10:24:20Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean |
| 2026-08-25T10:24:40Z | API smoke test (:5178) | PASS | empty=[]; POST trims name+stamps UTC, rank 1/2 ordered; count honored; empty-name & negative → 400 |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 12 | 40 | ✅ |
| New files | 8 | 25 | ✅ |
| New projects | 0 | 1 | ✅ |
| New packages | 0 | 3 | ✅ |
| LOC delta | ~470 | 2000 | ✅ |

## Decisions / Escalations
- None.

## Pull Request
- **Url:** https://github.com/john-slo/quadspace/pull/2
- **State:** OPEN (shared initial-game PR)
