# PROCESS — The Spec-Driven Development (SDD) Cycle

> The canonical definition of the Agentic SDD process. It runs on the **GitHub Copilot CLI** and
> produces the **same artifacts** every time. Companion docs: [`PLANNING.md`](PLANNING.md),
> [`GUARDRAILS.md`](GUARDRAILS.md), [`CONVENTIONS.md`](CONVENTIONS.md), [`GITHUB.md`](GITHUB.md),
> [`SKILLS.md`](SKILLS.md), [`TOOLS.md`](TOOLS.md). The driving agent's entry point is the
> repo-root [`AGENTS.md`](../../AGENTS.md).

## Two nested cycles

```
  PRODUCT CYCLE (optional, once per product/epic)      FEATURE CYCLE (once per feature)
  ===============================================      ================================
   BLUEPRINT  design the product, pick the              INTAKE -> REGISTER -> WORKTREE -> SPEC
              architecture, enumerate features            -> IMPLEMENT<->VERIFY -> REVIEW -> PR
       |                                                          (one tracker per feature)
       v                                                                  ^
   BACKLOG    write specs/backlog.md: an ordered,                         |
              dependency-aware feature catalog     ── for each READY ─────┘
                                                       feature, autonomously
```

- The **product cycle** (BLUEPRINT + BACKLOG) is a lightweight up-front pass for greenfield apps or
  epics, so features are specced in advance. A single small change **skips it** and starts at INTAKE.
  See [`PLANNING.md`](PLANNING.md).
- The **feature cycle** is the classic INTAKE→PR loop. The agent walks the backlog and runs it for
  each `READY` item, **without stopping between features** (autonomy). Each feature ends at its merge
  gate: `autonomy.autonomousMerge: true` (default) merges after review + CI; `false` stops at the PR
  for a human.
- A run can be resumed purely from `specs/backlog.md` + each feature `tracker.md`.

## Principles

1. **One tracker is the single source of truth.** `specs/<id>-<slug>/tracker.md` holds all state for
   a feature; `specs/backlog.md` holds product state. Every phase writes to the tracker.
2. **Trackers and the backlog are the *only* spec artifacts.** Do **not** create side documents
   (`DESIGN.md`, `TASKS.md`, `*_SUMMARY.md`, `*_GUIDE.md`, `*_CHECKLIST.md`). Design goes in the
   tracker's **Design** section, tasks in **Task Checklist**, verification in **Verification Log**.
   Loose docs are drift — see [`GUARDRAILS.md`](GUARDRAILS.md).
3. **Scope is locked after intake.** Requirements + Acceptance Criteria are frozen. Any expansion
   requires an explicit `Decision/Escalation` entry — otherwise the run **stops and escalates**.
4. **Smallest change that satisfies the acceptance criteria.** No speculative abstractions, no
   unrelated refactors (YAGNI, [`GUARDRAILS.md`](GUARDRAILS.md)).
5. **Guardrails over trust.** Change budget, dependency allowlist, traceability, and bounded loops
   are checked by the `reviewer` before every PR, not left to good intentions.
6. **Every phase is visible.** The tracker always records the current `Phase`, the `ActivePrompt`
   driving it, and an append-only `Phase Log`, so a human can see exactly which step is running.

## Artifacts

| Artifact | Location | Produced in phase |
| --- | --- | --- |
| Feature backlog | `specs/backlog.md` | Backlog (product cycle) |
| Feature request | `specs/<id>-<slug>/feature-request.md` | Intake |
| GitHub Issue | GitHub (number stored in tracker) | Register |
| Feature branch | `feature/<id>-<slug>` | Worktree |
| Git worktree *(if dedicated)* | `<worktreeRoot>/<id>-<slug>` | Worktree |
| **Tracker** | `specs/<id>-<slug>/tracker.md` | Spec (updated every phase) |
| Implementation + tests | source tree | Implement |
| Pull request (linked via `Closes #N`) | GitHub | PR |

`<id>` is the GitHub issue number (or `TBD` until Register). `<slug>` is a kebab-case slug of the
title. There is **exactly one** `specs/backlog.md` per repo and **exactly one** `tracker.md` per
feature.

> **Backlog `Id` vs. feature `<id>`.** A backlog row's `Id` is just a sequential ordering/priority
> key. The feature's folder, branch, and tracker use `<id>` = the **GitHub issue number** created in
> REGISTER (they are not the same thing, though for the first feature in a fresh repo they often
> coincide). Always name `specs/<issue-number>-<slug>/` from the issue number, not the backlog Id.

## Canonical Phase & Status values (do not invent variants)

The tracker `Phase` and `Status` fields are **enums**. Using any other value (e.g.
`IMPLEMENT_IN_PROGRESS`) breaks resumability.

| Field | Allowed values |
| --- | --- |
| `Phase` | `INTAKE`, `REGISTER`, `WORKTREE`, `SPEC`, `IMPLEMENT`, `VERIFY`, `REVIEW`, `PR`, `DONE` |
| `Status` | `IN_PROGRESS`, `ESCALATED`, `BLOCKED`, `DONE` |

Progress *within* a phase belongs in the **Task Checklist** and **Phase Log**, not in the `Phase`
value. Backlog rows use `Status`: `DRAFT`, `READY`, `IN_PROGRESS`, `DONE`, `BLOCKED`.

### Prompt driving each phase

Each phase is driven by a specific prompt/playbook in [`../prompts/`](../prompts). The `ActivePrompt`
metadata and the `Phase Log` **Prompt** column record it.

| Phase | Prompt |
| --- | --- |
| INTAKE | `requirements-analyst` |
| REGISTER | `orchestrator` |
| WORKTREE | `orchestrator` |
| SPEC | `orchestrator` (or `product-architect` in the product cycle) |
| IMPLEMENT | `implementer` (or `test-author`) |
| VERIFY | `implementer` / `test-author` / `orchestrator` |
| REVIEW | `reviewer` |
| PR | `orchestrator` |
| DONE | `-` |

## State machine (feature cycle)

```
   INTAKE   clarify or record assumptions; write acceptance criteria; SCOPE LOCK
     |
   REGISTER create the GitHub issue -> capture <id>
     |
   WORKTREE (if dedicated) create branch feature/<id>-<slug> + git worktree
     |               (if same-branch) skip; edits stay on the current branch
   SPEC     write tracker.md (design + task checklist, each task mapped to an AC)
     |
   IMPLEMENT  code + tests (in scope)  <----+   bounded loop
     |                                       |   (loops.maxImplementIterations,
   VERIFY     build + test + format  --FAIL--+    loops.maxBuildRetries)
     | PASS
   REVIEW   diff vs CONVENTIONS + GUARDRAILS + budget + traceability + DoD
     |
   PR       create the PR (Closes #<id>); then the MERGE GATE:
     |         autonomousMerge=true  -> request review tool, wait for review + CI,
     |                                  address feedback, then MERGE (squash) & continue
     |         autonomousMerge=false -> STOP (a human approves/merges)
   DONE     (autonomy) mark the backlog row DONE, start the next READY item

   ESCALATED / BLOCKED are reachable from any phase on a guardrail stop.
```

Terminal states: **DONE**, **ESCALATED** (stopped for a human decision), **BLOCKED** (external
blocker, recorded in the tracker).

## Phases

### 1. INTAKE
- Input: a feature description (free text, a `feature-request.md`, an existing GitHub issue number,
  or a **backlog row** `Outcome` selected during the product cycle).
- The `requirements-analyst` prompt produces clarified **Requirements**, explicit **Assumptions**,
  and testable, numbered **Acceptance Criteria**.
- **Clarify (lightweight):** if a human is available, ask a few high-impact clarifying questions
  first. Record open questions you cannot resolve as explicit **Assumptions** and proceed — do not
  block. Also ask the **branch question**: how should this feature's changes be isolated? Record
  `WorktreeMode` in Assumptions as one of:
  - `branch` — a dedicated feature branch checked out in place (the simple default; recommended for a
    single-agent CLI run).
  - `worktree` — a dedicated branch **and** a separate git worktree directory (use when you need the
    main working copy left untouched, e.g. parallel work).
  - `same-branch` — edits stay on the current branch (small change, no PR isolation).
- **Scope lock:** once Acceptance Criteria are recorded, Requirements and Acceptance Criteria are
  **frozen**. Later edits require a `Decision/Escalation` entry.

### 2. REGISTER
- Create the GitHub issue from the Requirements + Acceptance Criteria (see [`GITHUB.md`](GITHUB.md)).
- Persist the returned **issue number** and URL into the tracker Metadata. Idempotent: if the tracker
  already has an issue number, skip creation.

### 3. WORKTREE (conditional on `WorktreeMode`)
- `branch` (default) → create the feature branch in place: `git checkout -b feature/<id>-<slug>`.
  No separate directory. Idempotent (skip if already on the branch).
- `worktree` → create branch **and** a git worktree under `git.worktreeRoot`
  (`git worktree add <root>/<id>-<slug> -b feature/<id>-<slug> <targetBranch>`), then work there.
- `same-branch` → **skip this phase**; edits happen on the current branch.
- In all cases, never commit directly to `github.targetBranch`.

### 4. SPEC
- Generate `tracker.md` from [`../templates/tracker.template.md`](../templates/tracker.template.md)
  if not present.
- Author the **Design** section (smallest viable approach, a couple of paragraphs) and a **Task
  Checklist** where **every task maps to at least one Acceptance Criterion**.
- Record any genuinely open design questions or consciously-excluded work in the tracker's **Open
  Questions / Out of Scope** section — briefly, no side documents, no auto-created issues.
- If the Design will not fit one tracker, the feature is too big: split it into more backlog rows.

### 5. IMPLEMENT ↔ VERIFY (bounded loop)
- The `implementer` prompt changes **only in-scope files** (files traceable to a task).
- The `test-author` prompt adds tests when `quality.requireTests` is true or a task needs them.
- **VERIFY** runs the configured build/test/format commands (`stack.*`, default `dotnet`).
- On failure, loop back to IMPLEMENT with the parsed errors. The loop is **bounded** by
  `loops.maxImplementIterations` and `loops.maxBuildRetries`. On exceeding a bound → **ESCALATE**.

### 6. REVIEW (mandatory)
- The `reviewer` prompt checks the diff against [`CONVENTIONS.md`](CONVENTIONS.md),
  [`GUARDRAILS.md`](GUARDRAILS.md), the change budget, **traceability** (every changed hunk maps to a
  task; every task maps to an acceptance criterion), and the **Definition of Done** below.
- REVIEW is never skipped. Any failure that cannot be fixed within scope → **ESCALATE**.
- For a large diff, the driving agent may run REVIEW as a separate sub-agent (fresh context) so the
  review is independent of the implementation.

### 7. PR (create + merge gate)
- Ensure the PR exists: push the branch and create the PR with `Closes #<issueNumber>` in the body
  (see [`GITHUB.md`](GITHUB.md)). When `autonomy.autonomousPrReview: false`, stop after REVIEW for a
  human to create the PR.
- Then run the **merge gate**, set by `autonomy.autonomousMerge`:
  - **`true` (default) — the agent merges.** Bounded loop (`review.maxRounds`): if
    `review.waitForReviewTool`, request the review bot (`review.reviewer`) and wait for its review;
    address findings and red CI on the branch (commit + push), **re-requesting the reviewer after
    every commit**; repeat until the review is clean and (if `review.requireCiGreen`) checks are
    green; then **merge** (`github.mergeMethod`, deleting the branch per `github.deleteBranchOnMerge`)
    and sync the target branch locally. Set `Status: DONE` only **after** a successful merge.
    Exceeding `review.maxRounds`, an un-addressable finding, or CI that will not go green →
    `Decision/Escalation` and **ESCALATE** (do not merge).
  - **`false` — human merge gate.** The agent **stops** at PR creation. It never approves, votes,
    merges, or auto-completes. A human performs the merge.
- If this feature came from `specs/backlog.md`, mark that row `DONE` (recording the PR link) and
  continue to the next `READY` row.

## Bounded loops & escalation

Every loop has an explicit cap from config. When a cap is hit, the run **stops** and writes a
`Decision/Escalation` entry describing the blocker and the last attempts. It never silently broadens
scope, adds a dependency, or exceeds the change budget to "make it work." Escalation is a first-class
terminal outcome, not a failure to hide.

## Definition of Done (DoD)

A feature is **DONE** only when **all** of the following hold — the `reviewer` verifies them before
PR, and CI ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)) re-checks build/test/format:

- [ ] Every Acceptance Criterion is satisfied and mapped to at least one completed task.
- [ ] **No scope creep:** all changes are traceable to a task; no unlogged Requirements edits.
- [ ] **Within change budget:** files/LOC/new-files/new-projects/new-packages under `changeBudget`
      limits unless a `Decision/Escalation` entry approves the overage.
- [ ] **No unapproved dependencies:** any new package is in `allowedPackages` or has a Decision entry.
- [ ] The configured **build** succeeds with **no warnings** (warnings-as-errors on .NET).
- [ ] The configured **tests** pass; required logic has tests; coverage ≥ `quality.coverageThreshold`.
- [ ] The configured **format check** is clean (`dotnet format --verify-no-changes` by default).
- [ ] Tracker is complete (all sections filled, no `TODO`/`{{ }}` placeholders) and committed.
- [ ] Tracker `Phase`/`Status` use only the canonical enum values; the `Phase Log` records the run.
- [ ] **No off-tracker spec docs** were created.
- [ ] GitHub issue exists and is **linked** to the PR via `Closes #<issueNumber>` in the PR body.
- [ ] PR opened using the PR template. With `autonomy.autonomousMerge: false`, the PR is **not**
      merged/approved by the agent (human gate); with `true`, it is merged only **after** review-tool
      feedback is resolved and required checks are green.
- [ ] No secrets committed (`.env`, tokens, etc.).

## Tracker lifecycle

1. **Created** in SPEC (`Status: IN_PROGRESS`, `Phase: SPEC`).
2. **Updated** at the start/end of every phase: `Phase`, `ActivePrompt`, task statuses, a `Phase Log`
   row, verification log, decisions.
3. **Frozen sections:** Requirements + Acceptance Criteria after INTAKE scope-lock.
4. **Finalized** in PR/DONE: `Status: DONE`, `ActivePrompt: -`, PR link recorded (or `ESCALATED` /
   `BLOCKED`).
