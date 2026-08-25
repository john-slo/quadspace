# GUARDRAILS — Disciplined Autonomy

> These principles prevent two failure modes: **drift** (building things nobody asked for) and
> **invisible scope creep** (changes that don't trace back to requirements). The `reviewer` prompt
> enforces them before every PR. When a principle is violated, the run **stops and escalates** — it
> never auto-expands scope, adds dependencies, or weakens tests to "make it work."

## 1. Scope lock

After INTAKE, the tracker's **Requirements** and **Acceptance Criteria** are **frozen**. Editing them
requires a `Decision/Escalation` entry. If new requirements emerge mid-flight, **stop** and escalate.

## 2. Traceability

- **Every** changed file/hunk must map to a **Task** in the tracker.
- **Every** task must map to at least one **Acceptance Criterion**.
- Untraceable changes are rejected. If you need a change with no task, add the task first (within
  scope) or **escalate**.

## 3. YAGNI (smallest viable change)

Do the **least** that satisfies the Acceptance Criteria:

- No new architectural patterns, layers, or abstractions not required by a task.
- No speculative configuration, feature flags, extension points, or generics.
- No refactoring of unrelated code or symbol renaming outside the change.
- Prefer editing existing files over adding new ones; a function over a class, a class over a project.

A larger change budget (for greenfield work) relaxes *quantity*, never YAGNI. A feature that adds
speculative "framework" layers not required by an Acceptance Criterion is still a violation.

## 4. Spec-artifact discipline

The tracker and backlog are the **only** spec artifacts. Do **not** create `DESIGN.md`, `TASKS.md`,
`*_SUMMARY.md`, `*_GUIDE.md`, `*_CHECKLIST.md`, or similar loose docs.

- **Design** → tracker **Design** section. **Tasks** → **Task Checklist** (mapped to ACs).
- **Verification** → **Verification Log**. **Run trail** → **Phase Log**.
- **Open design questions / excluded work** → the tracker's **Open Questions / Out of Scope** section.

Loose docs drift from the tracker and hide real state.

## 5. Canonical phases & visibility

- `Phase` and `Status` are **enums** (see [`PROCESS.md`](PROCESS.md)). Do not invent variants like
  `IMPLEMENT_IN_PROGRESS`; they break resumability.
- Every phase transition updates three places: `Phase` metadata, `ActivePrompt`, and a `Phase Log`
  row. If you cannot tell which phase/prompt is active, **stop and re-read the tracker**.

## 6. Merge gate

- The merge gate is set by `autonomy.autonomousMerge` (see [`GITHUB.md`](GITHUB.md)):
  - **`true` (default):** the agent merges the PR itself, but **only** after the review tool's
    feedback is resolved and required checks are green, via `gh pr merge`. It never *approves/votes*
    on a PR, never merges with red CI or unresolved findings, and never merges to bypass an escalation.
  - **`false`:** the agent **never** merges, approves, votes on, or auto-completes a PR.
- Autonomy means running **many features through the gate** without a human stop between them. It
  never relaxes scope lock, the change budget, or the dependency allowlist.

## 7. Commit hygiene

- **Stage intentionally.** Review `git status --short` before committing; do **not** blindly
  `git add -A`. Sub-agents can leave scratch files (`diff.txt`, `*.tmp`) in the tree — never let them
  land in a commit/PR. Known scratch names are in `.gitignore`.

## Forbidden actions (hard stops)

- Merging with red CI or unresolved review findings; approving or voting on a PR; merging at all when
  `autonomy.autonomousMerge: false`.
- Committing secrets (`.env`, tokens, connection strings) or stray scratch/artifact files.
- Editing frozen Requirements/AC without a Decision entry.
- Inventing non-canonical `Phase`/`Status` values, or skipping REVIEW.
- Creating off-tracker spec documents.
- Disabling analyzers, lowering `TreatWarningsAsErrors`, or deleting tests to pass the build.

## Change budget

`process.config.json > changeBudget` sets a single, tunable budget — **guidance, not an absolute
wall**:

| Metric | Config key |
| --- | --- |
| Max files changed | `maxFilesChanged` |
| Max new files | `maxNewFiles` |
| Max new projects | `maxNewProjects` |
| Max new packages | `maxNewPackages` |
| Max LOC delta | `maxLocDelta` |

Measure usage with `git diff --stat <base>...HEAD` (and count untracked files). Tighten the budget for
maintenance in a mature codebase; relax it for greenfield work where features are naturally larger.

**Overriding a budget:** if a feature genuinely needs to exceed a limit, write a `Decision/Escalation`
entry stating (1) **what** exceeded and by how much, (2) **why** it is necessary to meet an Acceptance
Criterion (not speculative), (3) **evidence** (linked tasks/criteria), and (4) the **smaller
alternative** you rejected and why. Valid example: *"maxNewFiles 45 vs 40: AC1 requires full CRUD for
Orders — 23 new resource/service/repo classes + 20 test files; GET-only rejected as it fails AC1."*
Invalid: *"added a base framework layer"* (speculative — escalate). No Decision entry + budget
exceeded = automatic escalation.

## When guardrails trip

1. Write a `## Decision / Escalation` entry in the tracker: what tripped, evidence, and options.
2. Set tracker `Status: ESCALATED` and **stop**.
3. Do **not** auto-expand scope, add dependencies, exceed limits, or weaken tests to work around it.

Escalation is a **first-class outcome**, not a failure to hide.
