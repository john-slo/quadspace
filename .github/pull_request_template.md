<!--
  Default pull request description for GitHub. Mirrors sdd/templates/pr.template.md so that
  manually opened PRs match the ones the process opens. Fill in the sections below.
  NOTE: The Agentic SDD merge gate is set by autonomy.autonomousMerge — default true (the agent merges
  after the review tool + CI are green) or false (a human merges). The agent never approves/votes on a
  PR or merges with red CI/unresolved review (see sdd/governance/GUARDRAILS.md).
-->
## What & why
<!-- Summary of the change and the problem it solves. -->

Closes #<id>

## Acceptance criteria
<!-- Checklist mirroring the tracker's Acceptance Criteria; tick each satisfied item. -->
- [ ] AC1 - ...

## How it was verified
<!-- Commands come from process.config.json > stack (defaults shown). -->
- Build - <pass/fail>
- Test - <pass/fail> (coverage <n>%)
- Format check - <clean/changes>

## Guardrails
- Change budget: <files / LOC vs limits>
- New packages: <count> - New projects: <count>
- Scope: all changes traceable to tracker tasks - <yes/no>

## Tracker
`specs/<id>-<slug>/tracker.md`

---
> Merge gate: if this PR was created by the Agentic SDD process, it is merged by the agent once the
> review tool + CI are green (`autonomy.autonomousMerge: true`, default), or it **awaits human merge**
> (`autonomousMerge: false`). The agent never approves/votes or merges with red CI/unresolved review.
