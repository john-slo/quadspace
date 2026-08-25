# ARCHITECTURE — quadspace

> Template. `bootstrap/install.ps1` fills the `{{TOKENS}}`. The `implementer` reads this to place
> code correctly and to avoid inventing new layers (see `GUARDRAILS.md`).

## Solution layout

```
{{SOLUTION_LAYOUT}}
```

## Projects

| Project | Responsibility |
| --- | --- |
| {{PROJECT}} | {{RESPONSIBILITY}} |

## Key patterns (use these; do not introduce new ones without a Decision)

<!-- List the cross-cutting decisions features must respect. Examples for a .NET app: rendering model
     (Blazor Server/WASM/Auto), state management, data-access approach. For a library: immutability,
     the public API surface shape, error-handling convention. Replace with what fits this product. -->
- {{KEY_PATTERN_1}}
- {{KEY_PATTERN_2}}
- Cross-cutting: {{CROSS_CUTTING}}

## Dependencies (allowlist mirror)

Packages permitted without escalation live in `process.config.json > allowedPackages`. Adding a
package requires updating that list **and** a tracker `Decision/Escalation` entry.

## Extension points

- {{EXTENSION_POINTS}}
