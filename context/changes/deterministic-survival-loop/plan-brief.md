# Deterministic Survival Loop — Plan Brief

> Full plan: `context/changes/deterministic-survival-loop/plan.md`

## What & Why

Implement roadmap slice S-02 so a visitor can start, pause, accelerate, and observe a deterministic needs-driven survival loop. This is the first production simulation engine: organisms respond to hunger, thirst, and energy through local perception and simple autonomous actions while the same seed and command schedule always produce the same outcome.

## Starting Point

Wild Seed currently produces a deterministic but static `WorldMap` and renders a one-shot REST snapshot. The repository has canonical fingerprint infrastructure, synthetic determinism fixtures, and a provisional 5,000-agent benchmark, but no production tick engine, mutable runtime state, SignalR session host, live controls, or renderer update path.

## Desired End State

A newly generated world opens paused. The visitor can run it at `1×`, `5×`, `20×`, or unrestricted `MAX`, observe interpolated organism movement and compact action/death telemetry, pause without state drift, and reconnect within 60 seconds to the same paused session. Herbivores can find and consume vegetation, drink from shore, rest, age, and die; carnivores follow the same metabolism but starve until hunting is introduced in S-03.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| S-02 food loop | Herbivores eat renewable vegetation and drink from shore | S-02 must demonstrate a closed survival loop rather than movement toward unusable resources. | Plan |
| Carnivores before S-03 | Normal metabolism with no hunting or plant consumption | Avoids temporary food sources or exceptions while preserving the S-01 configuration. | Plan |
| Initial state | Paused | Preserves the observable tick-zero state and gives the visitor explicit experiment control. | Plan |
| Speed controls | `1×`, `5×`, `20×`, `MAX` | Covers observation, the performance target, and unrestricted execution without clutter. | Plan |
| MAX observation | Latest snapshot at approximately 15 Hz | Keeps the world observable without allowing rendering or slow clients to block simulation. | Plan |
| Resource conflicts | Proportional integer sharing with stable-ID remainder ordering | Removes iteration-order bias while conserving the exact available resource. | Plan |
| Death presentation | End-of-tick removal plus frontend-only visual effect | Makes death visible without introducing corpse state before S-03. | Plan |
| Compatibility | Preserve static/probe `v1`; production live state uses `v2` | Keeps reviewed history intact and clearly versions the first production rules. | Plan |
| Disconnect | Pause immediately and retain detached session for 60 seconds | Prevents unnoticed progress and limits orphaned CPU usage. | Plan |
| Performance gate | Real benchmark required; 200 ticks/s is reported but non-blocking | Produces honest production evidence without turning early optimization into a release blocker. | Plan |

## Scope

**In scope:**

- Versioned runtime state, deterministic random policy, needs, age, actions, death causes, and renewable vegetation.
- Local perception, exploration, movement, resting, shoreline drinking, herbivore eating, proportional resource sharing, and death.
- Runtime `v2` fingerprints and golden checkpoints independent of observation and collection ordering.
- In-memory API sessions, SignalR commands, sampled latest-only snapshots, disconnect pause, and 60-second retention.
- React controls, compact survival telemetry, reconnect UX, interpolated PixiJS organism updates, and death effects.
- A separate real 5,000-organism survival-loop benchmark with recorded evidence.

**Out of scope:**

- Hunting, fleeing, attacking, predation, or corpses as resources; these belong to S-03.
- Reproduction, inheritance, mutation execution, family relationships, statistics history, charts, or organism inspection.
- Persistence, replay, authentication, multi-user session sharing, MessagePack, or horizontal API scaling.
- A hard pass/fail requirement for the 200-ticks/s target in this slice.

## Architecture / Approach

`WorldMap` remains the static generated input. `WildSeed.Simulation` owns a mutable, headless runtime state and advances it through a two-phase deterministic tick: perceive/score intents from one read state, then resolve movement and shared resources before lifecycle removal. ASP.NET Core owns session pacing and SignalR sampling; React owns controls and status; PixiJS consumes dynamic snapshots directly and interpolates them without feeding visual state back into the engine.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Compatibility and v2 foundations | Preserved v1 history, explicit v2 identity, numeric and random policies | Accidentally changing reviewed v1 fingerprints. |
| 2. Domain and runtime state | Needs, resources, organism runtime state, and engine-owned storage | Mixing hot mutable state with static rendering/domain snapshots. |
| 3. Survival mechanics | Deterministic perception, scoring, movement, eating, drinking, and sharing | Hidden order bias or resource rounding drift. |
| 4. Lifecycle and determinism proof | Aging, deaths, events, runtime fingerprints, and v2 goldens | Snapshot or event observation consuming state. |
| 5. API sessions and SignalR | Commands, pacing, latest-only streaming, pause/reconnect lifecycle | Disconnect races or slow-client backpressure. |
| 6. Realtime frontend | Controls, telemetry, reconnect UX, interpolation, and death effects | Rebuilding terrain or resetting the camera on every sample. |
| 7. Benchmark and final verification | Production 5,000-organism evidence and end-to-end validation | Reporting an informational miss as if the NFR passed. |

**Prerequisites:** S-01 configurable procedural world and F-01 determinism/performance contract are implemented.

**Estimated effort:** Approximately 5–7 focused implementation sessions across seven phases.

## Open Risks & Assumptions

- Numeric rates in `SurvivalRulesV2` need one reviewed calibration pass; changing them after goldens are accepted requires a new contract version or deliberate golden lifecycle decision.
- Carnivores are expected to die from starvation in S-02 because their food action intentionally arrives in S-03.
- In-memory sessions do not survive an API restart; the frontend must report expiration and require regeneration.
- A result below 200 ticks/s is acceptable for S-02 only when the measured gap and follow-up are recorded explicitly.

## Success Criteria (Summary)

- The same seed, configuration, and command schedule produce identical v2 fingerprints at agreed checkpoints regardless of snapshot cadence.
- Visitors can control a paused/generated simulation at all four speeds, observe the survival loop, and reconnect to the same paused state within 60 seconds.
- The live renderer preserves camera state and stays responsive in `MAX`, while a real 5,000-organism benchmark records honest throughput evidence.
