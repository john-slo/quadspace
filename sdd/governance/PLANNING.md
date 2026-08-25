# PLANNING — Product Cycle & Backlog

> A **lightweight** up-front pass that runs once before any feature on a greenfield app or a new
> epic. It exists so the agent isn't forced to discover architecture and scope one feature at a time.
> For a single small change to an existing app, **skip this** and start the feature cycle at INTAKE.
> Companion: [`PROCESS.md`](PROCESS.md), [`GUARDRAILS.md`](GUARDRAILS.md).

The `product-architect` prompt drives BLUEPRINT + BACKLOG. The `orchestrator` drives EXECUTE.

## When to run

- **Greenfield app or a new epic:** run BLUEPRINT + BACKLOG before the first INTAKE.
- **A single small change:** skip; start at INTAKE.
- **Resuming:** if `specs/backlog.md` already exists, do **not** re-run BLUEPRINT/BACKLOG — read the
  backlog and continue EXECUTE from the next `READY` row.

Enable/disable via `process.config.json > planning.productCycle`.

## BLUEPRINT — design the product once

Fill the two governance docs so no feature re-invents architecture. Output lives **entirely** in
these two files — no new documents.

1. **Ground the product** — [`PRODUCT.md`](PRODUCT.md): what it is, primary users and
   jobs-to-be-done, domain glossary, non-goals, quality bar.
2. **Fix the architecture** — [`ARCHITECTURE.md`](ARCHITECTURE.md): solution layout, projects and
   responsibilities, chosen patterns, dependency allowlist, and product-level decisions (hosting,
   database, auth, real-time). A feature that needs to change one of these must **escalate**.

## BACKLOG — enumerate every feature once

1. Write **`specs/backlog.md`** from [`../templates/backlog.template.md`](../templates/backlog.template.md).
2. Decompose the product into **small, PR-sized features**. Each must fit one tracker and one change
   budget ([`GUARDRAILS.md`](GUARDRAILS.md)). If it won't, split it into more rows — never grow the
   budget.
3. For every row capture: `Id` (sequential), `Slug` (kebab-case), `Priority` (P0/P1/P2), `DependsOn`
   (row ids or `-`), a one-line `Outcome`, and `Status` (`READY` when specced enough to execute, else
   `DRAFT`).
4. Order rows so dependencies come first.

**Human checkpoint (optional, once):** when `planning.humanReviewBacklog` is true, a human reviews
`specs/backlog.md` before EXECUTE. This is the natural place for up-front steering; after it,
execution is autonomous.

## EXECUTE — process the backlog autonomously

The `orchestrator` loops until no `READY` rows remain or an autonomy limit is hit:

1. Pick the highest-priority `READY` row whose `DependsOn` are all `DONE`.
2. Mark it `IN_PROGRESS` in `specs/backlog.md`.
3. Run the **feature cycle** ([`PROCESS.md`](PROCESS.md)), creating `specs/<id>-<slug>/tracker.md`.
   The row's `Outcome` seeds INTAKE.
4. On the feature reaching `DONE`, mark the row `DONE` (record the PR link) and go to step 1 —
   **no human stop between features**.
5. Stop when: no `READY` rows remain, `autonomy.maxFeaturesPerRun` is reached, or a feature ends
   `ESCALATED`/`BLOCKED`. Record where you stopped in the backlog Run Log.

### What autonomy does and does not mean

- **Does:** run many features through the merge gate without asking a human between them; create one
  branch, issue, tracker, and PR per feature; and — with `autonomy.autonomousMerge: true` (default) —
  merge each PR after its review tool + CI are green, then continue.
- **Does not:** approve or vote on a PR; merge with red CI or unresolved findings; merge at all when
  `autonomy.autonomousMerge: false`; exceed a change budget; add an unlisted dependency; edit frozen
  scope; or invent new architecture. Any of these **stops and escalates**.

## Greenfield note

Early features often build foundation code (domain models, test harness, CI). That is still a
legitimate feature with its own Acceptance Criteria — not side work. Such features are naturally
larger, so tune `changeBudget` upward for greenfield stages; YAGNI still applies (every new
class/layer traces to a task, and every task to an AC).

## Anti-drift

- The backlog + the two governance docs are the **only** planning artifacts. No `ROADMAP.md`,
  `PLAN.md`, or per-feature `DESIGN.md`/`TASKS.md`.
- Do not implement during BLUEPRINT/BACKLOG — planning produces specs, not code.
- Do not over-specify a row into a mini-tracker; the real spec is written in the feature's tracker
  during its SPEC phase.
