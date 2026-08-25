# TOOLS — Commands & Invocation Patterns

> The concrete commands the agent uses. Stack commands come from `process.config.json > stack` and
> **default to .NET/Blazor**; override them for other stacks. GitHub operations are in
> [`GITHUB.md`](GITHUB.md).

## Build / test / format (stack commands)

VERIFY runs the configured commands. Defaults (`stack.kind: "dotnet"`):

| Purpose | `stack` key | Default |
| --- | --- | --- |
| Build (strict) | `buildCommand` | `dotnet build {solution} -c Release` |
| Test | `testCommand` | `dotnet test {solution} -c Release` |
| Format (verify) | `formatCommand` | `dotnet format {solution} --verify-no-changes` |

`{solution}` is substituted from `stack.solution` (empty = whole repo / auto-discover). Override the
commands for other stacks, e.g. `"buildCommand": "npm run build"`, `"testCommand": "pytest"`.

The build must be **warning-free** (on .NET, warnings are errors — never pass `-warnaserror-` or
disable analyzers). The convenience wrapper
[`../scripts/lib/build-and-test.ps1`](../scripts/lib/build-and-test.ps1) reads these commands, runs
them, and returns structured JSON; it also performs the two Windows preflights below. You can also run
the commands directly.

### Preflight: release locked build outputs (Windows)

App hosts or an open IDE can hold `*.dll` outputs open, so a .NET build fails with `MSB3021`/`MSB3027`
("*being used by another process*"). Stop the running app hosts **by PID** (never name-based
`taskkill /IM`); configure their process names in `process.config.json > build.lockingProcesses`:

```powershell
Get-Process -Name "<AppHost>" -ErrorAction SilentlyContinue |
  ForEach-Object { Stop-Process -Id $_.Id -Force }
```

If a lock persists, an IDE is likely holding it — surface that rather than retry.

### Preflight: normalise to LF before `dotnet format --verify-no-changes`

The repo enforces **LF** (`.gitattributes`, `.editorconfig`), but agent-created files are often CRLF,
which fails the format check on brand-new files. Normalise changed + untracked source files to LF
before formatting (the `build-and-test` wrapper does this automatically).

## Git & worktrees

One feature = one branch. If the feature is `dedicated`, it also gets one worktree:

```
git worktree add <worktreeRoot>/<id>-<slug> -b feature/<id>-<slug> <targetBranch>
git worktree list
git worktree remove <path>
```

Never commit to `github.targetBranch` directly; never force-push a shared branch.

## Change-budget measurement

```
git diff --stat <targetBranch>...HEAD      # files changed + LOC delta
git ls-files --others --exclude-standard    # untracked (new) files
```

Compare against `process.config.json > changeBudget` (see [`GUARDRAILS.md`](GUARDRAILS.md)).

## GitHub

All GitHub access uses the Copilot CLI's built-in GitHub tools + the `gh` CLI — see
[`GITHUB.md`](GITHUB.md).
