# PRODUCT — quadspace

> Template. `bootstrap/install.ps1` replaces the `{{TOKENS}}` for each repo. Keep this current;
> the `requirements-analyst` and `implementer` prompts read it to ground features in the product.

## What this is

{{PRODUCT_DESCRIPTION}}

## Primary users & jobs-to-be-done

- {{PRIMARY_USERS}}

## Domain glossary

| Term | Meaning |
| --- | --- |
| {{TERM}} | {{DEFINITION}} |

## Non-goals / out of scope

- {{NON_GOALS}}

## Quality bar

- Stack: {{PRIMARY_STACK}} (default .NET / Blazor; set in `process.config.json > stack`)
- Supported targets: {{SUPPORTED_TARGETS}}
- Coverage threshold: see `process.config.json > quality.coverageThreshold`
