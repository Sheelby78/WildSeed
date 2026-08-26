# Repository Guidelines

Wild Seed is a deterministic 2D ecosystem simulation. The backend is ASP.NET Core on .NET 10; the frontend is React 19, TypeScript 6, and Vite 8.

## Critical Architecture Rules

- Dependencies point `WildSeed.Api → WildSeed.Simulation → WildSeed.Domain`; never add a reverse reference.
- Domain owns entities, value objects, genomes, needs, and invariant rules. It must not reference ASP.NET Core, SignalR, rendering, persistence, or wall-clock APIs.
- Simulation owns deterministic ticks, world generation, behavior orchestration, and statistics. The same seed and configuration must produce the same result regardless of rendering; see @context/foundation/prd.md.
- API is the composition root. Keep SignalR in an API adapter and publish sampled state/events 10–20 times per second, not one message per simulation tick. UI interpolation must not feed back into simulation state.
- React owns UI and PixiJS owns world rendering. Never place simulation rules in components, render loops, or transport handlers.
- Do not introduce MediatR, CQRS, repositories, or an Infrastructure project without a concrete external dependency that requires them.

## Project Layout

- @Backend/WildSeed.slnx contains production projects under `Backend/src/`: `WildSeed.Domain/`, `WildSeed.Simulation/`, and `WildSeed.Api/`.
- `Backend.Tests/` mirrors the layers with `WildSeed.Domain.Tests/`, `WildSeed.Simulation.Tests/`, and `WildSeed.Architecture.Tests/`. Dependency rules live at @Backend.Tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs.
- Organize `Frontend/src/` into `app/` for composition, `features/` for user-facing slices, `rendering/` for PixiJS, `transport/` for SignalR and DTO mapping, and `shared/` for feature-independent code. Shared code must not import from features.
- Product decisions live in `context/foundation/`; workflow artifacts live in `context/changes/`.

## Commands and Verification

Run from the repository root:

- `dotnet restore Backend/WildSeed.slnx` — restore all production and test projects.
- `dotnet build Backend/WildSeed.slnx --no-restore` — compile the complete backend solution.
- `dotnet test Backend/WildSeed.slnx --no-build` — run domain, simulation, and architecture tests.
- `dotnet run --project Backend/src/WildSeed.Api/WildSeed.Api.csproj` — start the API locally.
- `npm --prefix Frontend ci` — install the locked frontend dependencies.
- `npm --prefix Frontend run dev` — start Vite locally.
- `npm --prefix Frontend run build` and `npm --prefix Frontend run lint` — required frontend checks.

C# enables nullable references and implicit usings. Name tests after observable behavior; simulation changes require deterministic tests, and dependency changes must keep architecture tests green. TypeScript rejects unused locals, unused parameters, and switch fallthrough via @Frontend/tsconfig.app.json; use PascalCase component files, single-quoted imports, and no semicolons. No frontend test runner or CI exists yet. Git is not initialized, so commit conventions remain undefined.
