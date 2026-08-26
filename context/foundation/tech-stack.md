---
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
---

## Why this stack

Wild Seed is a solo, four-week web MVP whose deterministic, performance-critical simulation is anchored in C# and .NET. The verified ASP.NET Core web API starter provides the backend foundation, while a separate Vite-based React and TypeScript frontend uses PixiJS for efficient world rendering. SignalR transports sampled state and events to the UI but remains an adapter outside the domain and Simulation Engine, preserving the option to replace the transport or execution host later. Development is local-first; the intended deployment is Azure App Service for the backend and Azure Static Web Apps for the frontend. GitHub Actions will run build, tests, and lint initially, with automatic deployment after merges to `main` once Azure environments are enabled.
