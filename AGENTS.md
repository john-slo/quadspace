# AGENTS.md — Agentic SDD (GitHub Copilot CLI)

> This repository runs the **Agentic Spec-Driven Development (SDD)** process on the **GitHub Copilot
> CLI**. You are the driving agent. Follow this file and the governance docs it points to. The whole
> point of SDD is disciplined autonomy: **plan the scope, lock it, implement the smallest change that
> satisfies it, review, and ship — with every step visible in one tracker.**

## Read first
- [`sdd/governance/PROCESS.md`](sdd/governance/PROCESS.md) — the phase-by-phase state machine + Definition of Done.
- [`sdd/governance/GUARDRAILS.md`](sdd/governance/GUARDRAILS.md) — the hard rules (scope lock, traceability, YAGNI, budgets, merge gate).
- [`sdd/governance/PLANNING.md`](sdd/governance/PLANNING.md) — the optional product/backlog cycle for greenfield apps.
- `process.config.json` — repo settings (GitHub coordinates, stack commands, autonomy, budgets).

## The phase prompts (your playbooks)
Run the phases in order, following the matching prompt. The `orchestrator` is your top-level driver.

| Phase | Prompt |
| --- | --- |
| (product cycle) | [`sdd/prompts/product-architect.md`](sdd/prompts/product-architect.md) |
| driver | [`sdd/prompts/orchestrator.md`](sdd/prompts/orchestrator.md) |
| INTAKE | [`sdd/prompts/requirements-analyst.md`](sdd/prompts/requirements-analyst.md) |
| IMPLEMENT | [`sdd/prompts/implementer.md`](sdd/prompts/implementer.md) · [`sdd/prompts/test-author.md`](sdd/prompts/test-author.md) |
| REVIEW | [`sdd/prompts/reviewer.md`](sdd/prompts/reviewer.md) |

## The feature cycle (short form)
`INTAKE → REGISTER → WORKTREE → SPEC → IMPLEMENT ↔ VERIFY → REVIEW → PR → DONE`

1. **INTAKE** — clarify (a few high-impact questions if a human is present, else record assumptions),
   write numbered, testable Acceptance Criteria, choose `WorktreeMode` (branch / worktree /
   same-branch), then **scope-lock**.
2. **REGISTER** — create the GitHub issue; store its number in the tracker.
3. **WORKTREE** — per `WorktreeMode`: `branch` (default, `git checkout -b feature/<id>-<slug>` in
   place), `worktree` (also a separate git worktree dir), or `same-branch` (skip). Never commit to
   the target branch.
4. **SPEC** — write `specs/<id>-<slug>/tracker.md`: Design + a Task Checklist where every task maps to
   an AC. Note open questions / out-of-scope briefly in the tracker. **No side documents.**
5. **IMPLEMENT ↔ VERIFY** — smallest in-scope change + tests; run the configured build/test/format;
   loop within `loops.*`; escalate on exceed.
6. **REVIEW** — mandatory. Check conventions, guardrails, budget, traceability, DoD. Escalate
   unfixable-in-scope findings.
7. **PR** — create the PR with `Closes #<issueNumber>`, then run the merge gate per
   `autonomy.autonomousMerge` (default `true` = the agent merges after the review tool + CI are green;
   `false` = a human merges).

## Non-negotiables
- **One tracker is the source of truth.** Every phase updates `Phase`, `ActivePrompt`, and a `Phase
  Log` row. Never invent `Phase`/`Status` values.
- **Scope is frozen after INTAKE.** Changing Requirements/Acceptance Criteria needs a
  `Decision/Escalation` entry.
- **Smallest viable change (YAGNI).** No speculative abstractions, no unrelated refactors.
- **Stay in budget** (`process.config.json > changeBudget`); exceeding it needs a Decision entry.
- **Never** merge with red CI or unresolved review, approve/vote on a PR, commit secrets, or create
  off-tracker spec docs.
- When a guardrail trips: write a `Decision/Escalation`, set `Status: ESCALATED`, **stop**.

## GitHub & tools
GitHub is reached via the Copilot CLI's built-in GitHub tools + `gh` CLI (auth from `gh auth login` —
no PAT). See [`sdd/governance/GITHUB.md`](sdd/governance/GITHUB.md) and
[`sdd/governance/TOOLS.md`](sdd/governance/TOOLS.md). Build/test/format commands come from
`process.config.json > stack` and default to .NET/Blazor; override them for other stacks.
