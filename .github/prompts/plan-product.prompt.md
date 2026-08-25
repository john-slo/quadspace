---
description: Run the Agentic SDD product cycle (BLUEPRINT + BACKLOG) for a greenfield app or epic.
---

Run the **Agentic SDD product cycle** as the `product-architect`, for the product/epic described below.

Product / epic: ${input:product}

Follow [`sdd/prompts/product-architect.md`](../../sdd/prompts/product-architect.md) and
[`sdd/governance/PLANNING.md`](../../sdd/governance/PLANNING.md): **BLUEPRINT** (fill
`sdd/governance/PRODUCT.md` + `sdd/governance/ARCHITECTURE.md`) then **BACKLOG** (write
`specs/backlog.md` as an ordered, dependency-aware catalog of small, PR-sized features). Produce specs
only — no code. When `planning.humanReviewBacklog` is true, present the backlog for review, then hand
off to the `orchestrator` to EXECUTE.
