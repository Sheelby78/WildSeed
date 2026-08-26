---
bootstrapped_at: 2026-08-26T10:39:44Z
starter_id: dotnet
starter_name: ".NET (ASP.NET Core webapi) + Vite React extension"
project_name: wild-seed
language_family: multi
package_manager: dotnet
cwd_strategy: native-cwd
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: "npm audit --json; dotnet list package --vulnerable --include-transitive"
---

## Hand-off

```yaml
starter_id: dotnet
package_manager: dotnet
project_name: wild-seed
hints:
  language_family: multi
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: custom
  quality_override: false
  self_check_answers:
    typed: true
    from_official_starter: true
    conventions: true
    docs_current: true
    can_judge_agent: true
  has_auth: false
  has_payments: false
  has_realtime: true
  has_ai: false
  has_background_jobs: false
```

Wild Seed is a solo, four-week web MVP whose deterministic, performance-critical simulation is anchored in C# and .NET. The verified ASP.NET Core web API starter provides the backend foundation, while a separate Vite-based React and TypeScript frontend uses PixiJS for efficient world rendering. SignalR transports sampled state and events to the UI but remains an adapter outside the domain and Simulation Engine, preserving the option to replace the transport or execution host later. Development is local-first; the intended deployment is Azure App Service for the backend and Azure Static Web Apps for the frontend. GitHub Actions will run build, tests, and lint initially, with automatic deployment after merges to `main` once Azure environments are enabled.

## Pre-scaffold verification

| Signal | Value | Severity | Notes |
| --- | --- | --- | --- |
| npm package | `create-vite` v9.2.0 published 2026-08-24 | fresh | Resolved for the explicitly requested React frontend extension. |
| Vite GitHub repo | not run | n/a | The registry card points to the documentation site rather than a GitHub repository. |
| .NET starter recency | not run | n/a | The registry card points to Microsoft Learn; no supported recency signal was available. |
| Local toolchain | .NET 10.0.302; Node 26.2.0; npm 11.18.0 | current | Both official generators were available. |

## Scaffold log

The hand-off schema supports one starter, but the user explicitly required a two-directory polyglot scaffold. The run therefore used the registered `.NET` starter as the anchor and the registered `vite-react` card as a frontend extension.

**Resolved backend invocation**: `dotnet new webapi -n WildSeed.Api -o Backend --no-restore`
**Resolved frontend invocation**: `npx --yes create-vite@latest Frontend --template react-ts`
**Strategy**: scaffold directly into the two pre-existing empty target directories
**Exit code**: 0 for both generators
**Files written by CLI**: backend application scaffold and frontend React/TypeScript scaffold
**Pre-existing files preserved**: `context/` and `.agents/`; no target-directory conflicts
**Dependencies installed**: `npm install` in `Frontend/`; `dotnet restore` in `Backend/`
**Build verification**: `npm run build` succeeded; `dotnet build --no-restore` succeeded with one NU1903 warning
**Conflicts (`.scaffold` siblings)**: none
**`.gitignore` handling**: Vite created `Frontend/.gitignore`; the .NET template did not create one
**Temporary scaffold cleanup**: not applicable; generators wrote directly into their empty target directories

An initial `npm create vite` invocation was parsed incorrectly by npm and produced the vanilla TypeScript template. Only those newly generated files were removed, and the official React/TypeScript template was regenerated successfully with `npx`.

## Post-scaffold audit

**Tools**: `npm audit --json`; `dotnet list package --vulnerable --include-transitive`
**Summary**: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW after dependency update
**Direct vs transitive**: no current findings

#### CRITICAL findings

None.

#### HIGH findings

None. The initial scaffold resolved transitive `Microsoft.OpenApi` 2.0.0 and emitted `NU1903` for `GHSA-v5pm-xwqc-g5wc`. Updating the direct `Microsoft.AspNetCore.OpenApi` reference from 10.0.10 to 10.0.11 removed the vulnerable dependency; the follow-up audit and build completed cleanly.

#### MODERATE findings

None.

#### LOW / INFO findings

None.

The npm dependency tree reported 0 vulnerabilities across all severities.

## Hints recorded but not acted on

| Hint | Value |
| --- | --- |
| bootstrapper_confidence | verified |
| quality_override | false |
| path_taken | custom |
| self_check_answers | all five checks true |
| team_size | solo |
| deployment_target | azure-app-service |
| ci_provider | github-actions |
| ci_default_flow | auto-deploy-on-merge |
| has_auth | false |
| has_payments | false |
| has_realtime | true |
| has_ai | false |
| has_background_jobs | false |

The v1 bootstrapper does not install PixiJS or the SignalR JavaScript client, configure Azure resources, create CI workflows, or enforce the transport adapter boundary.

## Next steps

Next: a future skill will set up agent context (CLAUDE.md, AGENTS.md). For now, your project is scaffolded and verified — happy hacking.

Useful manual steps in the meantime:

- Add a repository-level `.gitignore` covering .NET `bin/` and `obj/` output.
- Add PixiJS and the SignalR JavaScript client when implementing the first vertical slice.
- Keep SignalR behind an adapter so the domain and Simulation Engine do not reference transport or ASP.NET Core types.
- `git init` if you have not already initialized repository history.
