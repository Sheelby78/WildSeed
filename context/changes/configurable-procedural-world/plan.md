# Configurable Procedural World Implementation Plan

## Overview

Implement S-01: the first visual vertical slice of Wild Seed. A visitor can configure world parameters through a preset-driven UI with an advanced toggle, generate a deterministic procedural terrain with vegetation and initial organism placement, and observe the result rendered as a color-coded tile grid in PixiJS with pan/zoom and regeneration — all before any simulation tick loop exists.

## Current State Analysis

Domain contains only `DomainAssemblyMarker`. Simulation contains determinism infrastructure (`CanonicalStateWriter`, `StateFingerprint`, `SimulationContract`) but no production engine, world model, or RNG. API is the default `dotnet new webapi` scaffold with a weather forecast endpoint. Frontend is the default Vite React-TS scaffold with no PixiJS, no feature directories, and no state management.

F-01 established: 100ms logical tick, SHA-256 canonical fingerprinting with `WriteOrdered`, golden checkpoints at ticks 0/10/100/1000 (test-only synthetic probe), and a 5000-agent benchmark with SoA layout. The synthetic benchmark `V1` is immutable and must not be modified.

### Desired End State

After this plan:

- Domain exposes `WorldConfiguration`, `TerrainType`, `Tile`, `WorldMap`, `Organism` (stub), `Genome` (stub), and `Species` types with full invariant protection.
- Simulation exposes `WorldGenerator` that uses FastNoiseLite (single-file, MIT) to produce deterministic terrain from a seed, assigns per-tile vegetation density, and places initial organisms randomly on land tiles.
- Simulation can compute a `StateFingerprint` for the generated world using the existing `CanonicalStateWriter`.
- API exposes `POST /api/world/generate` accepting a configuration DTO and returning a world snapshot as JSON.
- Frontend renders the world as a color-coded `ParticleContainer` tile grid in PixiJS 8, with custom camera pan/zoom, a preset selector with advanced toggle for all FR-001 parameters, and regeneration without page reload.
- Same seed + configuration always produces the same world (verified by fingerprint-based determinism tests).
- Architecture tests remain green.

### Key Discoveries

- [`CanonicalStateWriter`](file:///C:/Users/sheel/Documents/.NET/WildSeed/Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs) provides fluent `WriteHeader`, typed write methods, and `WriteOrdered<T,TKey>` for sorted collection serialization. S-01 must use this to hash terrain + vegetation + organisms.
- [`StateFingerprint.Compute`](file:///C:/Users/sheel/Documents/.NET/WildSeed/Backend/src/WildSeed.Simulation/Determinism/StateFingerprint.cs#L29-L35) accepts a `CanonicalStateWriter` or `ReadOnlySpan<byte>` and defaults to `SimulationContract.Version` (1).
- [`DependencyRulesTests`](file:///C:/Users/sheel/Documents/.NET/WildSeed/Backend/tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs) enforces: Domain → nothing; Simulation → Domain only (no ASP.NET, no BenchmarkDotNet); Api → Simulation.
- PixiJS 8's `ParticleContainer` with `Particle` objects using `Texture.WHITE` and per-particle tint is the performant way to render 16K–65K colored rectangles.
- `pixi-viewport` has no official v8 support. A custom camera using `Container({ isRenderGroup: true })` with pointer drag and wheel zoom is the recommended pattern.
- FastNoiseLite is a single C# file (MIT), deterministic from seed, supports OpenSimplex2 with fractal modes — ideal for terrain generation without adding NuGet dependencies.
- Frontend has no path aliases configured; `@/*` → `src/*` should be added to match the AGENTS.md convention.

## What We're NOT Doing

- No simulation tick loop, movement, needs, or behavior — S-02 owns all runtime behavior.
- No SignalR — deferred to S-02 when live streaming begins. S-01 uses a REST endpoint.
- No frontend tests — Vitest setup deferred per tech-stack.md.
- No organism sprites or visual differentiation beyond colored dots — rendering polish comes later.
- No mutation logic execution — mutation probability and strength are configuration inputs stored for S-04.
- No benchmark for world generation — the existing synthetic benchmark V1 is immutable.
- No CI workflow.

## Implementation Approach

Build bottom-up: Domain types first, then Simulation generation, then API endpoint, then Frontend rendering and UI. Each phase is independently testable. The approach keeps Domain pure (no framework references), puts all generation logic in Simulation, and uses a simple REST endpoint to bridge backend and frontend.

## Critical Implementation Details

### Timing & lifecycle

FastNoiseLite must be instantiated with the world seed inside `WorldGenerator` — not stored as a long-lived singleton — to ensure deterministic output per generation call. The noise instance is scoped to one `Generate` invocation.

### Performance constraints

The world generation endpoint must respond within a few seconds for 256×256 grids (~65K tiles). FastNoiseLite evaluates in O(1) per coordinate, so 65K noise lookups are trivial. The JSON response payload for 256×256 will be ~500KB–1MB; this is acceptable for a one-shot REST call.

---

## Phase 1: Domain World Model

### Overview

Introduce the core domain types that represent a world configuration, terrain, tiles, and the world map. These are pure value objects and entities with no framework dependencies.

### Changes Required

#### 1. TerrainType enum

**File**: `Backend/src/WildSeed.Domain/Terrain/TerrainType.cs`

**Intent**: Define the terrain categories produced by procedural generation. Each type maps to an elevation threshold range from noise output.

**Contract**: `public enum TerrainType : byte { DeepWater, ShallowWater, Sand, Grass, Forest }`

#### 2. Tile value object

**File**: `Backend/src/WildSeed.Domain/Terrain/Tile.cs`

**Intent**: Represent a single cell in the world grid. Immutable snapshot of terrain type, position, and vegetation density.

**Contract**: `public readonly record struct Tile(int X, int Y, TerrainType Terrain, float VegetationDensity)`. Vegetation density clamped to [0.0, 1.0]. Only land tiles (Sand, Grass, Forest) may have non-zero vegetation; water tiles enforce 0.0.

#### 3. Species enum

**File**: `Backend/src/WildSeed.Domain/Organisms/Species.cs`

**Intent**: Distinguish herbivores from carnivores for initial population placement and later behavior differentiation.

**Contract**: `public enum Species : byte { Herbivore, Carnivore }`

#### 4. Genome value object (stub)

**File**: `Backend/src/WildSeed.Domain/Organisms/Genome.cs`

**Intent**: Placeholder for the inherited genome that S-04 will flesh out. For S-01, a genome is created with a speed trait only, enough to give organisms a distinguishing characteristic.

**Contract**: `public readonly record struct Genome(float Speed)`. Speed clamped to [0.1, 10.0].

#### 5. Organism entity

**File**: `Backend/src/WildSeed.Domain/Organisms/Organism.cs`

**Intent**: Represent a single organism in the world. For S-01 this is a positioned, species-tagged entity with a genome stub. No behavior, needs, or lifecycle — those come in S-02.

**Contract**: `public sealed class Organism` with `Guid Id`, `Species Species`, `Genome Genome`, `float X`, `float Y`, `bool IsAlive` (default true). Constructor validates position is non-negative.

#### 6. WorldConfiguration value object

**File**: `Backend/src/WildSeed.Domain/World/WorldConfiguration.cs`

**Intent**: Capture all user-configurable parameters for world generation per FR-001.

**Contract**: `public sealed record WorldConfiguration` with properties: `int Seed`, `int Width` (tiles, 64–512), `int Height` (tiles, 64–512), `int InitialHerbivores` (0–5000), `int InitialCarnivores` (0–5000), `float VegetationDensity` (0.0–1.0), `float WaterLevel` (0.0–1.0), `float MutationProbability` (0.0–1.0), `float MutationStrength` (0.0–1.0). Constructor validates all ranges.

#### 7. WorldMap entity

**File**: `Backend/src/WildSeed.Domain/World/WorldMap.cs`

**Intent**: The root aggregate for a generated world — holds the tile grid, organism list, and originating configuration.

**Contract**: `public sealed class WorldMap` with `WorldConfiguration Configuration`, `int Width`, `int Height`, `Tile[,] Tiles` (2D array), `IReadOnlyList<Organism> Organisms`. Constructor validates dimensions match configuration. Provides `Tile GetTile(int x, int y)` with bounds checking.

### Success Criteria

#### Automated Verification

- Solution builds cleanly: `dotnet build Backend/WildSeed.slnx --no-restore`
- Architecture tests pass: `dotnet test Backend/tests/WildSeed.Architecture.Tests/ --no-build`
- All existing tests pass: `dotnet test Backend/WildSeed.slnx --no-build`

#### Manual Verification

- Domain types compile without referencing any external framework

---

## Phase 2: Simulation Procedural Generation

### Overview

Implement the world generator that turns a `WorldConfiguration` into a populated `WorldMap` using deterministic noise-based terrain, vegetation density assignment, and random organism placement.

### Changes Required

#### 1. FastNoiseLite source file

**File**: `Backend/src/WildSeed.Simulation/Noise/FastNoiseLite.cs`

**Intent**: Add the single-file FastNoiseLite library (MIT) as a source dependency for deterministic 2D noise generation. No NuGet package needed.

**Contract**: Download from https://github.com/Auburn/FastNoiseLite, place in `Noise/` directory. Used internally by `WorldGenerator` only.

#### 2. WorldGenerator

**File**: `Backend/src/WildSeed.Simulation/WorldGeneration/WorldGenerator.cs`

**Intent**: Orchestrate procedural world generation. Creates a noise instance from the seed, evaluates elevation at each tile coordinate, maps elevation to terrain type using thresholds influenced by `WaterLevel`, assigns vegetation density to land tiles influenced by `VegetationDensity`, and spawns initial organisms on random land tiles.

**Contract**: `public sealed class WorldGenerator` with method `WorldMap Generate(WorldConfiguration config)`. The method is pure — same config always produces the same `WorldMap`. Internally:
- Uses `FastNoiseLite` with `OpenSimplex2` noise type and configurable frequency.
- Elevation thresholds: `WaterLevel` parameter shifts the boundary between water and land terrain types.
- Vegetation density on land tiles: base density from a second noise layer, scaled by `VegetationDensity` parameter. Forest tiles get higher base density than Grass, Sand gets lower.
- Organism placement: uses a deterministic `SimulationRandom` (to be created) seeded from the world seed to pick random land tile positions for `InitialHerbivores` herbivores and `InitialCarnivores` carnivores.

#### 3. SimulationRandom

**File**: `Backend/src/WildSeed.Simulation/Random/SimulationRandom.cs`

**Intent**: Production deterministic RNG for all simulation randomness. Wraps `System.Random` with a known seed for reproducibility.

**Contract**: `public sealed class SimulationRandom` with constructor `(int seed)`. Methods: `int NextInt(int minInclusive, int maxExclusive)`, `float NextFloat()` (0.0–1.0), `float NextFloat(float min, float max)`.

#### 4. World fingerprinting

**File**: `Backend/src/WildSeed.Simulation/WorldGeneration/WorldFingerprint.cs`

**Intent**: Compute a `StateFingerprint` for a generated `WorldMap` using the existing canonical encoding infrastructure. This enables determinism verification without comparing entire world states.

**Contract**: `public static class WorldFingerprint` with method `StateFingerprint Compute(WorldMap world)`. Encoding order: header (seed, tick=0, contract version) → grid dimensions → tiles ordered by (Y, X) with terrain type byte and vegetation float → organisms ordered by Id with species, genome, position.

### Success Criteria

#### Automated Verification

- Solution builds cleanly: `dotnet build Backend/WildSeed.slnx --no-restore`
- All existing tests pass: `dotnet test Backend/WildSeed.slnx --no-build`

#### Manual Verification

- `WorldGenerator.Generate` returns a populated `WorldMap` with terrain variety when called from a test harness

---

## Phase 3: Backend Tests

### Overview

Add comprehensive tests for domain invariants, generation determinism, terrain distribution, and the world fingerprint. These tests prove correctness before the API or frontend exist.

### Changes Required

#### 1. Domain tests

**File**: `Backend/tests/WildSeed.Domain.Tests/WorldConfigurationTests.cs`

**Intent**: Verify `WorldConfiguration` rejects invalid parameter ranges and accepts valid ones.

**Contract**: Tests for: seed accepts any int, width/height reject < 64 and > 512, population counts reject negatives and > 5000, density/level/probability/strength reject values outside [0.0, 1.0].

#### 2. Domain tile tests

**File**: `Backend/tests/WildSeed.Domain.Tests/TileTests.cs`

**Intent**: Verify `Tile` invariants — vegetation clamping, water tiles enforce zero vegetation.

**Contract**: Tests for: vegetation clamped to [0, 1], water terrain types force vegetation to 0.

#### 3. Domain organism tests

**File**: `Backend/tests/WildSeed.Domain.Tests/OrganismTests.cs`

**Intent**: Verify `Organism` construction invariants.

**Contract**: Tests for: default IsAlive true, negative position rejected, genome speed clamping.

#### 4. Generation determinism tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/WorldGeneration/WorldGeneratorDeterminismTests.cs`

**Intent**: Prove same configuration produces the same world fingerprint across independent runs. This is the core determinism contract for S-01.

**Contract**: Tests for: (1) two independent `Generate` calls with the same config produce the same `WorldFingerprint`, (2) different seeds produce different fingerprints, (3) changing any single configuration parameter produces a different fingerprint.

#### 5. Generation distribution tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/WorldGeneration/WorldGeneratorDistributionTests.cs`

**Intent**: Verify that terrain distribution responds to configuration parameters and organisms are placed on valid tiles.

**Contract**: Tests for: (1) higher `WaterLevel` produces more water tiles, (2) vegetation density parameter influences average tile vegetation, (3) all organisms are placed on land tiles (not water), (4) organism count matches requested populations, (5) forest tiles have higher vegetation than sand tiles.

#### 6. SimulationRandom determinism tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/Random/SimulationRandomTests.cs`

**Intent**: Verify the production RNG is deterministic from seed.

**Contract**: Tests for: same seed produces same sequence, different seeds produce different sequences, range methods stay within bounds.

### Success Criteria

#### Automated Verification

- All tests pass: `dotnet test Backend/WildSeed.slnx --no-build`
- Architecture tests still green: `dotnet test Backend/tests/WildSeed.Architecture.Tests/ --no-build`

#### Manual Verification

- Test names clearly describe observable behavior

---

## Phase 4: API World Generation Endpoint

### Overview

Expose a REST endpoint that accepts world configuration, runs the generator, and returns the world state as JSON. This bridges backend simulation and frontend rendering.

### Changes Required

#### 1. Configuration DTO

**File**: `Backend/src/WildSeed.Api/Contracts/GenerateWorldRequest.cs`

**Intent**: Define the API request shape for world generation. Maps to `WorldConfiguration` with validation.

**Contract**: `public sealed record GenerateWorldRequest` with nullable properties matching `WorldConfiguration` fields. Includes a `ToDomain()` method that applies defaults for missing fields and returns a `WorldConfiguration`.

#### 2. World snapshot DTO

**File**: `Backend/src/WildSeed.Api/Contracts/WorldSnapshotResponse.cs`

**Intent**: Define the API response shape — a JSON-serializable world snapshot.

**Contract**: `public sealed record WorldSnapshotResponse` with `int Width`, `int Height`, `TileDto[][] Tiles` (jagged array for JSON), `OrganismDto[] Organisms`, `string Fingerprint`. Nested records: `TileDto(int X, int Y, string Terrain, float VegetationDensity)`, `OrganismDto(string Id, string Species, float X, float Y, float Speed)`. Includes a static `FromDomain(WorldMap, StateFingerprint)` factory.

#### 3. World generation endpoint

**File**: `Backend/src/WildSeed.Api/Endpoints/WorldEndpoints.cs`

**Intent**: Wire up the `POST /api/world/generate` minimal API endpoint. Accepts `GenerateWorldRequest`, validates, generates world, computes fingerprint, returns `WorldSnapshotResponse`.

**Contract**: Static class with `MapWorldEndpoints(this WebApplication app)` extension method. Returns `Results<Ok<WorldSnapshotResponse>, ValidationProblem>`.

#### 4. Wire up in Program.cs

**File**: `Backend/src/WildSeed.Api/Program.cs`

**Intent**: Register the world endpoint and add CORS for frontend dev server. Remove the weather forecast scaffold code.

**Contract**: Call `app.MapWorldEndpoints()`. Add CORS policy allowing `http://localhost:5173` (Vite dev server). Register `WorldGenerator` as a singleton service.

### Success Criteria

#### Automated Verification

- Solution builds: `dotnet build Backend/WildSeed.slnx --no-restore`
- All tests pass: `dotnet test Backend/WildSeed.slnx --no-build`
- API starts without errors: `dotnet run --project Backend/src/WildSeed.Api/WildSeed.Api.csproj`

#### Manual Verification

- `POST /api/world/generate` with `{ "seed": 42, "width": 128, "height": 128 }` returns a JSON world snapshot with terrain variety and organisms
- Same request twice returns the same fingerprint

---

## Phase 5: Frontend Project Setup and PixiJS Rendering

### Overview

Install PixiJS, establish the feature-driven directory structure, and implement the world renderer — a color-coded tile grid with a custom pan/zoom camera.

### Changes Required

#### 1. Install dependencies

**Intent**: Add PixiJS 8 to the frontend project.

**Contract**: `npm --prefix Frontend install pixi.js`

#### 2. Directory structure

**Intent**: Create the feature-driven layout per AGENTS.md: `app/`, `features/world/`, `rendering/`, `transport/`, `shared/`.

**Contract**: Create directories:
- `Frontend/src/app/` — app shell and composition
- `Frontend/src/features/world/` — world configuration feature
- `Frontend/src/rendering/` — PixiJS rendering code
- `Frontend/src/transport/` — API client code
- `Frontend/src/shared/` — shared types and utilities

#### 3. Path alias configuration

**File**: `Frontend/tsconfig.app.json`

**Intent**: Add `@/*` → `src/*` path alias for clean imports per AGENTS.md convention.

**Contract**: Add `"baseUrl": "."` and `"paths": { "@/*": ["src/*"] }` to `compilerOptions`.

**File**: `Frontend/vite.config.ts`

**Intent**: Mirror the path alias in Vite's resolver.

**Contract**: Add `resolve.alias` mapping `@` to `path.resolve(__dirname, 'src')`.

#### 4. API client

**File**: `Frontend/src/transport/WorldApi.ts`

**Intent**: HTTP client for the world generation endpoint.

**Contract**: Export `generateWorld(config: WorldConfig): Promise<WorldSnapshot>` that POSTs to `/api/world/generate`. Types: `WorldConfig` (matches `GenerateWorldRequest`), `WorldSnapshot` (matches `WorldSnapshotResponse`), `TileData`, `OrganismData`.

#### 5. Vite dev proxy

**File**: `Frontend/vite.config.ts`

**Intent**: Proxy `/api` requests to the backend dev server to avoid CORS in development.

**Contract**: Add `server.proxy` mapping `/api` to `http://localhost:5184`.

#### 6. World renderer

**File**: `Frontend/src/rendering/WorldRenderer.ts`

**Intent**: PixiJS renderer that draws the world tile grid as colored particles and organisms as smaller colored circles. Manages the PixiJS `Application` lifecycle.

**Contract**: Export class `WorldRenderer` with methods:
- `init(canvas: HTMLCanvasElement): Promise<void>` — creates PixiJS Application, attaches to canvas
- `renderWorld(snapshot: WorldSnapshot): void` — creates/updates `ParticleContainer` with tiles as scaled `Particle` objects using `Texture.WHITE` and per-particle tint based on terrain type. Overlays organism positions as smaller particles.
- `destroy(): void` — cleanup

Terrain colors: DeepWater → `0x1a3c6e`, ShallowWater → `0x3a7ecf`, Sand → `0xd4b463`, Grass → `0x4a8f3f` (modulated by vegetation density), Forest → `0x2d5a1e` (modulated by vegetation density).

#### 7. Camera controller

**File**: `Frontend/src/rendering/CameraController.ts`

**Intent**: Custom pan/zoom controller for the world container using PixiJS 8's native `isRenderGroup` optimization.

**Contract**: Export class `CameraController` with:
- `attach(worldContainer: Container, canvas: HTMLCanvasElement): void` — binds pointer drag for pan and wheel for zoom
- `detach(): void` — removes event listeners
- Zoom range: 0.1x to 5x. Pan bounded to world dimensions.

#### 8. App shell

**File**: `Frontend/src/app/App.tsx`

**Intent**: Replace the Vite scaffold with the Wild Seed app shell — a full-viewport canvas with a configuration sidebar.

**Contract**: Root component that renders a layout with a sidebar (for config) and a main area (for the PixiJS canvas). Uses a `ref` for the canvas element, initializes `WorldRenderer` on mount, cleans up on unmount.

#### 9. Clean up scaffold files

**Intent**: Remove the default Vite scaffold files that are no longer needed.

**Contract**: Delete `Frontend/src/App.tsx`, `Frontend/src/App.css`, `Frontend/src/index.css`, `Frontend/src/assets/react.svg`, `Frontend/src/assets/vite.svg`, `Frontend/src/assets/hero.png`. Update `Frontend/src/main.tsx` to import from `app/App`.

### Success Criteria

#### Automated Verification

- Frontend builds: `npm --prefix Frontend run build`
- Frontend lints: `npm --prefix Frontend run lint`

#### Manual Verification

- `npm --prefix Frontend run dev` shows a full-viewport canvas
- A hardcoded test world renders as a colored tile grid
- Mouse drag pans the view, scroll wheel zooms in/out

---

## Phase 6: Frontend Configuration UI

### Overview

Build the preset selector with advanced toggle that exposes all FR-001 parameters, wires it to the API, and enables regeneration without page reload.

### Changes Required

#### 1. World presets

**File**: `Frontend/src/features/world/WorldPresets.ts`

**Intent**: Define named preset configurations that give users a friendly starting point.

**Contract**: Export `WorldPreset` type and `PRESETS` array with at least: `Island` (high water, small map), `Continental` (medium water, large map), `Arid` (low water/vegetation, medium map), `Lush` (high vegetation, medium map). Each preset provides all `WorldConfig` fields.

#### 2. Configuration panel component

**File**: `Frontend/src/features/world/ConfigPanel.tsx`

**Intent**: Sidebar panel with preset selector dropdown and an expandable "Advanced" section exposing individual parameter controls.

**Contract**: React component accepting `onGenerate: (config: WorldConfig) => void`. Renders:
- Preset dropdown (selecting a preset fills all fields)
- "Advanced Settings" collapsible section with: seed (text input), width/height (dropdown: Small 128, Medium 192, Large 256), vegetation density (slider 0–1), water level (slider 0–1), initial herbivores (number input), initial carnivores (number input), mutation probability (slider 0–1), mutation strength (slider 0–1)
- "Generate World" button

#### 3. Global styles

**File**: `Frontend/src/app/globals.css`

**Intent**: Minimal global styles for the app shell — dark theme, full-viewport layout, sidebar styling.

**Contract**: CSS that sets up: dark background, sidebar fixed width on the left, main canvas area filling remaining space. Cohesive style matching Wild Seed's nature theme (dark greens, earth tones). No default browser dialogs or unstyled elements per PRD guardrail.

#### 4. Wire config to rendering

**File**: `Frontend/src/app/App.tsx`

**Intent**: Connect the configuration panel to the API client and world renderer. When the user clicks "Generate", POST config to API, receive snapshot, render it.

**Contract**: App component manages state: `loading`, `error`, `currentSnapshot`. On generate: calls `generateWorld`, on success calls `renderer.renderWorld`. Shows loading state on the canvas during generation.

### Success Criteria

#### Automated Verification

- Frontend builds: `npm --prefix Frontend run build`
- Frontend lints: `npm --prefix Frontend run lint`

#### Manual Verification

- Selecting a preset fills all parameter fields
- Clicking "Generate World" sends a request and renders the result
- Changing the seed and regenerating produces a different world
- Using the same seed twice produces the same visual result
- Advanced settings expand/collapse correctly
- All sliders and inputs are functional

---

## Phase 7: API Integration Test

### Overview

Add an integration test for the world generation endpoint to verify the API contract works end-to-end.

### Changes Required

#### 1. API integration test

**File**: `Backend/tests/WildSeed.Api.Tests/WorldEndpointTests.cs`

**Intent**: Verify the `POST /api/world/generate` endpoint accepts configuration, returns a valid world snapshot, and produces deterministic results.

**Contract**: Uses `WebApplicationFactory<Program>` for in-process testing. Tests: (1) valid config returns 200 with expected JSON shape, (2) same config twice returns same fingerprint, (3) invalid config returns 400 with validation errors, (4) default values applied when optional fields omitted.

#### 2. Test project setup

**File**: `Backend/tests/WildSeed.Api.Tests/WildSeed.Api.Tests.csproj`

**Intent**: Create the API test project with proper dependencies.

**Contract**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + FluentAssertions. References `WildSeed.Api`. Register in the solution file.

### Success Criteria

#### Automated Verification

- All tests pass: `dotnet test Backend/WildSeed.slnx --no-build`
- Solution builds: `dotnet build Backend/WildSeed.slnx --no-restore`

#### Manual Verification

- API test project is registered in the solution

---

## Phase 8: Integration and Final Verification

### Overview

Full end-to-end verification. Ensure the complete backend builds, all tests pass, architecture rules hold, frontend builds and lints, and the full flow works manually.

### Changes Required

#### 1. Solution file update

**File**: `Backend/WildSeed.slnx`

**Intent**: Ensure the API test project is included in the solution.

**Contract**: Add `WildSeed.Api.Tests` project reference if not already present from Phase 7.

#### 2. Remove weather forecast artifacts

**File**: `Backend/src/WildSeed.Api/WildSeed.Api.http`

**Intent**: Remove the scaffold HTTP test file that references the deleted weather forecast endpoint.

**Contract**: Delete or replace with a world generation request example.

### Success Criteria

#### Automated Verification

- Full backend build: `dotnet build Backend/WildSeed.slnx --no-restore`
- All backend tests pass: `dotnet test Backend/WildSeed.slnx --no-build`
- Architecture tests green: `dotnet test Backend/tests/WildSeed.Architecture.Tests/ --no-build`
- Frontend builds: `npm --prefix Frontend run build`
- Frontend lints: `npm --prefix Frontend run lint`

#### Manual Verification

- Start API and frontend dev servers simultaneously
- Select "Island" preset, click Generate — see a world with islands and water
- Select "Continental" preset, regenerate — see a different terrain distribution
- Enter a custom seed, generate twice — same world both times
- Pan and zoom the rendered world
- Verify organisms appear as dots on land tiles (not in water)
- Expand advanced settings, change vegetation density, regenerate — observe difference

---

## Testing Strategy

### Unit Tests

- `WorldConfiguration` range validation (all parameters)
- `Tile` invariants (vegetation clamping, water tiles)
- `Organism` construction (position validation, genome clamping)
- `SimulationRandom` determinism (same seed → same sequence)
- `WorldGenerator` determinism (same config → same fingerprint)
- `WorldGenerator` distribution (water level affects water tile ratio, vegetation parameter affects density)
- Organism placement validity (all on land, correct counts)

### Integration Tests

- API endpoint returns valid JSON for valid config
- API endpoint returns same fingerprint for same config
- API endpoint rejects invalid config with 400
- API applies defaults for omitted optional fields

### Manual Testing Steps

1. Start backend: `dotnet run --project Backend/src/WildSeed.Api/WildSeed.Api.csproj`
2. Start frontend: `npm --prefix Frontend run dev`
3. Select each preset and generate — verify terrain variety
4. Use same seed twice — verify identical worlds
5. Pan and zoom the map — verify smooth interaction
6. Change advanced settings and regenerate — verify parameter effects
7. Try extreme values (max populations, max water) — verify graceful handling

## Performance Considerations

- 256×256 grid = 65,536 tiles. FastNoiseLite evaluates O(1) per coordinate — generation is sub-second.
- JSON payload for 256×256 ≈ 500KB–1MB. Acceptable for one-shot REST.
- PixiJS `ParticleContainer` with 65K particles renders efficiently in WebGL; per-particle tint updates avoid rebatching.
- Camera `isRenderGroup: true` offloads transform to GPU.

## References

- PRD: [`context/foundation/prd.md`](file:///C:/Users/sheel/Documents/.NET/WildSeed/context/foundation/prd.md)
- Roadmap: [`context/foundation/roadmap.md`](file:///C:/Users/sheel/Documents/.NET/WildSeed/context/foundation/roadmap.md)
- Archived F-01 plan: [`context/archive/2026-08-26-determinism-performance-contract/plan.md`](file:///C:/Users/sheel/Documents/.NET/WildSeed/context/archive/2026-08-26-determinism-performance-contract/plan.md)
- Architecture rules: [`DependencyRulesTests.cs`](file:///C:/Users/sheel/Documents/.NET/WildSeed/Backend/tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs)
- CanonicalStateWriter: [`CanonicalStateWriter.cs`](file:///C:/Users/sheel/Documents/.NET/WildSeed/Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs)
- FastNoiseLite: https://github.com/Auburn/FastNoiseLite

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Domain World Model

#### Automated

- [x] 1.1 Solution builds cleanly
- [x] 1.2 Architecture tests pass
- [x] 1.3 All existing tests pass

#### Manual

- [x] 1.4 Domain types compile without referencing any external framework

### Phase 2: Simulation Procedural Generation

#### Automated

- [x] 2.1 Solution builds cleanly
- [x] 2.2 All existing tests pass

#### Manual

- [x] 2.3 WorldGenerator.Generate returns a populated WorldMap with terrain variety

### Phase 3: Backend Tests

#### Automated

- [x] 3.1 All tests pass
- [x] 3.2 Architecture tests still green

#### Manual

- [x] 3.3 Test names clearly describe observable behavior

### Phase 4: API World Generation Endpoint

#### Automated

- [x] 4.1 Solution builds
- [x] 4.2 All tests pass
- [x] 4.3 API starts without errors

#### Manual

- [x] 4.4 POST /api/world/generate returns a JSON world snapshot with terrain variety and organisms
- [x] 4.5 Same request twice returns the same fingerprint

### Phase 5: Frontend Project Setup and PixiJS Rendering

#### Automated

- [x] 5.1 Frontend builds
- [x] 5.2 Frontend lints

#### Manual

- [x] 5.3 Dev server shows a full-viewport canvas
- [x] 5.4 A hardcoded test world renders as a colored tile grid
- [x] 5.5 Mouse drag pans the view, scroll wheel zooms

### Phase 6: Frontend Configuration UI

#### Automated

- [x] 6.1 Frontend builds
- [x] 6.2 Frontend lints

#### Manual

- [x] 6.3 Selecting a preset fills all parameter fields
- [x] 6.4 Generate World sends request and renders result
- [x] 6.5 Same seed twice produces same visual result
- [x] 6.6 Advanced settings expand/collapse correctly

### Phase 7: API Integration Test

#### Automated

- [x] 7.1 All tests pass
- [x] 7.2 Solution builds

### Phase 8: Integration and Final Verification

#### Automated

- [x] 8.1 Full backend build
- [x] 8.2 All backend tests pass
- [x] 8.3 Architecture tests green
- [x] 8.4 Frontend builds
- [x] 8.5 Frontend lints

#### Manual

- [x] 8.6 Full end-to-end flow works (presets, regeneration, pan/zoom, determinism)
- [x] 8.7 Organisms appear on land tiles only
- [x] 8.8 Advanced settings affect generation output
