# Predator-Prey Dynamics Implementation Plan

## Overview

Implement roadmap slice S-03: predator-prey dynamics. Organisms gain mutual awareness through a high-performance spatial index. Carnivores autonomously track, pursue (Hunt), attack, and consume prey (Herbivores) to satisfy hunger; Herbivores sense approaching predators and flee (Flee) in the opposite direction. Sprinting dynamics (1.5x speed at 2x energy cost) create chase tension. A new `SimulationContract.Version3` establishes golden checkpoints without invalidating prior contract baselines, while PixiJS renders distinct visual cues and predation death effects.

## Current State Analysis

In S-02, the simulation established the deterministic survival loop (`SimulationEngine`) for herbivores (grazing, drinking, resting, exploring) and basic starvation for carnivores.
Currently:
- `OrganismAction` contains `Explore`, `SeekFood`, `Eat`, `SeekWater`, `Drink`, and `Rest`. It lacks `Hunt` (chase), `Attack`, and `Flee`.
- `DeathCause` contains `Starvation`, `Dehydration`, and `OldAge`. It lacks `Predation`.
- `PerceptionService` only scans tiles for vegetation and water; it does not perceive nearby organisms.
- `ActionScorer` only scores water, vegetation, and rest for herbivores; carnivores can only drink, rest, or explore until they starve.
- No spatial index exists for live organisms during tick execution; naive distance checks would be $O(N^2)$ (25 million comparisons for $N=5,000$).
- `SimulationContract` defines `Version1` and `Version2` with `SurvivalRulesV2` and canonical state hashing.

## Desired End State

After this plan is complete:
1. **Spatial Indexing**: A uniform grid spatial index (`SpatialGrid`) maintains organism positions with zero heap allocation per tick.
2. **Mutual Perception**: Herbivores detect predators within danger perception radius; Carnivores detect prey within hunting perception radius.
3. **Action Scoring**:
   - Carnivores score `Hunt` towards the closest detected prey and `Attack` when in striking distance on the same/adjacent tile.
   - Herbivores score `Flee` away from the closest detected predator with high priority unless in agonal thirst/hunger.
4. **Sprint & Energy Tradeoffs**: Hunting and fleeing move at sprint speed ($1.5\times$) consuming double energy ($2\times$), tiring organisms over sustained chases.
5. **Predation Resolution**: Successful attacks instantly kill the prey with `DeathCause.Predation`, immediately satisfying the predator's hunger and awarding energy, while emitting a `PredationEvent`.
6. **Contract Version 3**: `SimulationContract.Version3` and `SurvivalRulesV3` define the canonical fingerprint contract for S-03, backed by golden checkpoint tests. Existing V1 and V2 tests remain green.
7. **Frontend Visualization**: PixiJS renders distinct tints/indicators for predators and prey, visual states for hunting and fleeing, and animated predation impact effects.
8. **Performance**: Headless execution with 5,000 organisms achieves at least 200 ticks/second.

### Key Discoveries

- `Backend/src/WildSeed.Domain/Organisms/OrganismAction.cs:3`: Enum can be extended with `Hunt`, `Attack`, `Flee`.
- `Backend/src/WildSeed.Domain/Organisms/DeathCause.cs:3`: Enum can be extended with `Predation`.
- `Backend/src/WildSeed.Simulation/Perception/PerceptionService.cs:9`: Perception currently only checks tiles; needs organism perception via `SpatialGrid`.
- `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs:23`: Engine order of operations must resolve movement, attack claims, instant kills, and event generation before end-of-tick death cleanup.
- `Backend/src/WildSeed.Api/Contracts/SimulationSnapshotResponse.cs:3`: Frontend already receives organism action in snapshots.
- `Frontend/src/rendering/WorldRenderer.ts:124`: PixiJS renders organism dots and can render action indicators and color distinctions for carnivores vs herbivores.

## What We're NOT Doing

- No reproduction, mating, genome recombination, or mutations (belongs to S-04).
- No historical charts or timeline analytics (belongs to S-05).
- No organism inspection inspector window (belongs to S-06).
- No God Mode interactions or manual spawning (belongs to S-07).
- No multi-tick combat with HP / health bars (instant kill on attack matches PRD §Non-Goals).
- No carcass items or decomposing meat resources on terrain tiles (instant feeding satisfies PRD).

## Implementation Approach

1. **Domain & Contract Foundations**: Extend `OrganismAction` and `DeathCause`. Create `SurvivalRulesV3` with hunting radii, sprint multipliers, and energy costs. Introduce `SimulationContract.Version3`.
2. **High-Performance Spatial Partitioning**: Implement `SpatialGrid` inside `WildSeed.Simulation` with preallocated cell buckets for $O(1)$ nearest-neighbor queries without allocations.
3. **Behavioral Perception & Scoring**: Update `PerceptionService` to populate nearby threats and prey. Update `ActionScorer` with dynamic target tracking, threat prioritization, and sprint dynamics.
4. **Engine & Combat Resolution**: Update `SimulationEngine` and `MovementResolver` to apply sprint speeds and energy costs. Add `PredationResolver` to process attacks, eliminate prey deterministically, feed predators, and generate `PredationEvent`s.
5. **SignalR & Transport**: Ensure new actions and death causes serialize cleanly across REST and SignalR.
6. **PixiJS Rendering**: Distinguish carnivores (warm red/crimson accents) from herbivores (earthy green/amber accents), highlight sprinting/hunting/fleeing organisms, and trigger a brief visual burst on predation kills.
7. **Determinism Goldens & Benchmarks**: Add `ContractV3GoldenFingerprints` and `SurvivalLoopPopulation5000V3Benchmark` to verify regression-free determinism and high throughput.

## Critical Implementation Details

- **Performance constraints**: `SpatialGrid` must reuse preallocated arrays and linked-list head/next pointer buffers across ticks to avoid millions of heap allocations during organism repositioning.
- **State sequencing**: In each tick: (1) update spatial index, (2) perceive environment + organisms, (3) score intents, (4) resolve movement with sprint modifiers, (5) resolve attack intents and instant kills, (6) resolve vegetation grazing, (7) evaluate environmental deaths (starvation, thirst, age), (8) emit snapshots and death events.
- **User experience spec**: Predator pursuit and prey fleeing should be immediately recognizable on the canvas through subtle speed differences and action indicator highlights.

---

## Phase 1: Domain & Contract Foundations

### Overview

Add domain actions and death causes for predation, define `SurvivalRulesV3`, create `SpatialGrid`, and register `SimulationContract.Version3`.

### Changes Required

#### 1. Domain Enums

**File**: `Backend/src/WildSeed.Domain/Organisms/OrganismAction.cs`
**Intent**: Add `Hunt`, `Attack`, and `Flee` to `OrganismAction`.
**Contract**: `Hunt = 6, Attack = 7, Flee = 8`.

**File**: `Backend/src/WildSeed.Domain/Organisms/DeathCause.cs`
**Intent**: Add `Predation` to `DeathCause`.
**Contract**: `Predation = 3`.

#### 2. Survival Rules V3 & Simulation Contract

**File**: `Backend/src/WildSeed.Simulation/Contracts/SurvivalRulesV3.cs`
**Intent**: Define constants for predator detection radius, prey danger radius, sprint multiplier ($1.5\times$), sprint energy cost, and attack range ($1.0$ tile).
**Contract**: Static configuration class containing V3 simulation parameters.

**File**: `Backend/src/WildSeed.Simulation/Contracts/SimulationContract.cs`
**Intent**: Add `Version3` identity and set `CurrentVersion = Version3`.
**Contract**: `public static readonly SimulationContract Version3 = new(3, ...);`

#### 3. Zero-Allocation Spatial Grid

**File**: `Backend/src/WildSeed.Simulation/Spatial/SpatialGrid.cs`
**Intent**: Implement a uniform cell grid using flat integer arrays (`head` and `next`) for $O(1)$ spatial queries within radius $R$ without memory allocation.
**Contract**: Methods: `Clear()`, `Insert(int index, float x, float y)`, `FindNearest(float x, float y, float radius, Species targetSpecies, ReadOnlySpan<OrganismState> organisms)`.

### Success Criteria

#### Automated Verification

- `dotnet build Backend/WildSeed.slnx --no-restore` compiles cleanly.
- Unit tests verify `SpatialGrid` insertion and nearest neighbor queries: `dotnet test Backend/tests/WildSeed.Simulation.Tests/ --no-build`.

#### Manual Verification

- Architecture tests pass confirming no domain reverse dependencies.

---

## Phase 2: Organism Perception & Action Scoring

### Overview

Expand `PerceptionService` to detect organisms via `SpatialGrid` and update `ActionScorer` to evaluate `Hunt`, `Attack`, and `Flee` behaviors.

### Changes Required

#### 1. Perception Service Expansion

**File**: `Backend/src/WildSeed.Simulation/Perception/PerceptionResult.cs`
**Intent**: Add nullable target positions for nearest threat predator (for herbivores) and nearest target prey (for carnivores).
**Contract**: `public sealed record PerceptionResult((int X, int Y)? FoodTile, (int X, int Y)? WaterTile, (float X, float Y)? NearestThreat, (float X, float Y)? NearestPrey, Guid? PreyId);`

**File**: `Backend/src/WildSeed.Simulation/Perception/PerceptionService.cs`
**Intent**: Use `SpatialGrid` to query nearest predator for herbivores (within danger radius) and nearest herbivore for carnivores (within hunt radius).
**Contract**: `Perceive(SimulationState state, OrganismState organism, SpatialGrid spatialGrid)`.

#### 2. Action Scorer Logic for Predators & Prey

**File**: `Backend/src/WildSeed.Simulation/Behavior/ActionScorer.cs`
**Intent**:
- Herbivores: When a threat is perceived, score `Flee` with high utility proportional to threat proximity, overriding feeding/drinking unless in critical need.
- Carnivores: When hungry and prey is perceived, score `Attack` if within striking distance ($\le 1.0$ tile), otherwise `Hunt` toward prey position.
**Contract**: `Score(OrganismState organism, PerceptionResult perception)`.

### Success Criteria

#### Automated Verification

- Unit tests verify carnivore scores `Hunt` when prey is visible and `Attack` when in range.
- Unit tests verify herbivore scores `Flee` when predator is in danger radius.

#### Manual Verification

- Verify utility scoring balance between critical thirst and threat avoidance.

---

## Phase 3: Engine Execution & Predation Resolution

### Overview

Update `SimulationEngine`, `MovementResolver`, and add `PredationResolver` to execute sprint movements, resolve attacks, kill prey deterministically, and replenish predator hunger and energy.

### Changes Required

#### 1. Movement Resolver Sprint Dynamics

**File**: `Backend/src/WildSeed.Simulation/Movement/MovementResolver.cs`
**Intent**: Apply $1.5\times$ speed multiplier during `Hunt` and `Flee` actions, with vector-away movement for fleeing.
**Contract**: `Move(SimulationState state, OrganismState organism, (float X, float Y)? target, bool isFleeing, float speedMultiplier)`.

#### 2. Predation Resolver & Death Events

**File**: `Backend/src/WildSeed.Simulation/Lifecycle/PredationResolver.cs`
**Intent**: Group attack intents, eliminate attacked herbivores with `DeathCause.Predation`, feed attacking carnivores (reducing hunger by `PredationHungerGain` and increasing energy), and generate `SimulationDeathEvent`.
**Contract**: `Resolve(SimulationState state, ReadOnlySpan<(OrganismState Organism, ActionIntent Intent)> scored)`.

#### 3. Simulation Engine Integration

**File**: `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs`
**Intent**: Integrate `SpatialGrid` update, perception with spatial queries, sprint energy drain ($2\times$), and `PredationResolver` into the tick pipeline.
**Contract**: `AdvanceTick()` updates spatial grid, scores actions, moves organisms, resolves predation, grazing, and deaths.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/WildSeed.slnx --no-build` passes with all domain and simulation tests.
- Unit tests confirm predator kills prey, decreases hunger, and generates predation death events.

#### Manual Verification

- Observe carnivore population survival in a world with abundant prey.

---

## Phase 4: API & Event Transport

### Overview

Expose new actions and predation death events through SignalR and REST DTOs.

### Changes Required

#### 1. SignalR Snapshot & Event Contracts

**File**: `Backend/src/WildSeed.Api/Contracts/SimulationSnapshotResponse.cs`
**Intent**: Verify string mapping or enum serialization for `Hunt`, `Attack`, `Flee`, and `Predation`.
**Contract**: DTOs reflect new `OrganismAction` and `DeathCause` values.

**File**: `Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs`
**Intent**: Ensure predation death events are merged and delivered via `LatestSnapshotMailbox`.
**Contract**: Session broadcasts death events with cause `Predation`.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/tests/WildSeed.Api.Tests/ --no-build` passes.

#### Manual Verification

- Verify SignalR hub streams snapshots containing `Hunt` and `Flee` actions.

---

## Phase 5: Frontend Visuals & Telemetry

### Overview

Update PixiJS renderer and React telemetry components to visibly display carnivores hunting, herbivores fleeing, and predation death effects.

### Changes Required

#### 1. Frontend Types & DTOs

**File**: `Frontend/src/transport/types.ts`
**Intent**: Add `Hunt`, `Attack`, `Flee` to `OrganismAction` type and `Predation` to `DeathCause` type.
**Contract**: TypeScript union types updated.

#### 2. PixiJS Organism & Effect Rendering

**File**: `Frontend/src/rendering/WorldRenderer.ts`
**Intent**:
- Distinct color palette: Carnivores in fiery coral/crimson, Herbivores in vibrant leaf green/amber.
- Visual state cues: Outline or trail indicator for sprinting/hunting/fleeing organisms.
- Predation burst effect: Short-lived reddish particle/ripple on predation death coordinate.
**Contract**: `updateOrganisms(organisms, deaths)` renders sprites and triggers burst effects.

#### 3. Control Panel & Status Bar Telemetry

**File**: `Frontend/src/features/simulation/SimulationControls.tsx`
**Intent**: Display current action breakdown including Hunting and Fleeing counts in the aggregate action bar.
**Contract**: Action counts include Hunt/Flee.

### Success Criteria

#### Automated Verification

- `npm --prefix Frontend run build` and `npm --prefix Frontend run lint` succeed cleanly.

#### Manual Verification

- Launch frontend (`npm --prefix Frontend run dev`) and visually verify carnivores chasing herbivores and burst effects on kills.

---

## Phase 6: Determinism, Golden Checkpoints & Regression Testing

### Overview

Establish `ContractV3GoldenFingerprints` and verify deterministic repeatability across multiple seeds and simulation paces.

### Changes Required

#### 1. Golden Fingerprints for Contract V3

**File**: `Backend/tests/WildSeed.Simulation.Tests/Determinism/ContractV3GoldenFingerprints.cs`
**Intent**: Record canonical state hashes at ticks 0, 10, 50, 100, 200 for standard test seeds under V3 rules.
**Contract**: Golden test suite asserting exact SHA-256 fingerprint matches.

#### 2. Predator-Prey Determinism Tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/Determinism/PredatorPreyDeterminismTests.cs`
**Intent**: Test that two independent simulations with identical seed produce identical predation events, positions, and death counts.
**Contract**: Equality assertions across independent runs.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/tests/WildSeed.Simulation.Tests/ --no-build` passes 100%.
- Goldens for V1, V2, and V3 all pass concurrently.

#### Manual Verification

- Verify determinism across 5 consecutive runs with identical seed.

---

## Phase 7: Benchmarks & Performance Verification

### Overview

Implement and execute `SurvivalLoopPopulation5000V3Benchmark` to verify that 5,000 active organisms with mutual spatial perception and predation run at >200 ticks/sec headless.

### Changes Required

#### 1. V3 Performance Benchmark

**File**: `Backend/benchmarks/WildSeed.Simulation.Benchmarks/Benchmarks/SurvivalLoopPopulation5000V3Benchmark.cs`
**Intent**: Benchmark `SimulationEngine.AdvanceTick()` with 5,000 organisms under V3 rules.
**Contract**: BenchmarkDotNet test fixture.

### Success Criteria

#### Automated Verification

- `dotnet build Backend/benchmarks/WildSeed.Simulation.Benchmarks/ --no-restore` compiles cleanly.

#### Manual Verification

- Run benchmark and confirm throughput meets performance expectations (>200 ticks/sec).

---

## Testing Strategy

### Unit Tests

- `SpatialGridTests`: Verify spatial bucket partitioning and neighbor finding.
- `PredatorPerceptionTests`: Verify carnivore target selection within hunt radius.
- `PreyFleeTests`: Verify herbivore evasion vector away from predator.
- `PredationResolutionTests`: Verify attack damage, instant kill, and hunger satisfaction.

### Integration Tests

- `PredatorPreyDeterminismTests`: Multi-tick determinism with active predation.
- `ContractV3GoldenFingerprints`: Golden checksum verification at ticks 0, 50, 100.

### Manual Testing Steps

1. Start API (`dotnet run --project Backend/src/WildSeed.Api`) and Frontend (`npm --prefix Frontend run dev`).
2. Generate a world with 50 carnivores and 200 herbivores.
3. Observe carnivores pursuing herbivores with visual speed boost and distinct action color.
4. Observe herbivores fleeing when predators approach.
5. Observe visual burst when a predator catches and eats prey.
6. Test speed multipliers (1x, 5x, 20x, MAX) and verify smooth rendering and stability.

---

## Performance Considerations

- **Spatial Indexing**: `SpatialGrid` avoids GC allocations by reusing struct/integer arrays.
- **Dynamic Snapshots**: SignalR updates remain sampled at 15 Hz to keep network traffic bounded regardless of simulation tick rate.

---

## References

- PRD requirements: `context/foundation/prd.md` (FR-006, US-01)
- Roadmap definition: `context/foundation/roadmap.md` (S-03)
- Prior survival loop implementation: `context/changes/deterministic-survival-loop/plan.md`

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Domain & Contract Foundations

#### Automated

- [x] 1.1 Compile backend with updated domain enums and SurvivalRulesV3 — dae67c9
- [x] 1.2 Unit tests verify SpatialGrid operations and zero-allocation queries — dae67c9

#### Manual

- [x] 1.3 Verify architecture dependency rules remain clean — dae67c9

### Phase 2: Organism Perception & Action Scoring

#### Automated

- [x] 2.1 Unit tests for predator hunt perception and attack scoring — dae67c9
- [x] 2.2 Unit tests for prey threat perception and flee scoring — dae67c9

#### Manual

- [x] 2.3 Verify utility balance between thirst and threat evasion — dae67c9

### Phase 3: Engine Execution & Predation Resolution

#### Automated

- [x] 3.1 Unit tests for sprint movement and energy consumption — dae67c9
- [x] 3.2 Unit tests for attack resolution, instant kills, and predation events — dae67c9

#### Manual

- [x] 3.3 Verify multi-tick predator survival with prey grazing — dae67c9

### Phase 4: API & Event Transport

#### Automated

- [x] 4.1 API tests pass with predation death events and updated snapshot actions — dae67c9

#### Manual

- [x] 4.2 SignalR hub streams hunt and flee action states to clients — dae67c9

### Phase 5: Frontend Visuals & Telemetry

#### Automated

- [x] 5.1 Frontend builds cleanly and passes oxlint — dae67c9

#### Manual

- [x] 5.2 Visual observation of hunting, fleeing, and predation particle effects — dae67c9

### Phase 6: Determinism & Golden Checkpoints

#### Automated

- [x] 6.1 Contract V3 golden fingerprint tests pass — dae67c9
- [x] 6.2 Regression tests pass for V1 and V2 golden baselines — dae67c9

#### Manual

- [x] 6.3 Verify identical outcome across 5 consecutive runs with identical seed — dae67c9

### Phase 7: Benchmarks & Verification

#### Automated

- [x] 7.1 Population 5000 V3 benchmark compiles and runs cleanly — dae67c9

#### Manual

- [x] 7.2 Confirm simulation throughput meets >200 ticks/sec headless — dae67c9
