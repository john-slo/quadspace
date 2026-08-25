<!--
  Agentic SDD product backlog - the single source of truth for the whole product/epic.
  Location: specs/backlog.md  (exactly one per repo)
  Produced by the product-architect prompt in the BLUEPRINT/BACKLOG phases (see governance/PLANNING.md).
  Machine-parsed: the "## Backlog" table rows and "- **Key:** value" metadata lines are read by the
  orchestrator. Keep those shapes intact. Feature ids are sequential; slugs are kebab-case.
  Rows are executed in order once their dependsOn ids are all DONE. Do NOT create side documents -
  the per-feature spec is written in that feature's specs/<id>-<slug>/tracker.md during SPEC.
-->
# Product Backlog: {{PRODUCT_NAME}}

## Metadata
- **Product:** {{PRODUCT_NAME}}
- **Created:** {{CREATED_UTC}}
- **Updated:** {{UPDATED_UTC}}
- **Blueprint:** governance/PRODUCT.md + governance/ARCHITECTURE.md
- **Status:** DRAFT

## Blueprint Summary
<!-- One paragraph: what the product is and the architecture chosen in BLUEPRINT. Full detail lives
     in governance/PRODUCT.md and governance/ARCHITECTURE.md - summarize, do not duplicate. -->
{{BLUEPRINT_SUMMARY}}

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
| 1 | {{SLUG}} | P0 | - | {{ONE_LINE_OUTCOME}} | DRAFT |

## Run Log
<!-- Append-only. The orchestrator writes one row each time EXECUTE starts/stops or a row transitions. -->
| Timestamp (UTC) | Event | Feature | Note |
| --- | --- | --- | --- |
| {{CREATED_UTC}} | backlog created | - | drafted during BACKLOG |

## Decisions
<!-- Required to add, remove, or re-scope a backlog row after the human checkpoint. Otherwise "None". -->
- None
