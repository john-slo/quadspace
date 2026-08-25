# ARCHITECTURE — quadspace

> Product architecture. Filled during the BLUEPRINT phase. The `implementer` reads this to place code
> correctly and to avoid inventing new layers (see `GUARDRAILS.md`). Changing a decision here requires
> a tracker `Decision/Escalation`.

## Solution layout

```
quadspace.sln
src/
  Quadspace.Core/       # Pure C# library (net10.0): score DTOs, GameConfig, game engine. No UI/JS/IO.
  Quadspace.Client/     # Blazor WebAssembly app: Razor UI + thin JS interop (canvas/audio/input)
  Quadspace.Host/       # ASP.NET Core host: serves the WASM client + score JSON API
tests/
  Quadspace.Tests/      # xUnit tests for the game engine + score persistence logic
```

The classic `.sln` format is used (not `.slnx`) so any pinned SDK builds it. Everything targets
`net10.0`.

## Projects

| Project | Responsibility |
| --- | --- |
| Quadspace.Core | Pure, dependency-free C# library. Immutable score/leaderboard DTOs (records), the `GameConfig` model, and the deterministic game engine (ship, spheres, shots, collisions, bouncing, levels, lives, scoring). No UI, no JS interop, no filesystem — so it is fully unit-testable. Referenced by Client, Host, and Tests. |
| Quadspace.Client | The Blazor WASM front end. Razor pages (home/attract, game, game-over), the per-frame loop that drives `Core` engine `Update` steps, and thin JS interop modules for canvas rendering, audio, and keyboard/gamepad input. Loads `game-config.json`. References Core. |
| Quadspace.Host | ASP.NET Core app that hosts the WASM client's static assets and exposes the minimal score API (`GET /api/scores/top`, `POST /api/scores`). Owns file-based score persistence under a server `scores/` directory. References Core (for DTOs) and Client (to serve it). |
| Quadspace.Tests | xUnit unit tests for the pure Core engine and the server score service. |

## Key patterns (use these; do not introduce new ones without a Decision)

- **Rendering model: Blazor WebAssembly.** The game loop runs client-side. The `Quadspace.Core`
  engine owns all simulation state and is deterministic and side-effect free (no DOM, no JS, no
  time-of-day, RNG injected) so it is unit-testable; it advances via an explicit
  `Update(deltaSeconds, input)` step.
- **Rendering/input/audio are thin JS interop.** A `requestAnimationFrame` loop in JS calls into C#
  (or C# drives via `IJSRuntime`) once per frame; canvas draw calls, Web Audio, and keyboard/gamepad
  reads live in small `.js` modules invoked through `IJSRuntime`/`[JSImport]`. No game rules in JS.
- **Score persistence: file-based, server-side, in the host.** The host writes a per-date daily JSON
  file (`scores/daily/YYYY-MM-DD.json`) and maintains a single `scores/top100.json` (insert-in-order,
  capped at 100). The client reaches it only over the JSON API — WASM cannot touch the filesystem.
- **Central tuning: `game-config.json`.** Every gameplay parameter (lives, speeds, spawn rates,
  invuln time, extra-life cadence, life-sphere chance, projectile speed/limits, arena size, etc.) is
  read from this file at startup; no gameplay magic numbers in code.
- **State management:** simple, explicit game-state objects passed by the engine; no external state
  library. UI reads engine state and calls the score API via a typed `HttpClient`.
- Cross-cutting: `Nullable` enabled, warnings-as-errors, file-scoped namespaces, records for DTOs,
  constructor injection (see `CONVENTIONS.md`).

## Dependencies (allowlist mirror)

Packages permitted without escalation live in `process.config.json > allowedPackages`:

- `Microsoft.AspNetCore.Components.WebAssembly`, `...WebAssembly.DevServer` (client)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` (host)
- `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector` (tests)

Adding any other package requires updating that list **and** a tracker `Decision/Escalation` entry.

## Extension points

- **game-config.json** is the sanctioned tuning surface — extend it with new parameters rather than
  hardcoding constants.
- The JS interop modules (render/audio/input) are the seam for later features (parallax polish, audio,
  gamepad) — add capability there without changing the C# engine's rules.
