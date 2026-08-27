# Deterministic Survival Loop Implementation Plan

## Overview

Implement roadmap slice S-02: the first production, deterministic simulation loop and its complete observation path. Organisms gain needs, local perception, movement, resource interaction, aging, and death; the API hosts independently paced in-memory sessions; and the frontend exposes paused/realtime/MAX controls with sampled, interpolated rendering.

## Current State Analysis

The current backend stops after deterministic procedural generation. `WorldMap` contains immutable terrain and a read-only list of immutable organisms, while `Organism` contains only identity, species, speed, position, and `IsAlive`. `WorldFingerprint` always writes tick zero and does not include runtime resources, needs, actions, age, or random state.

F-01 established a 100 ms logical tick, canonical SHA-256 fingerprints, reviewed v1 goldens, observation-cadence tests, and a provisional synthetic 5,000-agent benchmark. Those fixtures prove the verification mechanism but do not execute production ecology. The current production RNG wraps `System.Random`, so it is unsuitable as a versioned runtime random contract whose call count may change as systems evolve.

The API exposes only `POST /api/world/generate`; it has no SignalR registration, session state, runner, or pacing policy. The frontend performs one REST request and imperatively calls `WorldRenderer.renderWorld`. That renderer destroys and rebuilds terrain and organism graphics and resets the camera on every call, which cannot be used for 10–15 live updates per second.

## Desired End State

After this plan:

- A generated world creates one opaque, in-memory simulation session in a paused tick-zero state.
- Simulation advances only through explicit logical ticks and remains independent of wall-clock pacing, snapshots, SignalR, rendering, and interpolation.
- Herbivores autonomously explore, seek vegetation and shoreline water, eat, drink, rest, age, and die according to deterministic utility scoring.
- Carnivores share the same thirst, energy, movement, rest, aging, and death rules but have no food action and eventually starve; hunting remains S-03.
- Scarce vegetation is divided proportionally among simultaneous consumers without collection-order bias.
- Runtime state has a complete v2 canonical fingerprint and reviewed golden checkpoints while all existing v1 fixtures and static world fingerprints remain unchanged.
- The visitor can start, pause, and select `1×`, `5×`, `20×`, or `MAX`; MAX runs without realtime synchronization.
- The API publishes only the newest dynamic state at approximately 15 Hz and cannot block the engine on a slow client.
- Disconnect pauses the session and retains it for 60 seconds; reconnect restores the same session in a paused state and requires explicit resume.
- PixiJS renders static terrain once, interpolates dynamic positions, preserves pan/zoom, and displays short frontend-only death effects.
- A real 5,000-organism benchmark is executed and documented; comparison to 200 ticks/s is informational for this slice.

### Key Discoveries

- The production map has no controlled runtime mutation boundary (`Backend/src/WildSeed.Domain/World/WorldMap.cs:6`) and organisms have no needs or lifecycle state (`Backend/src/WildSeed.Domain/Organisms/Organism.cs:3`).
- Static fingerprinting hardcodes tick zero (`Backend/src/WildSeed.Simulation/WorldGeneration/WorldFingerprint.cs:13`) while canonical encoding already supports an explicit contract version and tick (`Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs:23`).
- Existing v1 fixtures default to the current global contract version, so they must be pinned to v1 before v2 becomes current (`Backend/tests/WildSeed.Simulation.Tests/Fixtures/DeterministicProbe.cs:14`).
- Existing determinism tests already define the required patterns for independent runs, golden checkpoints, observation-cadence independence, side-effect-free reads, and first-divergence diagnostics (`Backend/tests/WildSeed.Simulation.Tests/Determinism/DeterminismContractTests.cs:9`).
- The synthetic benchmark demonstrates preallocated read/write buffers and a uniform spatial grid but excludes real needs, terrain, scoring, resources, and death (`Backend/benchmarks/WildSeed.Simulation.Benchmarks/Workloads/SyntheticPopulation5000V1Scenario.cs:5`).
- The API currently registers only world generation and OpenAPI (`Backend/src/WildSeed.Api/Program.cs:4`), and the frontend has no SignalR dependency (`Frontend/package.json:12`).
- The renderer rebuilds static terrain and resets the camera on every world update (`Frontend/src/rendering/WorldRenderer.ts:74`), so live state requires a separate dynamic update path.

## What We're NOT Doing

- No predator detection, fleeing, hunting, attacking, combat, predation, or carcass resources; those belong to S-03.
- No reproduction, genome expansion beyond existing speed, inheritance, mutation execution, or family relationships; those belong to S-04 and S-06.
- No historical statistics, charts, event timeline, average-genome tracking, or persisted event log; those belong to S-05.
- No organism selection or detailed inspector; compact aggregate action and death telemetry is sufficient for S-02.
- No saved simulations, replay, accounts, shared/multiplayer sessions, database, distributed session store, or multi-node coordination.
- No arbitrary speed multiplier, `100×` guarantee, MessagePack, delta-compression protocol, or frontend test-runner introduction.
- No change to the immutable `SyntheticPopulation5000V1` benchmark.
- No hard build or xUnit failure when the real production benchmark reports below 200 ticks/s.

## Implementation Approach

Keep static generation and mutable simulation state separate. Domain owns small value types and invariants for needs, actions, death causes, and resources. Simulation creates engine-owned runtime storage from `WorldMap`, uses stable organism ordering and reusable buffers, and advances state through a two-phase tick. All agents decide from the same read state; shared interactions are grouped and resolved before commits; deaths are evaluated once and removed at the end of the tick.

Use integer units for accumulating needs and vegetation so conservation and proportional sharing are exact. Runtime exploration uses a counter-based deterministic random function keyed by seed, logical epoch, organism ID, and purpose channel rather than a shared consumed stream. Existing `SimulationRandom` remains generation-v1 behavior.

Preserve compatibility by explicitly naming v1 and pinning all old fixtures and `WorldFingerprint` to it. Introduce a separate runtime state fingerprint using contract v2. A change to simulation rules, numeric semantics, deterministic random mapping, phase ordering, or v2 canonical fields after goldens are accepted is outcome-affecting and must follow the contract-version lifecycle.

The API is the composition root and wall-clock adapter. It creates bearer-token sessions, serializes commands and ticks at a per-session execution boundary, and runs pacing in a hosted service. Static terrain crosses REST once; dynamic snapshots and best-effort death events cross a typed SignalR hub. A capacity-one latest-value mailbox prevents slow clients from backpressuring simulation.

The frontend keeps lifecycle and aggregate telemetry in React, but sends high-frequency organism snapshots directly from the transport adapter to PixiJS. The renderer owns previous/next sampled positions and interpolates them in its ticker. These positions are visual only and never return to the server.

## Critical Implementation Details

### Timing & lifecycle

The logical engine tick remains 100 ms. API pacing maps `1×`, `5×`, and `20×` to 10, 50, and 200 ticks per wall-clock second; `MAX` advances fixed-size batches without realtime delay and yields between batches so pause, disconnect, cancellation, and other sessions remain responsive. Pausing clears accumulated wall-clock debt, so resume never catches up time spent paused.

Disconnect must be serialized against the session tick boundary. A disconnect pauses only when its connection ID is still the current owner, preventing a stale disconnect callback from pausing a newly reattached owner. Reconnect always returns the current authoritative state as paused and never resumes implicitly.

### State sequencing

Every tick applies metabolism and aging, builds the read view, scores exactly one action per organism, records intents, resolves movement and resource claims, applies action effects, determines one death cause, removes deaths, and publishes an immutable result. Movement toward a resource and consuming it are separate actions on separate ticks. An organism at a critical need may survive the tick if its already-selected drink/eat action resolves before end-of-tick death evaluation.

### Performance constraints

Local resource perception scans a bounded tile window; organism perception uses a uniform spatial index rebuilt in stable ID order. Hot tick execution avoids snapshot mapping, hashing, serialization, SignalR sends, rendering work, and unbounded queues. The production benchmark times only engine ticks after setup.

### User experience spec

A successful generate replaces the old session only after the new paused session and tick-zero payload exist. During reconnect, controls are disabled and the current canvas remains visible. An expired or server-lost session remains visible but inactive and clearly asks the visitor to generate a new world.

## Phase 1: Preserve v1 and Establish Runtime Contract v2

### Overview

Protect reviewed compatibility history before defining the numeric and random contracts on which production simulation results will depend.

### Changes Required

#### 1. Explicit simulation contract identities

**File**: `Backend/src/WildSeed.Simulation/Contracts/SimulationContract.cs`

**Intent**: Separate the preserved foundation/static contract from the first production runtime contract without silently reinterpreting v1.

**Contract**: Expose explicit `Version1` and `CurrentVersion` identities with current runtime version 2 while retaining the 100 ms tick and performance arithmetic.

#### 2. Pin legacy canonical consumers

**Files**:

- `Backend/src/WildSeed.Simulation/WorldGeneration/WorldFingerprint.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Fixtures/DeterministicProbe.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Determinism/ContractV1GoldenFingerprints.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Determinism/DeterminismContractTests.cs`

**Intent**: Keep existing static-world and synthetic-probe bytes and golden hashes unchanged when v2 becomes current.

**Contract**: Every v1 writer and fingerprint call passes version 1 explicitly; existing four v1 digests remain byte-for-byte unchanged and continue to pass.

#### 3. Remove ambiguous version defaults

**Files**:

- `Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs`
- `Backend/src/WildSeed.Simulation/Determinism/StateFingerprint.cs`

**Intent**: Prevent new canonical consumers from accidentally inheriting a changing global version.

**Contract**: Production call sites provide contract identity explicitly wherever a fingerprint or canonical header is created; parsing and textual `vN:<digest>` behavior remains compatible.

#### 4. Runtime rules and deterministic random mapping

**Files**:

- `Backend/src/WildSeed.Simulation/Contracts/SurvivalRulesV2.cs`
- `Backend/src/WildSeed.Simulation/Random/DeterministicRandom.cs`
- `Backend/src/WildSeed.Simulation/Random/RandomChannel.cs`

**Intent**: Centralize outcome-affecting rates and define random values as pure functions of canonical identifiers rather than shared call order.

**Contract**: Rules define integer scales, metabolism, action effects, thresholds, perception radius, movement/exploration cadence, resource conversion/regrowth, and death precedence. Random mapping is keyed by world seed, logical tick or epoch, stable organism ID, and an explicit purpose channel.

### Success Criteria

#### Automated Verification

- Contract and canonical tests pass with current runtime version 2 and explicit legacy version 1.
- Existing v1 fingerprints at ticks 0, 10, 100, and 1,000 remain unchanged.
- Existing world-generation endpoint tests continue to return a v1 static fingerprint.
- Deterministic random tests prove repeatability, channel separation, organism-order independence, and no mutable random state.
- Architecture tests remain green.

#### Manual Verification

- Review confirms every legacy canonical consumer is explicitly pinned before v2 is used.
- Review accepts the initial `SurvivalRulesV2` values and identifies them as contract-versioned behavior.

**Implementation Note**: Pause after this phase for human review of the numeric and random contracts; later golden vectors make changes intentionally expensive.

---

## Phase 2: Introduce Domain Primitives and Engine-Owned Runtime State

### Overview

Add the invariant-bearing concepts required by survival while preserving `WorldMap` and its S-01 DTO as a static generation result.

### Changes Required

#### 1. Needs, actions, and death causes

**Files**:

- `Backend/src/WildSeed.Domain/Organisms/OrganismNeeds.cs`
- `Backend/src/WildSeed.Domain/Organisms/OrganismAction.cs`
- `Backend/src/WildSeed.Domain/Organisms/DeathCause.cs`

**Intent**: Give the domain explicit bounded values and vocabulary for hunger, thirst, energy, behavior, and lifecycle outcomes.

**Contract**: Needs use bounded integer units and invariant-preserving transitions. S-02 actions are Explore, SeekFood, Eat, SeekWater, Drink, and Rest; death causes are Starvation, Dehydration, and OldAge.

#### 2. Renewable vegetation value

**File**: `Backend/src/WildSeed.Domain/Terrain/VegetationResource.cs`

**Intent**: Represent exact consumable and regenerating biomass independently from immutable visual terrain density.

**Contract**: Current and capacity units are bounded integers; consume and regrow operations conserve units and never exceed `[0, capacity]`.

#### 3. Runtime organism and simulation state

**Files**:

- `Backend/src/WildSeed.Simulation/Engine/OrganismState.cs`
- `Backend/src/WildSeed.Simulation/Engine/SimulationState.cs`
- `Backend/src/WildSeed.Simulation/Engine/SimulationStateFactory.cs`

**Intent**: Own mutable hot-loop state in Simulation without making static `Organism`, `Tile`, or rendering DTOs mutable.

**Contract**: State contains seed/configuration, logical tick, stable-ID ordered living organisms, flat vegetation storage indexed by tile coordinate, reusable per-tick buffers, and the static map needed for terrain queries. Tick-zero conversion from `WorldMap` is deterministic and applies the reviewed density-to-capacity rounding policy.

#### 4. Read-only snapshot and tick result contracts

**Files**:

- `Backend/src/WildSeed.Simulation/Engine/SimulationSnapshot.cs`
- `Backend/src/WildSeed.Simulation/Engine/SimulationTickResult.cs`
- `Backend/src/WildSeed.Simulation/Events/SimulationEvent.cs`
- `Backend/src/WildSeed.Simulation/Events/OrganismDied.cs`

**Intent**: Expose observation and per-tick outcomes without leaking engine-owned collections or allowing snapshots to mutate state.

**Contract**: Snapshots are immutable projections. Death events contain tick, stable organism ID, species, cause, and final position and are stably ordered by ID.

### Success Criteria

#### Automated Verification

- Domain tests cover every needs/resource bound and transition.
- Tick-zero state creation is identical across independent worlds with the same seed and configuration.
- Runtime collections cannot be mutated through snapshot contracts.
- Existing S-01 world generation and API tests remain green.
- Full backend solution builds and architecture tests pass.

#### Manual Verification

- Review confirms static terrain and initial organisms remain compatible with the current one-shot renderer contract.
- Review confirms Domain contains invariants only and no tick orchestration, framework, transport, or wall-clock dependency.

---

## Phase 3: Implement the Deterministic Survival Tick

### Overview

Build the pure headless tick pipeline for local perception, utility action selection, movement, resource use, rest, and exact simultaneous conflict resolution.

### Changes Required

#### 1. Spatial and resource perception

**Files**:

- `Backend/src/WildSeed.Simulation/Spatial/UniformSpatialIndex.cs`
- `Backend/src/WildSeed.Simulation/Perception/PerceptionService.cs`
- `Backend/src/WildSeed.Simulation/Perception/PerceptionResult.cs`

**Intent**: Restrict organisms to local knowledge with bounded work suitable for 5,000 active organisms and later S-03 neighbor queries.

**Contract**: Tile scanning uses a bounded radius and stable coordinate ordering. The spatial index rebuilds once per tick in stable organism-ID order and returns deterministically ordered local results without O(N²) global scans.

#### 2. Utility scoring and action intents

**Files**:

- `Backend/src/WildSeed.Simulation/Behavior/ActionScorer.cs`
- `Backend/src/WildSeed.Simulation/Behavior/ActionIntent.cs`

**Intent**: Select exactly one available action from needs, local perception, species, and runtime rules.

**Contract**: Highest score wins with an explicit action-priority tie-break. Herbivores can seek/eat vegetation; carnivores cannot select food actions in S-02. Both species can seek/drink shoreline water, rest, and explore.

#### 3. Movement and shoreline resolution

**Files**:

- `Backend/src/WildSeed.Simulation/Movement/MovementResolver.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Movement/ShoreMovementTests.cs`

**Intent**: Move toward perceived targets or stable exploration headings while keeping organisms on land.

**Contract**: Movement derives distance from speed and the 100 ms logical tick, clamps to the world, uses deterministic obstacle alternatives, never enters shallow or deep water, and treats a cardinally adjacent land tile as a valid drinking position.

#### 4. Exact shared-resource arbitration

**Files**:

- `Backend/src/WildSeed.Simulation/Resources/VegetationResolver.cs`
- `Backend/src/WildSeed.Simulation/Resources/ProportionalResourceAllocator.cs`

**Intent**: Resolve all same-tick vegetation claims without giving priority to list iteration order.

**Contract**: Claims are grouped by tile and receive proportional integer grants. Any remainder is distributed one unit at a time by stable organism ID; total grants equal at most the available resource, duplicate claim IDs are rejected, and commits occur only after all grants are known.

#### 5. Simulation engine orchestration

**Files**:

- `Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Engine/SimulationEngineTests.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Behavior/HerbivoreSurvivalTests.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Behavior/CarnivoreMetabolismTests.cs`

**Intent**: Advance one or many logical ticks through one explicit, deterministic phase order.

**Contract**: The engine performs regeneration, metabolism/aging, read-view/index construction, scoring, intent creation, movement, drinking, vegetation arbitration, rest effects, and lifecycle evaluation in the documented order. `AdvanceTicks(N)` is outcome-equivalent to calling `AdvanceTick` N times.

### Success Criteria

#### Automated Verification

- Action tests prove highest-score selection, fixed ties, unavailable-action exclusion, and candidate-order independence.
- Perception and spatial-index tests cover radius boundaries, map edges, shoreline targets, and insertion-order independence.
- Herbivores can complete explore/seek/eat and seek/drink loops and recover the expected need units.
- Carnivores move, drink, and rest but never consume vegetation or select food actions.
- Proportional sharing conserves exact resource units and produces identical grants under every claimant permutation.
- Batch and single-tick execution produce identical state.
- Focused Domain and Simulation test suites pass.

#### Manual Verification

- Review of a small fixed scenario confirms movement and consumption are separate actions on successive ticks.
- Review confirms every state-affecting collection has a documented stable order or deterministic arbitration rule.

---

## Phase 4: Complete Lifecycle, Events, and v2 Determinism Proof

### Overview

Finish end-of-tick death semantics, make runtime state canonically observable, and lock the first production behavior contract with reviewed v2 checkpoints.

### Changes Required

#### 1. Death resolution and removal

**Files**:

- `Backend/src/WildSeed.Simulation/Lifecycle/DeathResolver.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Lifecycle/DeathResolutionTests.cs`

**Intent**: Produce one stable death outcome after action effects and remove dead organisms only after all tick interactions complete.

**Contract**: Death precedence is dehydration, starvation, then old age. Each death emits exactly one ordered event; dead organisms do not participate in future ticks and no corpse state remains.

#### 2. Runtime canonical fingerprint

**File**: `Backend/src/WildSeed.Simulation/Determinism/SimulationStateFingerprint.cs`

**Intent**: Hash the complete outcome-relevant production state independently from transport DTOs and transient events.

**Contract**: The v2 preimage includes seed/configuration, tick, dimensions, terrain and current/capacity vegetation in coordinate order, then living organisms ordered by unique ID with species, genome, position, age, needs, and current action. Output events and API/session metadata are excluded.

#### 3. v2 golden checkpoints and diagnostic matrix

**Files**:

- `Backend/tests/WildSeed.Simulation.Tests/Determinism/ContractV2GoldenFingerprints.cs`
- `Backend/tests/WildSeed.Simulation.Tests/Determinism/SurvivalLoopDeterminismTests.cs`

**Intent**: Preserve reviewed production outcomes and prove that observation, batching, and storage order cannot change them.

**Contract**: Commit manually reviewed tick 0/10/100/1000 fingerprints for one named world. Tests cover independent runs, different seeds/configurations, list and spatial insertion permutations, one-by-one versus batched ticks, headless/every-tick/sparse snapshots, repeated side-effect-free observations, event equality, pause invariance, and first divergent tick diagnostics.

### Success Criteria

#### Automated Verification

- Death tests prove action-before-death ordering, one stable cause, end-of-tick removal, and ordered events.
- v2 golden fingerprints match at ticks 0, 10, 100, and 1,000 without runtime regeneration.
- Every observation cadence and collection permutation produces the same v2 checkpoints.
- Existing v1 golden fingerprints and static world fingerprints remain unchanged.
- Full Domain, Simulation, and architecture test suites pass.

#### Manual Verification

- Temporarily induced divergence identifies the first differing tick and both fingerprints, then is reverted.
- Human review accepts the v2 canonical field order and committed golden values.

**Implementation Note**: Pause after this phase for explicit approval of the production v2 golden contract before transport work relies on it.

---

## Phase 5: Host Paused Sessions and Sampled SignalR State

### Overview

Turn the pure engine into independently controlled browser sessions without allowing host timing, connection state, or slow clients to enter simulation semantics.

### Changes Required

#### 1. Static and dynamic API contracts

**Files**:

- `Backend/src/WildSeed.Api/Contracts/GenerateWorldResponse.cs`
- `Backend/src/WildSeed.Api/Contracts/StaticWorldResponse.cs`
- `Backend/src/WildSeed.Api/Contracts/SimulationSnapshotResponse.cs`
- `Backend/src/WildSeed.Api/Contracts/SimulationStatusResponse.cs`
- `Backend/src/WildSeed.Api/Contracts/SimulationCommandResult.cs`
- `Backend/src/WildSeed.Api/Contracts/WorldSnapshotResponse.cs`

**Intent**: Send terrain and immutable metadata once while keeping recurring snapshots compact and explicit.

**Contract**: Generation returns an opaque bearer session token, static terrain/organism metadata, and a paused tick-zero v2 snapshot. Dynamic snapshots contain tick, run state, selected speed, fingerprint, population, action counts, death counts/events, and organism ID/position/action; they do not resend tiles or interpolate server-side.

#### 2. In-memory session ownership

**Files**:

- `Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs`
- `Backend/src/WildSeed.Api/SimulationHosting/SimulationSessionManager.cs`
- `Backend/src/WildSeed.Api/SimulationHosting/SimulationHostOptions.cs`
- `Backend/src/WildSeed.Api/Endpoints/WorldEndpoints.cs`

**Intent**: Create one paused, token-protected session per generated world with a serialized command/tick boundary.

**Contract**: Tokens are cryptographically random, memory-only, never logged, and excluded from fingerprints. A session has at most one active owner connection. Regeneration creates the replacement before disposing the old session. Detached sessions pause immediately and expire after 60 seconds measured with injectable `TimeProvider`.

#### 3. Typed SignalR control adapter

**Files**:

- `Backend/src/WildSeed.Api/Hubs/ISimulationClient.cs`
- `Backend/src/WildSeed.Api/Hubs/SimulationHub.cs`
- `Backend/src/WildSeed.Api/Program.cs`

**Intent**: Expose attach, start, pause, and closed-enum speed commands while keeping the transient hub stateless.

**Contract**: Commands are idempotent and return authoritative status or stable error codes. Attach requires the bearer token; reconnect reattaches and returns paused state. A stale disconnect cannot pause a newer owner. Unexpected session faults are isolated, logged, paused, and reported without stopping other sessions.

#### 4. Pacing and latest-only publication

**Files**:

- `Backend/src/WildSeed.Api/SimulationHosting/SimulationRunnerService.cs`
- `Backend/src/WildSeed.Api/SimulationHosting/LatestSnapshotMailbox.cs`
- `Backend/src/WildSeed.Api/appsettings.json`

**Intent**: Map wall-clock speeds to logical tick budgets and publish sampled state without blocking engine progress.

**Contract**: `1×/5×/20×` target 10/50/200 ticks per second; MAX executes bounded batches and yields between them. Publication is capped near 15 distinct snapshots per second and uses capacity-one replace-oldest behavior. Replaced envelopes merge undelivered death events; the latest authoritative live-organism list repairs missed effects.

#### 5. Session and transport tests

**Files**:

- `Backend/tests/WildSeed.Api.Tests/SimulationSessionManagerTests.cs`
- `Backend/tests/WildSeed.Api.Tests/SimulationRunnerServiceTests.cs`
- `Backend/tests/WildSeed.Api.Tests/SampledStatePublisherTests.cs`
- `Backend/tests/WildSeed.Api.Tests/SimulationHubTests.cs`
- `Backend/tests/WildSeed.Api.Tests/WorldEndpointTests.cs`

**Intent**: Verify lifecycle and pacing with fake time/manual ticks rather than flaky elapsed-time assertions.

**Contract**: Tests cover paused creation, one-owner rules, idempotent commands, exact tick budgets, no pause catch-up, bounded MAX batches, latest-only slow-consumer behavior, death-event merging, disconnect races, attach at 59 seconds, expiry at 60 seconds, fault isolation, and identical fingerprints under different publication cadence/backpressure.

### Success Criteria

#### Automated Verification

- Generation returns a token, static world, paused tick-zero state, and v2 runtime fingerprint.
- Session manager and runner tests pass with fake time and no wall-clock assertions.
- Slow publication never blocks ticks and eventually delivers the latest monotonic tick with merged death events.
- Disconnect pauses exactly once; reconnect before expiry restores the same fingerprint and remains paused; expiry rejects attach.
- Different snapshot schedules and SignalR backpressure produce identical engine fingerprints.
- API integration, full backend, and architecture tests pass.

#### Manual Verification

- A hub client can attach, start, switch every speed, pause, disconnect, and reattach to the same paused tick.
- Review confirms SignalR, timers, bearer tokens, connection IDs, and session status never enter canonical simulation state.

---

## Phase 6: Deliver Realtime Controls and Interpolated Rendering

### Overview

Replace the one-shot frontend lifecycle with a polished session controller and a renderer that updates organisms continuously without rebuilding terrain or disturbing the camera.

### Changes Required

#### 1. SignalR dependency and development proxy

**Files**:

- `Frontend/package.json`
- `Frontend/package-lock.json`
- `Frontend/vite.config.ts`

**Intent**: Add the official JavaScript SignalR client and WebSocket-capable development routing.

**Contract**: `/hubs` proxies to the API with WebSocket support; the existing `/api` proxy remains unchanged.

#### 2. Typed realtime transport

**Files**:

- `Frontend/src/transport/SimulationContracts.ts`
- `Frontend/src/transport/SimulationConnection.ts`
- `Frontend/src/transport/WorldApi.ts`

**Intent**: Encapsulate token attach, commands, sampled snapshots, reconnect, expiry, faults, and cleanup outside React components.

**Contract**: Automatic retry ends within the 60-second retention window. Reconnect reattaches explicitly and never resumes. Snapshot callbacks deliver dynamic state directly to the renderer path; lifecycle/status callbacks update React.

#### 3. Simulation controls and compact telemetry

**Files**:

- `Frontend/src/features/world/SimulationControls.tsx`
- `Frontend/src/features/world/SimulationControls.css`
- `Frontend/src/features/world/ConfigPanel.tsx`
- `Frontend/src/features/world/ConfigPanel.css`

**Intent**: Add cohesive start/pause and `1×/5×/20×/MAX` controls plus current tick, population, action counts, and death-cause counts.

**Contract**: A generated world is visibly paused. Controls disable while generating, reconnecting, expired, or faulted. The fingerprint label describes canonical state at the current tick rather than claiming the browser verified multiple runs.

#### 4. Static terrain and dynamic organism layers

**Files**:

- `Frontend/src/rendering/WorldRenderer.ts`
- `Frontend/src/rendering/OrganismLayer.ts`
- `Frontend/src/rendering/CameraController.ts`

**Intent**: Load terrain once and interpolate sampled organism positions in the Pixi ticker while preserving pan and zoom.

**Contract**: Initial load resets the camera exactly once. Dynamic pushes never rebuild terrain or reset camera state. Previous/next snapshots are keyed by stable organism ID; missing IDs disappear authoritatively; new IDs appear without interpolation. A death event may create a short visual-only effect that cannot affect engine state.

#### 5. Application session lifecycle

**Files**:

- `Frontend/src/app/App.tsx`
- `Frontend/src/app/App.css`

**Intent**: Compose generation, connection ownership, controls, aggregate telemetry, renderer callbacks, regeneration, reconnect, and terminal errors.

**Contract**: High-frequency organism arrays do not enter React state. A new successful generation atomically replaces the previous connection/session; failed generation leaves the previous view intact. Expired sessions retain an inactive canvas with a clear regenerate action.

### Success Criteria

#### Automated Verification

- Frontend dependency installation is locked in `package-lock.json`.
- TypeScript/Vite production build passes.
- Oxlint passes with no unused locals, parameters, or switch fallthrough.
- Full backend test suite remains green after frontend contract integration.

#### Manual Verification

- A generated world stays paused until Start and every speed control reports authoritative status.
- MAX remains responsive while UI samples update without queue growth or visible freezing.
- Pan and zoom remain unchanged across live snapshots, pause/resume, and reconnect.
- Action and death telemetry matches visible population changes; death effects are brief and do not resurrect removed organisms.
- Disconnect shows reconnecting state, disables controls, restores the same paused tick before 60 seconds, and shows expiration afterward.
- Regenerating only replaces the active world after the new session succeeds.

---

## Phase 7: Measure the Real Loop and Verify the Complete Slice

### Overview

Add production workload evidence without changing the historical synthetic benchmark, then verify determinism and user-visible behavior across headless and rendered execution.

### Changes Required

#### 1. Production survival-loop benchmark

**Files**:

- `Backend/benchmarks/WildSeed.Simulation.Benchmarks/Benchmarks/SurvivalLoopPopulation5000V2Benchmark.cs`
- `Backend/benchmarks/WildSeed.Simulation.Benchmarks/Workloads/SurvivalLoopPopulation5000V2Configuration.cs`
- `Backend/benchmarks/WildSeed.Simulation.Benchmarks/README.md`

**Intent**: Measure the real engine with 5,000 active organisms over fixed tick batches while preserving the immutable v1 synthetic baseline.

**Contract**: Setup constructs a deterministic world and runtime state outside measurement. The timed operation runs only engine ticks and returns a cheap observable checksum/tick/population result; snapshots, fingerprints, serialization, SignalR, rendering, and scenario creation remain outside timing. Iterations reset equivalent starting state so deaths do not progressively shrink later samples.

#### 2. Reference evidence

**File**: `Backend/benchmarks/WildSeed.Simulation.Benchmarks/reference-machine.md`

**Intent**: Record a reproducible production result and compare it honestly with the product target.

**Contract**: Evidence records CPU, cores, RAM, OS, runtime, GC/JIT, power mode, date, source revision if available, scenario/configuration, raw artifact location, mean tick time, ticks/s, realtime multiple, and allocations. A result below 200 ticks/s is explicitly marked as an open performance gap and does not fail the slice.

#### 3. Final integration verification

**Files**:

- `Backend/WildSeed.slnx`
- `context/changes/deterministic-survival-loop/plan.md`

**Intent**: Prove the complete backend, API, frontend, architecture, determinism, and benchmark workflows and record execution progress through the canonical checklist.

**Contract**: Headless and rendered runs with the same seed/configuration and command schedule reach identical agreed fingerprints. All automated commands execute from repository root.

### Success Criteria

#### Automated Verification

- Release backend restore and build pass: `dotnet restore Backend/WildSeed.slnx` and `dotnet build Backend/WildSeed.slnx --configuration Release --no-restore`.
- Full Release backend suite passes: `dotnet test Backend/WildSeed.slnx --configuration Release --no-build`.
- Frontend build and lint pass: `npm --prefix Frontend run build` and `npm --prefix Frontend run lint`.
- Production benchmark Dry smoke completes for exactly 5,000 active starting organisms and reports timing, allocations, ticks/s, and realtime multiple.
- Full production benchmark is executed and its result is recorded; threshold comparison remains informational.
- Headless, sampled, and rendered command schedules produce identical agreed v2 fingerprints.

#### Manual Verification

- Run a world through start, every speed, pause, MAX, disconnect/reconnect, resume, and regeneration without losing authoritative state or camera position.
- Observe herbivores eating and drinking, carnivores starving without eating plants, vegetation depletion/regrowth, all three death causes, and compact telemetry updates.
- Confirm the benchmark evidence clearly distinguishes measured result, 200-ticks/s target, and pass/non-blocking status.
- Confirm no S-03 through S-06 behavior or UI has entered the change.

---

## Testing Strategy

### Unit Tests

- Domain bounds and transitions for needs and renewable vegetation.
- Counter-based random repeatability and channel/identity independence.
- Local perception boundaries, stable spatial queries, deterministic action ties, and water/terrain movement constraints.
- Proportional resource allocation under abundance, scarcity, unequal demand, rounding remainders, duplicate IDs, and permuted inputs.
- Tick phase ordering, batch equivalence, carnivore restrictions, death precedence, ordered events, and end-of-tick removal.
- v1 compatibility plus v2 canonical sensitivity and golden checkpoints.
- Session state transitions, stale disconnect protection, fake-time retention, pacing, MAX fairness, latest-only backpressure, event merging, and fault isolation.

### Integration Tests

- World generation creates a paused tokenized session with separate static and dynamic contracts.
- Typed hub attach and control happy path through `WebApplicationFactory` where the test transport is reliable.
- Identical command schedules produce identical v2 fingerprints under different snapshot cadence and slow-consumer behavior.
- Full backend and architecture suites protect layer direction and framework boundaries.
- Frontend contract compatibility is verified by TypeScript build and lint; no frontend test runner is introduced in this slice.

### Manual Testing Steps

1. Generate the same preset and seed twice and record tick-zero v2 fingerprints.
2. Start at `1×`; observe coherent explore, seek, eat, drink, and rest transitions plus changing action telemetry.
3. Pause and wait; confirm tick, state, and fingerprint remain unchanged.
4. Switch through `5×`, `20×`, and `MAX`; confirm authoritative speed status and responsive controls.
5. Pan and zoom during live updates; confirm terrain and camera do not rebuild or reset.
6. Disconnect while running, reconnect before 60 seconds, and confirm the same tick/fingerprint returns paused.
7. Repeat beyond 60 seconds and confirm the session expires with a regenerate path.
8. Observe a resource conflict and verify population behavior remains stable across a repeated same-seed run.
9. Observe death disappearance, visual effect, cause telemetry, and no corpse state.
10. Compare a headless and rendered run at the same command-schedule checkpoint.
11. Run the full production benchmark and inspect the recorded evidence.

## Performance Considerations

The hot loop uses flat resource storage, stable contiguous organism state, reusable buffers, bounded tile perception, and an O(N) spatial-index rebuild. No canonical hashing, DTO creation, serialization, logging, SignalR send, React update, or PixiJS work belongs inside measured ticks. MAX uses bounded batches and cooperative yielding; snapshot sampling uses a capacity-one latest-value path.

The 5,000-organism and 200-ticks/s product target remains visible throughout profiling. For S-02, executing and recording the real benchmark is mandatory while reaching the number is informational. A miss must create an explicit follow-up and cannot be described as meeting the NFR.

## Migration Notes

There is no persisted simulation data. Existing static `WorldFingerprint` and test probe remain v1 and retain their reviewed values. The new runtime state begins at v2; frontend/API consumers must distinguish the static generation fingerprint from the current runtime fingerprint during the contract transition. In-memory sessions vanish on process restart by design.

The existing REST response changes shape to include a session token plus static and tick-zero runtime state. Backend and frontend must land within the same phase sequence; there is no requirement to support an older deployed frontend during this local greenfield change.

## References

- Product requirements: `context/foundation/prd.md`
- Roadmap slice S-02: `context/foundation/roadmap.md`
- Stack boundary: `context/foundation/tech-stack.md`
- F-01 plan: `context/archive/2026-08-26-determinism-performance-contract/plan.md`
- S-01 plan: `context/changes/configurable-procedural-world/plan.md`
- Simulation timing: `Backend/src/WildSeed.Simulation/Contracts/SimulationContract.cs:3`
- Canonical writer: `Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs:7`
- Static world generation: `Backend/src/WildSeed.Simulation/WorldGeneration/WorldGenerator.cs:10`
- Existing determinism matrix: `Backend/tests/WildSeed.Simulation.Tests/Determinism/DeterminismContractTests.cs:7`
- Architecture rules: `Backend/tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs:7`
- Current API endpoint: `Backend/src/WildSeed.Api/Endpoints/WorldEndpoints.cs:8`
- Current renderer: `Frontend/src/rendering/WorldRenderer.ts:28`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Preserve v1 and Establish Runtime Contract v2

#### Automated

- [ ] 1.1 Contract and canonical tests pass with current runtime version 2 and explicit legacy version 1
- [ ] 1.2 Existing v1 golden fingerprints remain unchanged
- [ ] 1.3 Existing static world endpoint continues to return a v1 static fingerprint
- [ ] 1.4 Deterministic random tests pass
- [ ] 1.5 Architecture tests remain green

#### Manual

- [ ] 1.6 Legacy canonical consumers are explicitly pinned before v2 use
- [ ] 1.7 Initial SurvivalRulesV2 values are accepted as contract-versioned behavior

### Phase 2: Introduce Domain Primitives and Engine-Owned Runtime State

#### Automated

- [ ] 2.1 Domain needs and resource transition tests pass
- [ ] 2.2 Tick-zero state creation is deterministic
- [ ] 2.3 Runtime snapshots cannot mutate engine collections
- [ ] 2.4 Existing S-01 tests remain green
- [ ] 2.5 Backend build and architecture tests pass

#### Manual

- [ ] 2.6 Static terrain and initial organisms remain compatible with S-01
- [ ] 2.7 Domain remains free of orchestration and outer dependencies

### Phase 3: Implement the Deterministic Survival Tick

#### Automated

- [ ] 3.1 Action selection and tie-break tests pass
- [ ] 3.2 Perception and spatial-index tests pass
- [ ] 3.3 Herbivore survival-loop tests pass
- [ ] 3.4 Carnivore metabolism restrictions pass
- [ ] 3.5 Proportional resource-sharing tests pass under input permutations
- [ ] 3.6 Batch and single-tick execution are equivalent
- [ ] 3.7 Focused Domain and Simulation suites pass

#### Manual

- [ ] 3.8 Movement and consumption occur as separate actions
- [ ] 3.9 Every state-affecting collection has deterministic ordering or arbitration

### Phase 4: Complete Lifecycle, Events, and v2 Determinism Proof

#### Automated

- [ ] 4.1 Death ordering, cause, event, and removal tests pass
- [ ] 4.2 v2 golden fingerprints match at ticks 0, 10, 100, and 1,000
- [ ] 4.3 Observation cadence and collection permutations preserve v2 fingerprints
- [ ] 4.4 v1 and static world fingerprints remain unchanged
- [ ] 4.5 Full Domain, Simulation, and architecture suites pass

#### Manual

- [ ] 4.6 Induced divergence reports the first differing tick and fingerprints
- [ ] 4.7 v2 canonical field order and golden values are accepted

### Phase 5: Host Paused Sessions and Sampled SignalR State

#### Automated

- [ ] 5.1 Generation returns token, static world, paused tick-zero state, and v2 fingerprint
- [ ] 5.2 Session manager and runner tests pass with fake time
- [ ] 5.3 Latest-only publication and death-event merging tests pass
- [ ] 5.4 Disconnect, reconnect, retention, and expiry tests pass
- [ ] 5.5 Publication cadence and backpressure preserve engine fingerprints
- [ ] 5.6 API integration, backend, and architecture suites pass

#### Manual

- [ ] 5.7 Hub client completes attach, control, disconnect, and paused reattach flow
- [ ] 5.8 Host and connection metadata remain outside canonical simulation state

### Phase 6: Deliver Realtime Controls and Interpolated Rendering

#### Automated

- [ ] 6.1 SignalR dependency is locked in package-lock.json
- [ ] 6.2 Frontend production build passes
- [ ] 6.3 Frontend lint passes
- [ ] 6.4 Full backend suite remains green

#### Manual

- [ ] 6.5 Generated world stays paused and all speed controls report authoritative status
- [ ] 6.6 MAX remains responsive with bounded sampled updates
- [ ] 6.7 Camera state survives live updates and reconnect
- [ ] 6.8 Action and death telemetry matches visible changes
- [ ] 6.9 Reconnect and expiry UX behave at the 60-second boundary
- [ ] 6.10 Regeneration atomically replaces the active session

### Phase 7: Measure the Real Loop and Verify the Complete Slice

#### Automated

- [ ] 7.1 Release backend restore and build pass
- [ ] 7.2 Full Release backend suite passes
- [ ] 7.3 Frontend build and lint pass
- [ ] 7.4 Production benchmark Dry smoke reports required metrics for 5,000 starting organisms
- [ ] 7.5 Full production benchmark result is recorded with informational threshold comparison
- [ ] 7.6 Headless, sampled, and rendered schedules preserve agreed v2 fingerprints

#### Manual

- [ ] 7.7 Complete control, pause, MAX, reconnect, resume, and regeneration flow succeeds
- [ ] 7.8 Survival behavior, vegetation dynamics, carnivore starvation, death causes, and telemetry are observable
- [ ] 7.9 Benchmark evidence distinguishes measured result, target, and non-blocking status
- [ ] 7.10 No S-03 through S-06 scope entered the implementation
