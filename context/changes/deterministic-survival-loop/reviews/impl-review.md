<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Deterministic Survival Loop

- **Plan**: context/changes/deterministic-survival-loop/plan.md
- **Scope**: Phases 1 to 7 (Full Plan Review)
- **Date**: 2026-08-27
- **Verdict**: NEEDS ATTENTION
- **Findings**: 1 critical, 5 warnings, 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING ⚠️ |
| Scope Discipline | PASS ✅ |
| Safety & Quality | FAIL ❌ |
| Architecture | PASS ✅ |
| Pattern Consistency | PASS ✅ |
| Success Criteria | PASS ✅ |

## Findings

### F1 — O(N²) linear search in simulation hot loop

- **Severity**: ❌ CRITICAL
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs:31
- **Detail**: In `AdvanceTick()`, intent application executes `_state.Organisms.First(item => item.Id == intent.OrganismId)` inside the intent loop. For N=5,000 organisms, this performs up to 25,000,000 comparisons per tick, creating a severe CPU bottleneck in the simulation engine.
- **Fix**: Build a local `Dictionary<Guid, OrganismState>` or map once before intent processing to enable O(1) lookups during action execution.
- **Decision**: FIXED (Direct pairing (Organism, Intent) eliminates O(N^2) lookup)

### F2 — Endianness-dependent BitConverter in deterministic random

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Random/DeterministicRandom.cs:9
- **Detail**: `DeterministicRandom` uses `BitConverter.ToUInt64(id[..8])` which follows native system endianness. On big-endian architectures, random values will diverge, breaking cross-platform simulation determinism.
- **Fix**: Use `BinaryPrimitives.ReadUInt64LittleEndian` from `System.Buffers.Binary` for deterministic byte parsing regardless of CPU architecture.
- **Decision**: FIXED (Replaced BitConverter with BinaryPrimitives.ReadUInt64LittleEndian)

### F3 — Hot-loop array allocation in IsDrinkingTile

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Perception/PerceptionService.cs:48
- **Detail**: `IsDrinkingTile` allocates `new[] { (0, -1), (-1, 0), (1, 0), (0, 1) }` on every tile checked within the perception radius, generating over 1.4 million heap allocations per tick for 5,000 organisms.
- **Fix**: Replace with a static cached `readonly (int Dx, int Dy)[]` array or inline coordinate offsets.
- **Decision**: FIXED (Replaced with static cached CardinalOffsets array)

### F4 — Missing LatestSnapshotMailbox causing runner stall on SignalR backpressure

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: Backend/src/WildSeed.Api/SimulationHosting/SimulationRunnerService.cs:16
- **Detail**: `SimulationRunnerService` synchronously awaits `hub.Clients.Group(session.Token).Snapshot(...)` in its main tick loop. Network lag or slow client consumption directly blocks tick advancement across sessions.
- **Fix A ⭐ Recommended**: Implement `LatestSnapshotMailbox` with capacity-one replace-oldest buffering and merged death events to completely decouple engine ticking from network I/O.
  - Strength: Fully meets the S-02 architectural specification and isolates simulation execution from client network speed.
  - Tradeoff: Adds an internal channel/queue and dispatch task per active session.
  - Confidence: HIGH — standard robust pattern for game/simulation server state replication.
  - Blind spot: None significant.
- **Fix B**: Keep synchronous runner broadcast but wrap in timeout / fire-and-forget task.
  - Strength: Minimal code modification.
  - Tradeoff: Potential out-of-order delivery or unmerged dropped death events.
  - Confidence: LOW — does not guarantee single-mailbox death event merging.
  - Blind spot: SignalR channel buffer behavior under heavy load.
- **Decision**: FIXED (Implemented LatestSnapshotMailbox with bounded channel and runner isolation via Fix A)

### F5 — Missing golden checkpoints and unit/determinism test suites

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: Backend/tests/WildSeed.Simulation.Tests/Determinism/ContractV2GoldenFingerprints.cs:1
- **Detail**: Planned test files (`ContractV2GoldenFingerprints.cs`, `SurvivalLoopDeterminismTests.cs`, `HerbivoreSurvivalTests.cs`, `CarnivoreMetabolismTests.cs`, `DeathResolutionTests.cs`, and API session tests) were not created.
- **Fix**: Generate and add the v2 golden checkpoint tests and behavioral unit test suites to lock in regression protection for the survival loop.
  - Strength: Guarantees future changes do not break deterministic survival behavior or v2 canonical fingerprints.
  - Tradeoff: Requires running golden recording once and committing test fixtures.
  - Confidence: HIGH — mirrors existing v1 golden test pattern.
  - Blind spot: None.
- **Decision**: FIXED (Added ContractV2GoldenFingerprints, SurvivalLoopDeterminismTests, HerbivoreSurvivalTests, CarnivoreMetabolismTests, DeathResolutionTests)

### F6 — PixiJS Graphics recreation churn and missing ticker interpolation

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: Frontend/src/rendering/WorldRenderer.ts:124
- **Detail**: `updateOrganisms` destroys and recreates the PixiJS `Graphics` instance on every snapshot rather than reusing and clearing it. In addition, position interpolation on the Pixi ticker was deferred.
- **Decision**: FIXED (Reused persistent organismGraphics instance with .clear() in WorldRenderer)

### F7 — Unnecessary full snapshot creation and sorting on every internal tick

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Engine/SimulationEngine.cs:39
- **Detail**: `AdvanceTick()` creates a full snapshot with LINQ `.OrderBy(item => item.Id)` at the end of every tick, even during multi-tick batches in `AdvanceTicks(N)` where intermediate snapshots are discarded.
- **Fix**: Make snapshot generation explicit (`CreateSnapshot()`) and only return events or lightweight state from internal batch iterations.
- **Decision**: SKIPPED

### F8 — Full SHA-256 state hashing at 15 Hz SignalR cadence

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Api/SimulationHosting/SimulationSession.cs:32
- **Detail**: `SimulationSession.CreateResponse()` computes a full canonical state SHA-256 fingerprint across all tiles and organisms 15 times per second.
- **Fix**: Sample the canonical fingerprint at lower cadence (e.g. 1 Hz) or compute on demand rather than on every 66 ms snapshot.
- **Decision**: FIXED (Sampled fingerprint every 10 ticks and when paused)
