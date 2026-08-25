# PRODUCT — quadspace

> Product blueprint. Filled during the BLUEPRINT phase. The `requirements-analyst` and `implementer`
> prompts read this to ground features in the product. Keep it current.

## What this is

quadspace is a browser-based, single-player neon 80s arcade shooter with a futuristic twist. The
player pilots a ship on a flat 2D plane and shoots metallic spheres that spawn from the edges, bounce
off the walls at 90° angles, and must be destroyed before they collide with the ship. The experience
emulates a retro arcade cabinet: a parallax "space depth-field" background, a high-score attract
screen, level-by-level progression, and a name-entry flow when the run ends.

It is delivered as a Blazor WebAssembly client served by an ASP.NET Core host. All gameplay simulation
(movement, collision, bouncing, scoring, levels, lives) runs client-side in C#. The host exposes a
small JSON score API that persists high scores as files on the server.

## Primary users & jobs-to-be-done

- **Arcade player** — wants to jump straight into a fast, responsive game, chase a high score across
  escalating levels, and see their name on the leaderboard. No sign-up, no friction.
- **Game tuner (the maintainer)** — wants to rebalance difficulty by editing a single configuration
  file, without touching code.

## Domain glossary

| Term | Meaning |
| --- | --- |
| Ship | The player-controlled craft; moves in x/y on the plane, fires shots in 4 directions. |
| Sphere | A metallic ball, ~ship-sized, spawned from a random edge with constant velocity; bounces off walls at 90°; destroyed by a shot. Awards 8 points. |
| Life-sphere | A rare, specially-marked sphere that grants +1 life when destroyed. |
| Shot / Projectile | A bullet fired by the ship up/down/left/right; destroys a sphere on contact. |
| Level | A round; level N requires destroying 8×N spheres; sphere spawn rate is N per second. |
| Life | A hit allowance; colliding with a sphere costs one; the run ends at 0. |
| Plane / Arena | The bounded 2D play area the ship and spheres occupy. |
| Score | Points earned (8 per sphere). Persisted with a player name at game over. |
| Top-100 | The single leaderboard file of the 100 highest scores; the home page shows the top 10. |
| Daily file | A per-date file recording all scores submitted that day. |
| game-config.json | The central tunable file holding every gameplay parameter. |

## Non-goals / out of scope

- No authentication, accounts, or personal data beyond a self-entered name (≤50 chars).
- No multiplayer, networking between players, or server-side game simulation.
- No cloud deployment/hosting in the initial build (local run + manual smoke test).
- No licensed/copyrighted music or art assets; audio is procedurally generated.
- No mobile/touch controls in the initial build (keyboard first; gamepad follows).

## Quality bar

- Stack: .NET 10 / Blazor WebAssembly + ASP.NET Core host (`process.config.json > stack`).
- Supported targets: current desktop Chromium/Firefox browsers via the ASP.NET Core host.
- Gameplay simulation and score logic live in C# and are unit-tested with xUnit.
- Coverage threshold: see `process.config.json > quality.coverageThreshold` (70%). Thin JS interop
  and rendering glue are exempt where no reasonable unit test applies.
- Build is warning-free (`TreatWarningsAsErrors`); format check clean; CI green before merge.
