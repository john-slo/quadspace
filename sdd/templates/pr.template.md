<!--
  Pull request description created in the PR phase. Mirrors .github/pull_request_template.md so manual
  PRs match. The body MUST contain `Closes #<issueNumber>` so GitHub links the issue.
-->
## What & why
{{SUMMARY}}

Closes #{{ISSUE_NUMBER}}

## Acceptance criteria
{{ACCEPTANCE_CRITERIA_CHECKLIST}}

## How it was verified
- Build — {{BUILD_RESULT}}
- Test — {{TEST_RESULT}} (coverage {{COVERAGE}}%)
- Format check — {{FORMAT_RESULT}}

## Guardrails
- Change budget: {{BUDGET_SUMMARY}}
- New packages: {{NEW_PACKAGES}} · New projects: {{NEW_PROJECTS}}
- Scope: all changes traceable to tracker tasks — {{TRACEABILITY_RESULT}}

## Tracker
`specs/{{ISSUE_NUMBER}}-{{SLUG}}/tracker.md`

---
> Created by the Agentic SDD process. With `autonomy.autonomousMerge: false` this PR **awaits human
> review and merge**; the process does not approve or vote.
