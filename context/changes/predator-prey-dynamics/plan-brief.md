# Predator-Prey Dynamics — Plan Brief

> Full plan: `context/changes/predator-prey-dynamics/plan.md`

## What & Why

Implement roadmap slice S-03: autonomous predator-prey interactions. Carnivores autonomously detect, chase (Hunt), attack, and consume Herbivores to satisfy hunger, while Herbivores perceive approaching predators and flee (Flee) in the opposite direction. Sprint dynamics ($1.5\times$ speed at $2\times$ energy cost) introduce dramatic chase sequences, establishing the core ecological tension of Wild Seed.

## Starting Point

Slice S-02 established the single-organism deterministic survival loop (grazing, drinking, resting, exploring) and basic starvation for carnivores. Organisms do not yet perceive other organisms, and the engine lacks a spatial index and combat/predation resolution.

## Desired End State

Visitors can observe active predator-prey dynamics on the canvas: carnivores chasing prey, prey fleeing with sprint boosts, attacks resulting in instant kills that feed predators, and rich visual distinction between predator and prey behaviors and death effects, all backed by zero-allocation spatial indexing and deterministic Contract V3 goldens.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
|---|---|---|
| Attack & Consumption | Instant kill on tile contact | Simple, deterministic, and fits PRD §Non-Goals (no complex HP/combat system). |
| Flee Direction | Vector opposite to closest predator | Computationally $O(1)$ on nearest threat and produces natural evasive maneuvers. |
| Target Selection | Dynamic closest prey re-evaluated each tick | Deterministic, requires no mutable target lock state, and reacts quickly to field changes. |
| Spatial Perception | Uniform Spatial Grid (`SpatialGrid`) | Provides $O(1)$ neighborhood search with zero heap allocations per tick for 5,000 agents. |
| Flee Priority | High priority over food/water unless agonal | Realistic self-preservation instinct preventing prey from grazing while being hunted. |
| Sprint Dynamics | $1.5\times$ speed at $2\times$ energy cost | Introduces realistic chase mechanics where stamina matters. |
| Contract Versioning | `SimulationContract.Version3` | Maintains backwards verification for V1 and V2 golden baselines. |
| Frontend Rendering | Distinct tints, sprint trails, predation bursts | Delivers high visual clarity and exciting observation on PixiJS canvas. |

## Scope

**In scope:**
- `SpatialGrid` zero-allocation spatial partitioner.
- Organism mutual perception (danger detection for prey, hunting detection for predators).
- Actions: `Hunt`, `Attack`, `Flee`.
- Sprinting speed ($1.5\times$) and energy drain ($2\times$).
- Predation resolution and `DeathCause.Predation` events.
- `SimulationContract.Version3` and golden fingerprint test suite.
- PixiJS rendering updates for predators/prey, sprint visual indicators, and kill effects.
- 5,000-organism V3 benchmark.

**Out of scope:**
- Mating, reproduction, genomes, and mutation (S-04).
- Historical charts and timeline graphs (S-05).
- Organism inspector window (S-06).
- God Mode interventions (S-07).
- Multi-tick combat or health points (HP).
- Meat/carcass resources on tiles.

## Architecture / Approach

```
SimulationEngine.AdvanceTick()
  │
  ├── 1. SpatialGrid.Rebuild(Organisms)
  ├── 2. PerceptionService.Perceive(World, Organisms, SpatialGrid)
  ├── 3. ActionScorer.Score(Needs, Threats, Prey) -> Intents (Hunt/Attack/Flee)
  ├── 4. MovementResolver.Move(SprintModifier, FleeVector)
  ├── 5. PredationResolver.Resolve(Attacks) -> Instant Kills & Predator Feeding
  ├── 6. VegetationResolver.Resolve(Graze)
  ├── 7. DeathResolver.Resolve(Starvation, Dehydration, OldAge, Predation)
  └── 8. State Snapshot & SignalR Mailbox Dispatch
```

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. Domain & Contracts | Enums, `SurvivalRulesV3`, `SpatialGrid`, `SimulationContract.Version3` | SpatialGrid boundary/indexing bugs |
| 2. Perception & Scoring | Spatial query integration, `Hunt`/`Attack`/`Flee` utility scoring | Threat evasion vs thirst utility imbalance |
| 3. Engine & Resolution | Sprint movement, `PredationResolver`, kill & feeding execution | Race conditions between simultaneous attacks on same prey |
| 4. API & Transport | DTO mapping for new actions and `Predation` death causes | Missing enum serialization |
| 5. Frontend Visuals | PixiJS organism styling, sprint effects, predation burst particles | Rendering stutter or particle leak |
| 6. Determinism & Goldens | Contract V3 goldens, multi-tick predator-prey determinism tests | Deterministic random divergence across platforms |
| 7. Benchmarking | `SurvivalLoopPopulation5000V3Benchmark` | SpatialGrid query overhead under high density |

**Prerequisites:** S-02 completed.
**Estimated effort:** ~1-2 implementation sessions across 7 phases.

## Open Risks & Assumptions

- **High organism density**: If 1,000 organisms cluster in one cell, spatial bucket query performance could degrade. *Mitigation: cell size tuned to average perception radius.*
- **Prey cornering**: Prey fleeing straight from a predator might hit map boundaries or deep water. *Mitigation: existing boundary clamping and water rejection keep movement valid.*

## Success Criteria (Summary)

- Carnivores visibly hunt and consume herbivores, preventing starvation in prey-rich worlds.
- Herbivores actively flee approaching predators with sprint boosts.
- Contract V3 golden checkpoints pass deterministically.
- Headless 5,000-organism benchmark achieves >200 ticks/sec.
