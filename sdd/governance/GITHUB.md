# GITHUB — GitHub Operations

> The GitHub Copilot CLI reaches GitHub through its **built-in GitHub tools** (GitHub MCP) and the
> authenticated **`gh` CLI** — no PAT, no `.env`, no provider abstraction. Auth comes from
> `gh auth login` / Copilot. This note lists the handful of operations the process uses and the
> autonomy boundary; the phase details live in [`PROCESS.md`](PROCESS.md).

Repo coordinates come from `process.config.json > github` (`owner`, `repo`, `targetBranch`).

## Operations

| Operation | Phase | How |
| --- | --- | --- |
| **Create issue** | REGISTER | Built-in GitHub tool (or `gh issue create`) from the tracker Requirements + Acceptance Criteria. Store the returned number + URL in the tracker. Idempotent — skip if the tracker already has an issue number. |
| **Create branch + worktree** | WORKTREE *(dedicated only)* | `git worktree add <worktreeRoot>/<id>-<slug> -b feature/<id>-<slug> <targetBranch>`. |
| **Create pull request** | PR | Push the branch, then create the PR (built-in tool or `gh pr create`). The body **must** contain `Closes #<issueNumber>` so GitHub links the issue automatically — there is no separate linking step. |
| **Request / re-request reviewer** | PR merge gate *(autonomousMerge only)* | `gh api --method POST repos/<owner>/<repo>/pulls/<N>/requested_reviewers -f "reviewers[]=<review.reviewer>"`. Only `copilot-pull-request-reviewer[bot]` works via this REST endpoint. **Re-request after every new commit** — a push does not re-trigger the bot. Add `--jq '.number'` to keep the output small. |
| **Read review + CI** | PR merge gate | `gh pr view <N> --json reviews --jq '.reviews[] | {a:.author.login,s:.state}'`, `gh api repos/<owner>/<repo>/pulls/<N>/comments --jq '.[] | {path,line,body}'`, `gh pr checks <N>`, and `gh pr view <N> --json mergeable,mergeStateStatus`. |
| **Merge** | PR merge gate *(autonomousMerge only)* | `gh pr merge <N> --<github.mergeMethod> [--delete-branch]`, only after review is resolved and (if `review.requireCiGreen`) checks are green. Then `git checkout <targetBranch>; git pull --ff-only`. |

## Autonomy boundary

The merge gate is set by `autonomy.autonomousMerge` (see [`PROCESS.md`](PROCESS.md) → *PR*):

- **`true` (default):** the agent merges the PR **itself**, but **only** after the review tool's
  feedback is resolved and required checks are green, via `gh pr merge`. It never *approves* or
  *votes* on a PR, never merges with red CI or unresolved findings, and never merges to bypass an
  escalation.
- **`false`:** the agent **never** merges, approves, votes on, or auto-completes a PR. PRs are created
  only; approval and merge are human actions.

Autonomy means running **many features through the gate** without a human stop *between* them. It
never relaxes scope lock, the change budget, or the dependency allowlist.

## Notes from practice

- **The Copilot bot reviews even when it doesn't stay in `requested_reviewers`.** After you POST the
  request, `requested_reviewers` may read back empty, but the bot still posts a review (author
  `copilot-pull-request-reviewer`, state `COMMENTED`). Poll `gh pr view <N> --json reviews` rather
  than the requested-reviewers list to know it has run.
- Copilot reviews are `COMMENTED` (advisory), not `CHANGES_REQUESTED` — they don't block a merge by
  themselves. The gate to honour is: **address the comments**, then require `mergeStateStatus: CLEAN`
  + green CI before `gh pr merge`.
- Inline review comments do not auto-resolve after you push a fix; that's expected. Confirm the fix in
  a follow-up commit and re-request the review.
