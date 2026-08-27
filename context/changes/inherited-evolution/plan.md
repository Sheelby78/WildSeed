# Inherited Evolution Implementation Plan

## Overview

Implement roadmap slice S-04: inherited evolution. Organisms evolve across generations through multi-trait genomes (`Speed`, `Size`, `Vision`) with metabolic and physical trade-offs. Eligible organisms autonomously seek mates and perform two-parent sexual reproduction with configurable mutation rates, deterministic PRNG crossover, parental energy depletion, refractory breeding cooldowns, and lineage tracking (`MotherId`, `FatherId`, `Generation`). A new `SimulationContract.Version4` establishes golden checksums, SignalR streams `OrganismBorn` events, and PixiJS visually represents genetic size variation and mating animations.

## Current State Analysis

In S-03 (`predator-prey-dynamics`), the simulation established spatial indexing, mutual perception, carnivore hunting/attacks, herbivore fleeing, and sprint dynamics under `SimulationContract.Version3` and `SurvivalRulesV3`.
Currently:
- `Genome` only contains `float Speed` (clamped `0.1f..10.0f`). It lacks `Size` and `Vision` traits and associated metabolic trade-offs.
- `OrganismAction` contains `Explore`, `SeekFood`, `Eat`, `SeekWater`, `Drink`, `Rest`, `Hunt`, `Attack`, `Flee`. It lacks `Mate`.
- `OrganismState` lacks lineage tracking (`MotherId`, `FatherId`, `Generation`) and reproduction cooldown state (`ReproductionCooldownTicks`).
- Organisms only die; no new organisms are ever born during simulation execution, leading to eventual total population extinction.
- `WorldConfiguration` already includes `MutationProbability` and `MutationStrength`, but they are unused by the simulation engine.
- `SpatialGrid` queries threats and prey by species, but lacks eligibility-filtered mate queries for same-species organisms.
- `SimulationContract` defines `Version1`, `Version2`, and `Version3`.

## Desired End State

After this plan is complete:
1. **Multi-Trait Genomes**: `Genome` defines `Speed` ($0.5..3.0$), `Size` ($0.5..2.5$), and `Vision` ($4.0..16.0$) with physical and metabolic trade-offs (larger organisms require more energy and move slightly slower; faster organisms burn more metabolic energy; higher vision increases perception radius).
2. **Lineage & Maturity Tracking**: `OrganismState` and domain models track `MotherId`, `FatherId`, `Generation`, `AgeTicks`, and `ReproductionCooldownTicks`.
3. **Mating Behavior & Action Scoring**: Organisms evaluate `OrganismAction.Mate` when mature ($\ge 100$ ticks), well-fed, hydrated, high-energy ($\ge 600$), and not in cooldown. Herbivores and carnivores prioritize survival needs over mating when in critical need.
4. **Sexual Reproduction & Trait Crossover**: When two eligible mates are adjacent, reproduction occurs:
   - Offspring trait averages parental values: $\text{Trait}_{\text{base}} = \frac{\text{Trait}_A + \text{Trait}_B}{2}$.
   - Deterministic mutation applies per trait: $\text{Trait}_{\text{final}} = \text{Clamp}(\text{Trait}_{\text{base}} \times (1 + \Delta))$ where $\Delta \in [-\text{MutationStrength}, +\text{MutationStrength}]$ with probability $\text{MutationProbability}$.
   - Parents transfer $50\%$ energy to offspring (or incur significant energy cost) and enter a refractory cooldown ($200$ ticks).
   - Offspring spawns at an adjacent walkable tile.
5. **Carrying Capacity Guardrail**: Total living organisms are capped at $5,000$ to prevent unbounded growth and ensure performance contract guarantees.
6. **Contract Version 4**: `SimulationContract.Version4` and `SurvivalRulesV4` define canonical fingerprint baselines for multi-generation evolution, while V1, V2, and V3 tests remain green.
7. **SignalR & Frontend Telemetry**: `OrganismBorn` events stream via SignalR; PixiJS renders organisms with visual radius scaling proportional to `Genome.Size` and plays subtle birth/mating pulse effects.
8. **Performance**: Headless simulation with $5,000$ active organisms running mating and evolution achieves $>200$ ticks/sec.

### Key Discoveries

- `Backend/src/WildSeed.Domain/Organisms/Genome.cs:3`: Struct currently only has `Speed`. Needs `Size` and `Vision` with mathematical trade-offs.
- `Backend/src/WildSeed.Domain/Organisms/OrganismAction.cs:3`: Enum can be extended with `Mate`.
- `Backend/src/WildSeed.Domain/World/WorldConfiguration.cs:12`: `MutationProbability` and `MutationStrength` are already present in configuration and available to the engine.
- `Backend/src/WildSeed.Simulation/Spatial/SpatialGrid.cs:34`: Spatial grid can be extended to query eligible mates of same species within `Genome.Vision` radius.
- `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs:36`: Engine tick loop needs a deterministic `ReproductionResolver` step that collects new births, deducts parent energy, and inserts newborns into active organism list and spatial index.
- `Frontend/src/rendering/WorldRenderer.ts:124`: PixiJS renders organisms as circles and can easily scale graphics radius by `genome.size` and trigger pulse effects on `OrganismBorn`.

## What We're NOT Doing

- No asexual reproduction or self-cloning without a mate (two-parent sexual reproduction matches FR-007).
- No historical charts or timeline analytics charts (belongs to S-05: `ecosystem-statistics`).
- No full organism inspection window or interactive ancestor tree viewer UI (belongs to S-06: `organism-inspection`).
- No God Mode creature spawning or manual genome editing (belongs to S-07: `limited-god-mode`).
- No gender/sex segregation (hermaphroditic/compatible pairing within species keeps the MVP model lean and avoids unneeded mating stalls).
- No speciation or branching species taxonomy (matches PRD §Non-Goals).

## Implementation Approach

1. **Domain & Contract Foundations**:
   - Expand `Genome` with `Speed`, `Size`, `Vision`.
   - Add `OrganismAction.Mate`.
   - Add `OrganismBorn` simulation event.
   - Define `SurvivalRulesV4` (maturation age, mating energy threshold, mating cooldown ticks, energy cost, trait clamp ranges).
   - Register `SimulationContract.Version4`.
2. **Spatial Indexing & Mate Perception**:
   - Extend `SpatialGrid` to support querying nearest eligible mates of the same species.
   - Update `PerceptionService` to calculate perception radius dynamically based on `organism.Genome.Vision` and return `NearestMate` candidate.
3. **Behavior Scoring**:
   - Update `ActionScorer` to evaluate `OrganismAction.Mate` when organism meets age, energy, hunger, thirst, and cooldown criteria, prioritizing thirst/hunger/flee over mating.
4. **Reproduction & Mutation Resolution**:
   - Implement `ReproductionResolver`: pair adjacent mating intents deterministically (preventing double-mating in the same tick), calculate crossover traits, apply deterministic PRNG mutations bounded by `MutationStrength`, drain parent energy, apply refractory cooldown, and spawn offspring.
   - Guard against exceeding maximum population cap ($5,000$).
5. **Engine Integration**:
   - Integrate `ReproductionResolver` into `SimulationEngine.AdvanceTick()`.
   - Update `MovementResolver` and metabolic need costs to account for `Speed` and `Size` trade-offs.
   - Emit `OrganismBorn` events alongside death events in tick results.
6. **API & SignalR Transport**:
   - Update snapshot and event DTOs with genome traits, lineage references (`MotherId`, `FatherId`, `Generation`), and `OrganismBorn` payload.
7. **Frontend Visuals & Telemetry**:
   - Update TypeScript interfaces in `Frontend/src/transport/types.ts`.
   - Update `WorldRenderer.ts` to scale organism circle radius according to `genome.size` and trigger subtle birth pulse rings.
   - Add `Mate` action count to simulation telemetry.
8. **Determinism Goldens & Benchmarks**:
   - Add `ContractV4GoldenFingerprints` with SHA-256 state hashes across multiple seeds.
   - Add `SurvivalLoopPopulation5000V4Benchmark` to verify $>200$ ticks/sec throughput under active reproduction and evolution.

## Critical Implementation Details

- **Deterministic PRNG Stream**: Mutation and trait crossover must consume the engine's deterministic random stream, ensuring identical seeds produce bit-for-bit identical generational lineages.
- **Pairing Conflict Resolution**: If multiple organisms attempt to mate with the same partner in the same tick, resolution must be strictly ordered by `Organism.Id` to avoid non-deterministic race conditions and double-mating exploits.
- **Zero-Allocation Newborn Insertion**: Spawning newborns must append to pre-allocated or pooled buffers during tick execution and update the spatial grid without triggering GC churn.
- **Ecological Balance**: Without energetic trade-offs (e.g. larger size = higher base energy drain; faster speed = higher sprint/move cost), genomes would trivially drift to maximum speed and size..

---

## Phase 1: Domain Genome Expansion & Contract Foundations

### Overview

Expand `Genome` with `Speed`, `Size`, and `Vision` traits and invariant clamps. Add `OrganismAction.Mate`, `OrganismBorn` event, lineage fields (`MotherId`, `FatherId`, `Generation`), and introduce `SimulationContract.Version4` with `SurvivalRulesV4`.

### Changes Required

#### 1. Domain Genome & Lineage

**File**: `Backend/src/WildSeed.Domain/Organisms/Genome.cs`
**Intent**: Expand struct with `Speed` ($0.5f..3.0f$), `Size` ($0.5f..2.5f$), and `Vision` ($4.0f..16.0f$).
**Contract**: `public readonly record struct Genome(float Speed = 1.0f, float Size = 1.0f, float Vision = 8.0f)`.

**File**: `Backend/src/WildSeed.Domain/Organisms/Organism.cs`
**Intent**: Add `Guid? MotherId`, `Guid? FatherId`, and `int Generation` to domain model constructor and properties.
**Contract**: Domain organism maintains optional parent references and generation index.

**File**: `Backend/src/WildSeed.Domain/Organisms/OrganismAction.cs`
**Intent**: Add `Mate = 9` to `OrganismAction`.
**Contract**: Enum value `Mate = 9`.

**File**: `Backend/src/WildSeed.Simulation/Events/OrganismBorn.cs`
**Intent**: Define `OrganismBorn` simulation event containing `OrganismId`, `Species`, `X`, `Y`, `MotherId`, `FatherId`, `Generation`, and `Genome`.
**Contract**: Inherits from `SimulationEvent`.

#### 2. Survival Rules V4 & Simulation Contract

**File**: `Backend/src/WildSeed.Simulation/Contracts/SurvivalRulesV4.cs`
**Intent**: Define constants for maturation age ($100$ ticks), mating energy threshold ($600$), mating cooldown ($200$ ticks), mating energy cost ($300$), trait clamp ranges, and metabolic cost scaling factors.
**Contract**: Static configuration class containing V4 simulation parameters.

**File**: `Backend/src/WildSeed.Simulation/Contracts/SimulationContract.cs`
**Intent**: Add `Version4` identity and set `CurrentVersion = Version4`.
**Contract**: `public static readonly SimulationContract Version4 = new(4, ...);`

### Success Criteria

#### Automated Verification

- `dotnet build Backend/WildSeed.slnx --no-restore` compiles cleanly.
- Unit tests verify `Genome` clamp constraints and `OrganismBorn` event creation.

#### Manual Verification

- Architecture tests pass confirming no domain reverse dependencies.

---

## Phase 2: Perception & Mate Scoring

### Overview

Enhance `SpatialGrid` with mate proximity queries and update `ActionScorer` to evaluate `OrganismAction.Mate` when organisms meet maturity, energy, and cooldown criteria.

### Changes Required

#### 1. Spatial Grid Mate Queries

**File**: `Backend/src/WildSeed.Simulation/Spatial/SpatialGrid.cs`
**Intent**: Add `FindNearestEligibleMate(float x, float y, float radius, Species species, Guid selfId, ReadOnlySpan<OrganismState> organisms)` to find the closest eligible same-species partner without heap allocation.
**Contract**: Returns nullable index/tuple of the nearest eligible mate.

#### 2. Dynamic Perception

**File**: `Backend/src/WildSeed.Simulation/Perception/PerceptionResult.cs`
**Intent**: Add `(float X, float Y)? NearestMate` and `Guid? MateId` to `PerceptionResult`.
**Contract**: Record updated with mate coordinates and target ID.

**File**: `Backend/src/WildSeed.Simulation/Perception/PerceptionService.cs`
**Intent**: Use `organism.Genome.Vision` for search radius instead of static constants, and query nearest eligible mate when organism is in mating condition.
**Contract**: `Perceive(SimulationState state, OrganismState organism, SpatialGrid spatialGrid)`.

#### 3. Action Scorer Logic for Mating

**File**: `Backend/src/WildSeed.Simulation/Behavior/ActionScorer.cs`
**Intent**: Score `OrganismAction.Mate` towards perceived mate when organism has high energy ($\ge 600$), low thirst/hunger, mature age, and zero cooldown. Mating score yields to critical thirst/hunger and predator flee responses.
**Contract**: `Score(OrganismState organism, PerceptionResult perception)`.

### Success Criteria

#### Automated Verification

- Unit tests verify eligible organisms score `Mate` when partner is visible.
- Unit tests verify mating score is suppressed during critical hunger, thirst, or predator threats.

#### Manual Verification

- Verify utility balance between feeding, fleeing, and mate search.

---

## Phase 3: Reproduction & Mutation Resolution

### Overview

Implement `ReproductionResolver` with deterministic trait blending, mutation delta scaling (`MutationProbability`, `MutationStrength`), energy transfer, cooldown resets, offspring placement, and capacity capping ($\le 5,000$).

### Changes Required

#### 1. Reproduction Resolver

**File**: `Backend/src/WildSeed.Simulation/Lifecycle/ReproductionResolver.cs`
**Intent**:
- Pair adjacent mating organisms deterministically sorted by `Id`.
- Blend traits: `(A + B) / 2.0f`.
- Apply deterministic mutation delta per trait using configured `MutationProbability` and `MutationStrength`.
- Deduct `MatingEnergyCost` from both parents and reset `ReproductionCooldownTicks = MatingCooldownTicks`.
- Create newborn `OrganismState` with `MotherId`, `FatherId`, `Generation`, place at adjacent walkable tile, and emit `OrganismBorn`.
- Skip creation if total live organisms reach $5,000$.
**Contract**: `Resolve(SimulationState state, ReadOnlySpan<(OrganismState Organism, ActionIntent Intent)> scored, ref Random prng)`.

### Success Criteria

#### Automated Verification

- Unit tests verify trait averaging and mutation bounds.
- Unit tests verify parental energy deduction and cooldown reset.
- Unit tests verify mutual pairing without duplicate mating in a single tick.
- Unit tests verify population cap at 5,000 organisms.

#### Manual Verification

- Verify trait distribution drift over multiple generations in isolated unit tests.

---

## Phase 4: Simulation Engine & Lifecycle Pipeline

### Overview

Integrate `ReproductionResolver` into `SimulationEngine.AdvanceTick()`, update `MovementResolver` and metabolic need costs based on genome traits, and emit `OrganismBorn` events alongside death events.

### Changes Required

#### 1. State & Engine Updates

**File**: `Backend/src/WildSeed.Simulation/Engine/OrganismState.cs`
**Intent**: Add `Guid? MotherId`, `Guid? FatherId`, `int Generation`, and `int ReproductionCooldownTicks`. Decrement cooldown ticks in tick loop.
**Contract**: Organism state maintains reproduction lifecycle data.

**File**: `Backend/src/WildSeed.Simulation/Movement/MovementResolver.cs`
**Intent**: Apply `organism.Genome.Speed` scaling to base movement and sprint movement.
**Contract**: Movement distance scales proportionally with speed trait.

**File**: `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs`
**Intent**:
- Decrement `ReproductionCooldownTicks`.
- Scale metabolic energy consumption by `Genome.Speed` and `Genome.Size`.
- Execute `ReproductionResolver` and add newborns to active state and spatial grid.
- Include `OrganismBorn` in tick result event list.
**Contract**: `AdvanceTick()` advances generations, resolves births, and emits combined events.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/WildSeed.slnx --no-build` passes with all domain and simulation tests.
- Multi-generation simulation test confirms population growth and survival across 500+ ticks.

#### Manual Verification

- Observe sustained herbivore and carnivore reproduction cycles in long simulation runs.

---

## Phase 5: API & SignalR Transport

### Overview

Update snapshot and event DTOs with multi-trait genome data, lineage references, and `OrganismBorn` broadcast serialization.

### Changes Required

#### 1. DTO Updates & SignalR Hub

**File**: `Backend/src/WildSeed.Api/Contracts/SimulationSnapshotResponse.cs`
**Intent**: Add `GenomeDto` (`Speed`, `Size`, `Vision`), `MotherId`, `FatherId`, and `Generation` to organism snapshot DTO.
**Contract**: Snapshot DTO reflects expanded genome and lineage.

**File**: `Backend/src/WildSeed.Api/Contracts/OrganismBornDto.cs`
**Intent**: Add DTO for born events.
**Contract**: Serializable event payload.

**File**: `Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs`
**Intent**: Broadcast `OrganismBorn` events to connected SignalR clients.
**Contract**: Session dispatches birth events alongside death events.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/tests/WildSeed.Api.Tests/ --no-build` passes cleanly.

#### Manual Verification

- Verify SignalR hub streams snapshots containing multi-trait genomes and birth events.

---

## Phase 6: Frontend Visuals & Telemetry

### Overview

Update TypeScript types, render organism sprite scale dynamically based on `Genome.Size`, add subtle mating/birth particle animations in PixiJS, and include `Mate` in action counts.

### Changes Required

#### 1. Frontend Types & DTOs

**File**: `Frontend/src/transport/types.ts`
**Intent**: Update `Genome` type with `speed`, `size`, `vision`. Add `motherId`, `fatherId`, `generation`. Add `'Mate'` to `OrganismAction`. Add `OrganismBornEvent`.
**Contract**: TypeScript types reflect backend contracts.

#### 2. PixiJS Dynamic Trait Rendering

**File**: `Frontend/src/rendering/WorldRenderer.ts`
**Intent**:
- Scale organism circle radius by `organism.genome.size` (e.g. baseline radius $\times \text{size}$).
- Add subtle tint variation or speed trail for high-speed organisms.
- Play gentle expanding ring particle on birth coordinates when `OrganismBorn` events arrive.
**Contract**: `updateOrganisms(organisms, deaths, births)` renders scaled sprites and birth effects.

#### 3. Telemetry & Action Breakdown

**File**: `Frontend/src/features/simulation/SimulationControls.tsx`
**Intent**: Add `Mating` count to action breakdown and display average generation in status bar.
**Contract**: UI displays mating activity and generational progression.

### Success Criteria

#### Automated Verification

- `npm --prefix Frontend run build` and `npm --prefix Frontend run lint` succeed cleanly without warnings.

#### Manual Verification

- Launch frontend (`npm --prefix Frontend run dev`) and visually verify varying organism sizes, mating behavior, and birth animations.

---

## Phase 7: Determinism Goldens, Regression, & Benchmarks

### Overview

Implement `ContractV4GoldenFingerprints`, verify multi-generation determinism across seeds, ensure V1–V3 goldens stay green, and add `SurvivalLoopPopulation5000V4Benchmark` to verify $>200$ ticks/sec throughput.

### Changes Required

#### 1. Contract V4 Golden Fingerprints

**File**: `Backend/tests/WildSeed.Simulation.Tests/Determinism/ContractV4GoldenFingerprints.cs`
**Intent**: Record canonical state hashes at ticks 0, 10, 50, 100, 200, 500 for standard test seeds under V4 rules with active evolution.
**Contract**: Golden test suite asserting exact SHA-256 fingerprint matches.

#### 2. Inherited Evolution Determinism Tests

**File**: `Backend/tests/WildSeed.Simulation.Tests/Determinism/InheritedEvolutionDeterminismTests.cs`
**Intent**: Test that two independent simulations with identical seed and configuration produce identical offspring genomes, lineages, and population trajectories.
**Contract**: Deterministic equality assertions across multi-generation runs.

#### 3. V4 Performance Benchmark

**File**: `Backend/benchmarks/WildSeed.Simulation.Benchmarks/Benchmarks/SurvivalLoopPopulation5000V4Benchmark.cs`
**Intent**: Benchmark `SimulationEngine.AdvanceTick()` with 5,000 organisms under V4 evolution rules.
**Contract**: BenchmarkDotNet test fixture confirming $>200$ ticks/sec.

### Success Criteria

#### Automated Verification

- `dotnet test Backend/tests/WildSeed.Simulation.Tests/ --no-build` passes 100%.
- Goldens for V1, V2, V3, and V4 all pass concurrently.
- `dotnet build Backend/benchmarks/WildSeed.Simulation.Benchmarks/ --no-restore` compiles cleanly.

#### Manual Verification
- Run benchmark and confirm throughput meets performance expectations ($>200$ ticks/sec with 5,000 organisms).

---

## Testing Strategy

### Unit Tests

- `GenomeTests`: Trait initialization, clamping bounds, and trade-off formulas.
- `SpatialGridMateTests`: Zero-allocation nearest mate queries and eligibility filtering.
- `MatingActionScorerTests`: Mate action prioritization, energy threshold gating, and threat overrides.
- `ReproductionResolverTests`: Parental energy deduction, cooldown timers, trait blending, mutation deltas, and population capping.

### Integration Tests

- `InheritedEvolutionDeterminismTests`: Multi-generation determinism with reproduction and mutation.
- `ContractV4GoldenFingerprints`: Golden checksum verification at ticks 0, 50, 100, 200, 500.
- `RegressionDeterminismTests`: Ensure V1, V2, and V3 golden tests remain green.

### Manual Testing Steps

1. Start API (`dotnet run --project Backend/src/WildSeed.Api`) and Frontend (`npm --prefix Frontend run dev`).
2. Generate a world with 100 herbivores and 20 carnivores with mutation probability 0.1 and strength 0.2.
3. Accelerate simulation to 5x/20x and observe organisms seeking mates, reproducing, and producing varied-sized offspring.
4. Verify birth pulse animations on map canvas when new organisms spawn.
5. Check that populations sustain across generations without immediate extinction or runaway explosion beyond carrying capacity.

---

## Performance Considerations

- **Zero-Allocation Queries**: Mate search uses flat integer arrays in `SpatialGrid` without LINQ or closure allocations during the tick loop.
- **Population Cap**: Hard limit of 5,000 organisms prevents runaway memory allocations and preserves the headless throughput contract.
- **Sampled SignalR Telemetry**: Snapshot streaming remains throttled to 15 Hz regardless of simulation speed.

---

## References

- PRD requirements: `context/foundation/prd.md` (FR-001, FR-007, US-01)
- Roadmap definition: `context/foundation/roadmap.md` (S-04)
- Brief evolution specifications: `context/foundation/brief.md` (§9, §15, §16, §17)
- Prior predator-prey plan: `context/changes/predator-prey-dynamics/plan.md`

---

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Domain Genome Expansion & Contract Foundations

#### Automated

- [x] 1.1 Compile backend with expanded Genome traits, OrganismAction.Mate, and SurvivalRulesV4
- [x] 1.2 Unit tests verify Genome invariant bounds and OrganismBorn event creation

#### Manual

- [x] 1.3 Verify architecture dependency rules remain clean

### Phase 2: Perception & Mate Scoring

#### Automated

- [x] 2.1 Unit tests for spatial grid mate queries and dynamic vision perception
- [x] 2.2 Unit tests for mate action scoring and survival threshold overrides

#### Manual

- [x] 2.3 Verify utility balance between survival needs and mate search

### Phase 3: Reproduction & Mutation Resolution

#### Automated

- [x] 3.1 Unit tests for trait crossover, mutation deltas, and parental energy deduction
- [x] 3.2 Unit tests for mutual pairing resolution and 5,000 population cap enforcement

#### Manual

- [x] 3.3 Verify trait distribution drift and diversity across generations

### Phase 4: Simulation Engine & Lifecycle Pipeline

#### Automated

- [x] 4.1 Unit tests for multi-generation reproduction in SimulationEngine tick loop
- [x] 4.2 Unit tests for speed and size movement/metabolism trade-offs

#### Manual

- [x] 4.3 Verify stable multi-generation population cycles without sudden collapse

### Phase 5: API & SignalR Transport

#### Automated

- [x] 5.1 API tests pass with expanded Genome DTOs and OrganismBorn event serialization

#### Manual

- [x] 5.2 SignalR hub streams mate action states and birth events to clients

### Phase 6: Frontend Visuals & Telemetry

#### Automated

- [x] 6.1 Frontend builds cleanly and passes oxlint

#### Manual

- [x] 6.2 Visual observation of organism size scaling and birth particle rings in PixiJS

### Phase 7: Determinism Goldens, Regression, & Benchmarks

#### Automated

- [x] 7.1 Contract V4 golden fingerprint tests pass
- [x] 7.2 Regression tests pass for V1, V2, and V3 golden baselines
- [x] 7.3 Population 5000 V4 benchmark compiles cleanly

#### Manual

- [x] 7.4 Confirm simulation throughput meets >200 ticks/sec headless with 5,000 organisms
