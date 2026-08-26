---
project: "Wild Seed"
version: 1
status: draft
created: 2026-08-26
context_type: greenfield
product_type: web-app
target_scale:
  users: small
  qps: low
  data_volume: small
timeline_budget:
  mvp_weeks: 4
  hard_deadline: null
  after_hours_only: false
---

# Wild Seed Product Requirements Document

## Vision & Problem Statement

Wild Seed is an interactive 2D ecosystem simulation intended to feel like a living, autonomous world. Its defining experience is observing behavior and evolutionary direction emerge from interactions between simple rules rather than from scripted scenarios.

The project is a private hobby experiment and a curiosity on the boundary between simulation and game. It is being created because its creator considers the subject intrinsically interesting, while also serving as a polished demonstration of programming ability; it is not intended to solve an external user's existing problem or replace a current workaround.

## User & Persona

The primary persona is the creator of Wild Seed, pursuing a personally interesting programming experiment that also expands their portfolio. They interact with the product as an observer and experimenter: running simulations, inspecting organisms and population data, and modifying environmental conditions.

## Success Criteria

### Primary

- A visitor can configure and generate a world, run it across many generations, control simulation speed, observe dynamic population responses and inherited genome changes, and inspect the current state and genome of an organism.
- The same seed and configuration produce the same simulation result.

### Secondary

- A visitor can perform a limited set of God Mode interventions and observe the ecosystem's response.

### Guardrails

- A seed and configuration always produce the same simulation outcome.
- Simulation execution remains independent of rendering, including when running faster than realtime.
- Every capability admitted into the MVP is presented in a cohesive, project-specific final visual style; the MVP does not use default browser dialogs, unstyled placeholder windows, or knowingly temporary UI in place of the intended experience.

## User Stories

### US-01: Observe an evolving ecosystem

- **Given** a visitor has opened the public demo and configured a world
- **When** they generate the world and run the simulation across multiple generations
- **Then** they can observe resource use, predator-prey dynamics, reproduction, death, population changes, and inherited genome changes

#### Acceptance Criteria

- The visitor can pause and accelerate the simulation up to an unrestricted `MAX` mode.
- Population and average genome-trait statistics change as the simulation progresses.
- Selecting an organism reveals its current state, genome, and family relationships.
- Re-running the same seed and configuration yields the same outcome.

## Functional Requirements

### World and experiment

- FR-001: Visitor can configure the world seed, map size, initial populations, vegetation and water density, mutation probability, and mutation strength. Priority: must-have
  > Socrates: Counter-argument considered: exposing many parameters may overwhelm the first experience; presets could reduce complexity. Resolution: kept as written.
- FR-002: Visitor can generate a procedural world containing land, water, and renewable vegetation. Priority: must-have
  > Socrates: Counter-argument considered: procedural generation is not strictly required to prove emergence; a fixed map could suffice. Resolution: kept as written.
- FR-003: Visitor can start, pause, and change simulation speed between realtime multipliers and an unrestricted `MAX` mode. Priority: must-have
  > Socrates: Counter-argument considered: `MAX` may introduce premature optimization work before correctness at x10 is established. Resolution: kept as written.

### Autonomous ecosystem

- FR-004: Simulation can make organisms autonomously respond to hunger, thirst, and energy. Priority: must-have
  > Socrates: Counter-argument considered: modeling all three needs at once increases the number of difficult interactions; hunger and energy alone could prove the loop. Resolution: kept as written.
- FR-005: Simulation can limit organisms to local perception and let them explore, move toward perceived needs, and flee perceived threats. Priority: must-have
  > Socrates: Counter-argument considered: perception, exploration, and fleeing may create a costly navigation problem; simpler directional movement could suffice. Resolution: kept as written.
- FR-006: Simulation can let herbivores consume vegetation and carnivores detect, chase, attack, kill, and consume prey while prey can flee. Priority: must-have
  > Socrates: Counter-argument considered: a full multi-step hunt may consume a disproportionate share of the MVP. Resolution: kept as written.
- FR-007: Simulation can let eligible organisms reproduce and produce offspring that inherit combined parental genomes with configurable mutations and trait trade-offs. Priority: must-have
  > Socrates: Counter-argument considered: two-parent reproduction adds mate-selection complexity; simpler inheritance could demonstrate evolution. Resolution: kept as written.
- FR-008: Simulation can age organisms and kill them through starvation, dehydration, combat, predation, or old age. Priority: must-have
  > Socrates: Counter-argument considered: five named causes may not yield five meaningfully different behaviors. Resolution: kept as written.

### Observation and experimentation

- FR-009: Visitor can observe a rendered map with the current environment and organisms. Priority: must-have
  > Socrates: Counter-argument considered: rendering every organism may cap simulation scale; sampled rendering at high speed could preserve observability. Resolution: kept as written.
- FR-010: Visitor can observe population, birth, death, lifespan, and average genome-trait statistics over simulation time. Priority: must-have
  > Socrates: Counter-argument considered: broad analytics may distract from delivering the simulation engine. Resolution: kept as written.
- FR-011: Visitor can inspect a selected organism's state, current action, genome, parents, and children. Priority: must-have
  > Socrates: Counter-argument considered: retaining family relationships adds storage and lifecycle complexity; current state and genome alone could suffice. Resolution: kept as written.
- FR-012: Visitor can perform a limited set of God Mode interventions and observe the ecosystem's response. Priority: nice-to-have
  > Socrates: Counter-argument considered: God Mode does not itself prove autonomous emergence and could distract from the core loop. Resolution: kept as a removable nice-to-have.

## Non-Functional Requirements

- The simulation stably supports at least 5,000 simultaneously active organisms on a typical modern computer.
- With 5,000 active organisms and rendering disabled, `MAX` mode advances simulation time at least 20 times faster than realtime.
- The same seed and configuration always produce the same simulation outcome.
- Simulation execution remains independent of rendering; disabling or reducing rendering does not change simulation results.
- All MVP interactions use a cohesive, project-specific visual design with no default browser dialogs or unstyled placeholder windows standing in for the intended UI.

## Business Logic

In every deterministic tick, an organism selects the highest-scoring available action based on its needs, local perception, and genome, while the consequences of those decisions affect survival, reproduction, and population evolution.

The rule consumes the organism's current needs and energy, locally perceived resources and organisms, and inherited genome traits. Candidate behaviors include exploration, movement toward food or water, eating, drinking, fleeing, hunting, attacking, mating, and resting.

Its output is the organism's selected action and the resulting state change in the world. The visitor encounters the rule through visible organism behavior, population cycles, inherited trait changes, local extinction, statistics, and organism inspection rather than through a scripted scenario.

## Access Control

Public demo with no authentication and no role separation. Every visitor can start and interact with their own simulation.

## Non-Goals

- No weather, seasons, or diseases in the MVP; they are environmental expansions beyond the core ecosystem proof.
- No herd or territory behavior in the MVP; social behavior is deferred.
- No automatic speciation, color genetics, or evolution tree in the MVP; advanced evolutionary visualization and classification are deferred.
- No saved worlds, replay, or time machine in the MVP; persistence and historical state navigation are deferred.
- No accounts or multiplayer; the MVP remains a public demo where each visitor runs an independent world.
- No complex combat system; MVP combat remains limited to the simple rules described in the brief.
- No LLM-based or separately scripted AI; behavior emerges from deterministic simulation rules.
- Limited God Mode remains optional and is not required for the core MVP to be considered successful.

## Open Questions

No unresolved product-shaping questions.
