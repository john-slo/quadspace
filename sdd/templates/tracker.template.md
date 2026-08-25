<!--
  Agentic SDD tracker — the single source of truth for one feature.
  Location: specs/<id>-<slug>/tracker.md
  Keep the `## <Section>` headings and `- **Key:** value` metadata shapes intact (they are the
  process's memory). Replace the placeholder tokens and remove TODO markers before the feature is
  DONE.
-->
# Feature: {{TITLE}}

## Metadata
- **Feature:** {{TITLE}}
- **Slug:** {{SLUG}}
- **IssueNumber:** {{ISSUE_NUMBER}}
- **IssueUrl:** {{ISSUE_URL}}
- **Branch:** {{BRANCH}}
- **Worktree:** {{WORKTREE}}
- **WorktreeMode:** {{WORKTREE_MODE}}
- **Phase:** INTAKE
- **ActivePrompt:** requirements-analyst
- **Status:** IN_PROGRESS
- **Created:** {{CREATED_UTC}}
- **Updated:** {{UPDATED_UTC}}
<!--
  Phase and Status are ENUMS - use only the canonical values from governance/PROCESS.md.
    Phase:  INTAKE | REGISTER | WORKTREE | SPEC | IMPLEMENT | VERIFY | REVIEW | PR | DONE
    Status: IN_PROGRESS | ESCALATED | BLOCKED | DONE
  ActivePrompt is the prompt currently driving work; set it to "-" at DONE.
  WorktreeMode: branch (dedicated branch in place, default) | worktree (branch + separate dir) | same-branch.
  Keep Phase/ActivePrompt/Updated in sync with the latest Phase Log row (there is no auto-updater).
-->

## Phase Log
<!-- Append-only. One row per phase transition showing what happened, who drove it, and which model. -->
| Timestamp (UTC) | Phase | Prompt | Agent Model | Note |
| --- | --- | --- | --- | --- |
| {{CREATED_UTC}} | INTAKE | requirements-analyst | {{AGENT_MODEL}} | intake started |

## Requirements
<!-- FROZEN after scope lock. Edits require a Decisions / Escalations entry. -->
{{REQUIREMENTS}}

## Assumptions
<!-- Open questions answered autonomously (each is a risk if wrong) + WorktreeMode. -->
- {{ASSUMPTION}}

## Constraints & Risks
<!-- Non-requirements that affect the design: dependencies, pre-existing issues, known unknowns. -->
- {{CONSTRAINT}}

## Acceptance Criteria
<!-- FROZEN after scope lock. Testable; each maps to >=1 task. -->
- **AC1:** {{ACCEPTANCE_CRITERION}}

## Design
<!-- Smallest viable approach, a couple of paragraphs. No speculative abstractions (see GUARDRAILS.md). -->
{{DESIGN}}

## Open Questions / Out of Scope
<!-- Genuinely open design questions still to resolve, and work consciously excluded (with a reason).
     Keep it brief — this replaces heavyweight "frontier mapping". No auto-created issues. -->
- {{OPEN_QUESTION_OR_EXCLUSION}}

## Task Checklist
<!-- Each task references the AC(s) it satisfies. Toggle [ ] -> [x] as work completes. -->
- [ ] **T1** — {{TASK_DESCRIPTION}}  _(AC: AC1)_

## Test Plan
<!-- Tests to add/adjust; each ties back to an AC. -->
- **AC1** → {{TEST_DESCRIPTION}}

## Verification Log
<!-- Append-only. One entry per build/test run: timestamp, command, result. -->
| Timestamp (UTC) | Command | Result | Notes |
| --- | --- | --- | --- |
| {{UPDATED_UTC}} | (none yet) | — | tracker created |

## Change Budget
<!-- Snapshot from `git diff --stat <targetBranch>...HEAD` + untracked count; vs process.config.json > changeBudget. -->
| Metric | Used | Limit | OK |
| --- | --- | --- | --- |
| Files changed | 0 | {{MAX_FILES_CHANGED}} | ✅ |
| New files | 0 | {{MAX_NEW_FILES}} | ✅ |
| New projects | 0 | {{MAX_NEW_PROJECTS}} | ✅ |
| New packages | 0 | {{MAX_NEW_PACKAGES}} | ✅ |
| LOC delta | 0 | {{MAX_LOC_DELTA}} | ✅ |

## Decisions / Escalations
<!-- Required to change frozen scope, add a dependency, or exceed a budget. Otherwise leave "None". -->
- None

## Pull Request
- **Url:** {{PR_URL}}
- **State:** {{PR_STATE}}
