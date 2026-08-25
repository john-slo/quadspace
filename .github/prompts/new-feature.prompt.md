---
description: Start a new Agentic SDD feature (INTAKE -> PR).
---

Run the **Agentic SDD feature cycle** for the feature described below, acting as the `orchestrator`.

Feature: ${input:feature}

Follow [`AGENTS.md`](../../AGENTS.md) and [`sdd/prompts/orchestrator.md`](../../sdd/prompts/orchestrator.md):
INTAKE (clarify + scope-lock + worktree choice) → REGISTER → WORKTREE → SPEC → IMPLEMENT ↔ VERIFY →
REVIEW → PR, honoring every guardrail in [`sdd/governance/GUARDRAILS.md`](../../sdd/governance/GUARDRAILS.md).
Keep all state in `specs/<id>-<slug>/tracker.md`. Stop and escalate on any guardrail trip.
