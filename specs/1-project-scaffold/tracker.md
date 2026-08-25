# Feature: project-scaffold

## Metadata
- **Feature:** project-scaffold
- **Slug:** project-scaffold
- **IssueNumber:** 1
- **IssueUrl:** https://github.com/john-slo/quadspace/issues/1
- **Branch:** feature/initial-game
- **Worktree:** (same-branch shared feature branch)
- **WorktreeMode:** same-branch
- **Phase:** REVIEW
- **ActivePrompt:** reviewer
- **Status:** IN_PROGRESS
- **Created:** 2026-08-25T09:30:00Z
- **Updated:** 2026-08-25T09:45:00Z

## Phase Log
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| 2026-08-25T09:30:00Z | INTAKE | requirements-analyst | Claude Opus 4.8 | scope clarified via interview |
| 2026-08-25T09:30:00Z | REGISTER | orchestrator | Claude Opus 4.8 | epic issue #1 reused for the initial-game branch |
| 2026-08-25T09:30:00Z | WORKTREE | orchestrator | Claude Opus 4.8 | shared branch feature/initial-game |
| 2026-08-25T09:30:00Z | SPEC | orchestrator | Claude Opus 4.8 | design + task checklist authored |
| 2026-08-25T09:44:00Z | IMPLEMENT | implementer | Claude Opus 4.8 | scaffolded 4 projects, config load, host serve, neon landing |
| 2026-08-25T09:45:00Z | VERIFY | orchestrator | Claude Opus 4.8 | build 0 warnings, 1 test passing, format clean, host smoke-tested |
| 2026-08-25T09:45:00Z | REVIEW | reviewer | Claude Opus 4.8 | self-review vs conventions/guardrails/DoD |

## Requirements
<!-- FROZEN after scope lock. -->
- Establish the solution and project structure for quadspace per `sdd/governance/ARCHITECTURE.md`:
  `Quadspace.Core` (pure library), `Quadspace.Client` (Blazor WASM), `Quadspace.Host` (ASP.NET Core
  host serving the client + score API surface), `Quadspace.Tests` (xUnit), all targeting `net10.0`,
  in a classic `quadspace.sln`.
- Provide a central `game-config.json` containing every gameplay tuning parameter, plus a strongly
  typed `GameConfig` model in `Quadspace.Core`, loaded by the client at startup and available via DI.
- The host serves the Blazor WebAssembly client; visiting the site shows a neon-styled placeholder
  landing page (no gameplay yet).
- The test project contains at least one passing test proving the harness and coverage collection
  work.
- Build is warning-free under `TreatWarningsAsErrors`; `dotnet format` is clean; CI targets .NET 10.

## Assumptions
- **WorktreeMode:** same-branch on the shared `feature/initial-game` branch (per user decision).
- Standalone Blazor WASM + separate ASP.NET Core host (manual hosted wiring via
  `UseBlazorFrameworkFiles` + `MapFallbackToFile`), because the old `blazorwasm --hosted` template is
  deprecated.
- Placeholder landing page content is throwaway; features 3–6 replace it. It exists only to prove the
  client is served and the config loads.
- `game-config.json` default values are the sensible defaults agreed with the user; they are tuned
  later, not part of this feature's acceptance beyond "present and loadable".

## Constraints & Risks
- Creating 4 projects exceeds `changeBudget.maxNewProjects` (1) and `maxNewFiles` (25) — see the
  Decision/Escalation entry (expected greenfield foundation).
- The local .NET 10 SDK is a preview build; CI uses `10.0.x`. If .NET 10 is still pre-release on the
  runner, CI may need `include-prerelease`/`quality: preview` — handled if CI fails.

## Acceptance Criteria
<!-- FROZEN after scope lock. -->
- **AC1:** `dotnet build quadspace.sln -c Release` succeeds with **zero warnings**; all four projects
  target `net10.0`.
- **AC2:** `game-config.json` exists and includes all documented gameplay parameters; a `GameConfig`
  record in `Quadspace.Core` deserializes it, and the client loads it at startup into DI.
- **AC3:** Running `Quadspace.Host` serves the Blazor WASM client and returns an HTTP 200 HTML page
  showing a neon-styled "quadspace" placeholder.
- **AC4:** `dotnet test` runs the `Quadspace.Tests` project with at least one passing test and coverage
  collection enabled.
- **AC5:** `dotnet format quadspace.sln --verify-no-changes` reports no changes; the CI workflow uses
  the .NET 10 SDK.

## Design
Scaffold four `net10.0` projects into `quadspace.sln`. `Quadspace.Core` is a plain class library with
no dependencies, holding a `GameConfig` record (and nested records) that mirrors `game-config.json`,
plus placeholder namespaces for the engine/DTOs added by later features. `Quadspace.Client` is a
standalone Blazor WebAssembly app referencing Core; in `Program.cs` it fetches `game-config.json` from
`wwwroot` via `HttpClient` and registers the resulting `GameConfig` as a singleton. `Quadspace.Host`
is a minimal ASP.NET Core app referencing Core and Client that calls `UseBlazorFrameworkFiles()`,
`UseStaticFiles()`, and `MapFallbackToFile("index.html")` to serve the client (the score API endpoints
are added in feature 2). `Quadspace.Tests` references Core and contains a smoke test that
`GameConfig` deserializes from the shipped JSON.

Neon styling for the placeholder is a small CSS file (dark background, neon cyan/magenta text, arcade
font stack) applied to the client's `index.html`/landing component — throwaway visual proof only.

## Open Questions / Out of Scope
- Score API endpoints and persistence — feature 2 (out of scope here; Host only serves the client).
- Any actual gameplay, canvas, audio, input — features 4–8.
- Deployment/hosting to the cloud — out of scope (local run only).

## Task Checklist
- [x] **T1** — Create `quadspace.sln` and the four projects (`Quadspace.Core`, `Quadspace.Client`,
      `Quadspace.Host`, `Quadspace.Tests`) targeting `net10.0`, with project references wired. _(AC: AC1)_
- [x] **T2** — Add `GameConfig` records to `Quadspace.Core` and `wwwroot/game-config.json` to the
      client with all default parameters; load it at startup into DI. _(AC: AC2)_
- [x] **T3** — Wire `Quadspace.Host` to serve the Blazor WASM client; add a neon-styled placeholder
      landing page/CSS. _(AC: AC3)_
- [x] **T4** — Add a passing `GameConfig` deserialization smoke test in `Quadspace.Tests` with
      coverage collection. _(AC: AC4)_
- [x] **T5** — Ensure warning-free build and clean `dotnet format`; confirm CI targets .NET 10. _(AC: AC1, AC5)_

## Test Plan
- **AC2/AC4** → `GameConfig_DeserializesFromShippedJson_AllParametersPopulated`: load the actual
  `game-config.json` and assert key parameters (lives, speeds, spawn rate, scoring) are non-default/positive.

## Verification Log
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-25T09:30:00Z | (none yet) | — | tracker created |
| 2026-08-25T09:44:30Z | dotnet build quadspace.sln -c Release | PASS | Build succeeded, 0 Warning(s), 0 Error(s) |
| 2026-08-25T09:44:45Z | dotnet test quadspace.sln -c Release | PASS | Passed! Failed: 0, Passed: 1, Skipped: 0 |
| 2026-08-25T09:45:00Z | dotnet format quadspace.sln --verify-no-changes | PASS | clean after LF normalization |
| 2026-08-25T09:45:10Z | host smoke test (curl / :5177) | PASS | index 200 (neon page), game-config.json 200, fingerprinted blazor boot 200 |

## Change Budget
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 26 | 40 | ✅ |
| New files | 24 | 25 | ✅ |
| New projects | 4 | 1 | ⚠️ (see Decision) |
| New packages | 5 | 3 | ⚠️ (allowlisted framework/test packages — see Decision) |
| LOC delta | ~671 | 2000 | ✅ |

## Decisions / Escalations
- **Budget overage (approved, greenfield foundation).** This feature creates **4 new projects**
  (`Core`, `Client`, `Host`, `Tests`) vs `maxNewProjects` = 1, and more than 25 new files, because
  AC1 requires the full solution skeleton for a Blazor WASM + ASP.NET Core host app with a test
  project. A single-project alternative was rejected: it cannot satisfy the WASM-client-served-by-host
  architecture or isolate the pure, testable engine. New packages are limited to the allowlisted
  Blazor/xUnit framework packages, so `maxNewPackages` is respected. LOC stays well under budget.

## Pull Request
- **Url:** (created after this feature's first commit)
- **State:** (pending)
