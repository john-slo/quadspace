# SKILLS — Reusable Operations

> Skills are the small, mostly-deterministic operations the agent performs during the cycle. With the
> Copilot CLI driving directly, they are **direct commands**, not wrapped scripts — the agent runs
> `git`, `gh`, or the configured stack commands and reasons about the result. The one exception is the
> `.NET`/Windows build wrapper, which handles file-lock and line-ending preflights.

| Skill | Purpose | Phase | How |
| --- | --- | --- | --- |
| **create-issue** | Create the GitHub issue from clarified requirements; store the number in the tracker. | REGISTER | Built-in GitHub tool / `gh issue create` (see [`GITHUB.md`](GITHUB.md)). Idempotent. |
| **create-worktree** | Create `feature/<id>-<slug>` branch + git worktree. | WORKTREE *(dedicated)* | `git worktree add …` (see [`TOOLS.md`](TOOLS.md)). |
| **update-tracker** | Read/patch a tracker section, phase, or task status. | every phase | Edit `specs/<id>-<slug>/tracker.md` directly; keep `## Section` headings and `- **Key:** value` metadata shapes intact. |
| **build-and-test** | Run the configured build/test/format and return results. | IMPLEMENT / VERIFY | Run `stack.*` commands, or [`../scripts/lib/build-and-test.ps1`](../scripts/lib/build-and-test.ps1) for the Windows preflights + structured JSON. |
| **create-pr** | Push, then create the PR with `Closes #<issueNumber>` in the body. | PR | Built-in GitHub tool / `gh pr create` (see [`GITHUB.md`](GITHUB.md)). |

## Rules

- Skills never make creative decisions — they execute a well-defined operation and return output for
  the agent to reason about.
- No skill may merge, approve, complete, or vote on a PR (see [`GUARDRAILS.md`](GUARDRAILS.md)); the
  merge gate is the only place a merge can happen, and only when `autonomy.autonomousMerge: true`.
- Skills are **idempotent**: re-running after a resume detects existing artifacts (via tracker
  metadata) and no-ops rather than duplicating.
