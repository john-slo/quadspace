# CONVENTIONS — C# / Blazor Coding Standards

> Conventions for the **default .NET/Blazor stack** (`process.config.json > stack.kind: "dotnet"`).
> If your repo uses a different stack, replace this file with that stack's conventions — the process
> is stack-agnostic; only these rules are .NET-specific.
>
> Most rules are enforced by the target repo's `.editorconfig` + `Directory.Build.props`
> (`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`). The `reviewer` prompt checks the rest.
> Read alongside [`GUARDRAILS.md`](GUARDRAILS.md) — **the simplest correct code wins**.

## Language & compiler

- `Nullable` is **enabled**; nullable warnings are **errors**. No `!` null-forgiveness to silence
  the compiler — fix the nullability instead.
- `ImplicitUsings` enabled. Remove unnecessary usings (IDE0005 is a warning → error).
- Target the repo's pinned `LangVersion` (`latest`). No preview language features.
- `async` methods end with `Async` and return `Task`/`Task<T>`/`ValueTask`. **No `async void`**
  except event handlers.
- Never block on async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`).

## Naming

- PascalCase for types, methods, properties, constants; camelCase for locals/parameters;
  `_camelCase` for private fields; `I`-prefixed interfaces.
- One top-level type per file; file name matches the type name.

## Structure

- **File-scoped namespaces.** `using` directives outside the namespace, `System.*` first.
- Prefer records for immutable data (DTOs, options). Prefer `readonly`/`init` where possible.
- Keep methods small and single-purpose. Guard clauses over deep nesting. Always use braces.
- No `#region`. No commented-out code. Comments explain **why**, not **what**.

## Blazor components

- One component concern per file. Co-locate `Component.razor` with `Component.razor.cs`
  (partial class) when there is non-trivial logic; keep `@code` blocks small.
- Parameters: `[Parameter]` public properties; `[EditorRequired]` for mandatory inputs.
- Prefer `EventCallback`/`EventCallback<T>` for child→parent communication.
- Implement `IDisposable`/`IAsyncDisposable` when subscribing to events or timers; unsubscribe.
- Do not perform blocking or long-running work in `OnInitialized`; use `OnInitializedAsync`.
- Respect the render lifecycle; avoid `StateHasChanged()` spam. Use `@key` in dynamic lists.

## Dependency injection

- Constructor injection only. No service-locator (`IServiceProvider.GetService`) in app code.
- Register services with the narrowest correct lifetime; do not capture scoped services in singletons.
- New abstractions require a real second implementation or a testing need — otherwise inject the
  concrete type (see YAGNI in `GUARDRAILS.md`).

## Error handling & logging

- Throw specific exceptions; never swallow (`catch {}`). Do not use exceptions for control flow.
- Use `ILogger<T>` with structured messages (`_logger.LogInformation("Loaded {Count}", count)`);
  never log secrets or PII.

## Testing (see also `PROCESS.md` DoD)

- xUnit. Test names: `Method_State_ExpectedResult`. Arrange/Act/Assert with blank-line separation.
- One logical assertion focus per test. No network/disk/time dependencies — inject clocks/abstractions.
- New public behavior ships with tests. Do not weaken or delete a failing test to go green.
