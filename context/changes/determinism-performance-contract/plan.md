# Determinism and Performance Verification Contract Implementation Plan

## Overview

Establish the reusable contracts and executable verification harnesses that later simulation slices will use to prove repeatability, rendering-independent execution, and headless throughput. This change defines deterministic logical time, versioned canonical state fingerprints, regression checkpoints, and a dedicated synthetic BenchmarkDotNet workload without introducing provisional ecosystem behavior.

## Current State Analysis

Wild Seed is still at scaffold stage. `WildSeed.Domain` and `WildSeed.Simulation` contain only assembly markers, the API contains the generated weather example, and the only handwritten backend tests enforce dependency direction. There is no world state, tick loop, random-source policy, renderer adapter, or representative ecosystem workload to certify yet.

The product requirements nevertheless make determinism and performance load-bearing: the same seed and configuration must yield the same outcome, rendering cadence must not affect simulation state, 5,000 active organisms must be supported, and headless `MAX` execution must reach at least 20 times realtime. F-01 must therefore make these properties measurable now while clearly distinguishing a synthetic harness result from final ecosystem certification.

## Desired End State

The repository exposes a versioned simulation contract with a 100 ms logical tick and an explicit canonical state encoding whose SHA-256 fingerprint is stable within contract version 1. Automated tests prove identical initial, periodic, and final fingerprints across independent executions, preserve reviewed golden vectors across builds, detect relevant state changes, canonicalize unordered input, and demonstrate that observation cadence cannot alter state.

A separate BenchmarkDotNet console project measures a named, immutable `synthetic-population-5000-v1` headless workload. It reports mean time, allocation data, ticks per second, and realtime multiple; documents the reference-machine protocol; and treats at least 200 ticks per second (no more than 5 ms mean per tick) as the provisional 20-times-realtime acceptance threshold. The result validates the harness only. A later real survival-loop benchmark becomes authoritative for the product NFR.

### Key Discoveries:

- F-01 is explicitly a prerequisite measurement contract that unlocks the first world slice and later determinism/performance verification (`context/foundation/roadmap.md:54`).
- The product contract requires at least 5,000 active organisms, 20-times-realtime headless execution, deterministic outcomes, and rendering-independent state (`context/foundation/prd.md:97`).
- `MAX` means no wall-clock synchronization; rendering may be throttled or disabled while the engine advances as quickly as hardware allows (`context/foundation/brief.md:526`).
- Simulation currently has no engine code and depends only on Domain (`Backend/src/WildSeed.Simulation/WildSeed.Simulation.csproj:3`).
- Existing architecture tests prohibit Domain from referencing outer layers and Simulation from referencing API (`Backend.Tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs:10`).
- Simulation tests already reference both Simulation and Domain and use xUnit (`Backend.Tests/WildSeed.Simulation.Tests/WildSeed.Simulation.Tests.csproj:10`).

## What We're NOT Doing

- Implementing world generation, organisms, needs, genomes, behavior selection, spatial indexing for production, reproduction, combat, statistics, or a real simulation loop.
- Adding rendering, React, PixiJS, SignalR, API endpoints, speed controls, host pacing, or sampled transport messages.
- Claiming that the finished ecosystem already satisfies the 5,000-organism or 20-times-realtime NFR.
- Adding CI workflows or failing ordinary xUnit runs on machine-dependent elapsed-time thresholds.
- Guaranteeing replay compatibility across different simulation contract versions.
- Adding persistence, replay files, migrations, benchmark databases, or committed volatile BenchmarkDotNet output.
- Defining an allocation budget before a representative production workload exists.
- Retaining the generated API weather sample as part of the simulation contract; removing it belongs to the slice that replaces the scaffold API.

## Implementation Approach

Keep production scope deliberately narrow. Domain remains unchanged. Simulation owns logical tick semantics plus explicit canonical encoding and fingerprint primitives because these are reusable engine contracts. Correctness fixtures and synthetic agents remain in test and benchmark projects so they cannot become accidental domain models.

Canonical fingerprints use an explicit binary schema rather than reflection, ambient JSON serialization, runtime hash codes, or collection iteration order. Version 1 golden fingerprints are checked in at fixed logical ticks. Intentional changes to simulation rules, random streams, numeric semantics, seeded initialization, or canonical encoding require a new contract version and new golden set; performance-only refactors retain version 1 when fingerprints remain unchanged.

Wall-clock measurement exists only in the BenchmarkDotNet process. The benchmark initializes exactly 5,000 fixed-count synthetic agents outside the timed region, executes bounded deterministic state updates and local-neighbor reads headlessly, and exposes a checksum to prevent dead-code elimination. BenchmarkDotNet handles warmup and repeated measurements. Absolute acceptance is evaluated only on a documented reference machine; arbitrary machines may run a non-authoritative smoke job.

## Critical Implementation Details

### Performance constraints

One 100 ms logical tick represents 0.1 simulated seconds, so realtime is 10 ticks per wall-clock second. The 20-times-realtime target is therefore at least 200 ticks per second, equivalently no more than 5 ms mean wall-clock time per tick. Initialization, snapshot reads, canonical fingerprinting, reporting, and console I/O must remain outside the timed benchmark operation.

### State sequencing

Lock the version, tick duration, and canonical byte format before generating golden hashes. Canonical ordering tests must pass before committing golden vectors, and expected version 1 hashes must never be regenerated automatically during tests. Observation/fingerprint reads must be side-effect-free and must not advance simulation state or consume random values.

## Phase 1: Define the Versioned Deterministic Contract

### Overview

Introduce fixed logical time and a small, explicit canonical fingerprint facility in Simulation. Unit tests lock the byte-level rules before any golden simulation checkpoints are created.

### Changes Required:

#### 1. Simulation contract identity and timing

**File**: `Backend/src/WildSeed.Simulation/Contracts/SimulationContract.cs`

**Intent**: Define the compatibility identity and logical-time constants that future runners, tests, and benchmarks share without consulting wall-clock time.

**Contract**: Contract version 1; 100 ms per logical tick; 10 realtime ticks per simulated second; and the derived 200-ticks-per-second target for 20-times-realtime acceptance. Version identity covers outcome-affecting rules and canonical fingerprint semantics, not the application release number.

#### 2. Canonical state encoding

**File**: `Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs`

**Intent**: Provide an explicit, allocation-conscious writer for simulation-relevant state so fingerprint stability does not depend on runtime serialization behavior, locale, or container iteration order.

**Contract**: The format writes a magic header, fingerprint schema identity, simulation contract version, seed/configuration fields, logical tick, and state fields in documented order. It uses little-endian fixed-width integers, length-prefixed UTF-8 strings, explicit null markers and collection counts, numeric/ordinal stable-ID ordering, and defined boolean/enum encodings. Floating-point support must normalize negative zero and NaN representations or defer the field until a numeric policy exists.

#### 3. Versioned state fingerprint

**File**: `Backend/src/WildSeed.Simulation/Determinism/StateFingerprint.cs`

**Intent**: Turn canonical state bytes into a compact equality contract suitable for checkpoints, diagnostics, and future engine snapshots.

**Contract**: SHA-256 over the complete canonical preimage, represented with explicit version identity and lowercase hexadecimal digest such as `v1:<digest>`. Equality must cover simulation state only and exclude rendering DTOs, wall clock, logging, allocations, benchmark metadata, and observation cadence.

#### 4. Contract and canonicalization tests

**Files**:

- `Backend.Tests/WildSeed.Simulation.Tests/Contracts/SimulationContractTests.cs`
- `Backend.Tests/WildSeed.Simulation.Tests/Determinism/CanonicalStateFingerprintTests.cs`

**Intent**: Lock the timing math, encoding behavior, stable ordering, version inclusion, and fingerprint sensitivity before downstream tests depend on golden hashes.

**Contract**: Tests prove 100 ms equals 10 simulated ticks per second, 20 times realtime equals 200 ticks per second, reordered entity input yields the same digest after stable-ID canonicalization, and any relevant field or contract-version change changes the digest. Repeated computation over the same state must be identical.

### Success Criteria:

#### Automated Verification:

- Backend solution restores successfully: `dotnet restore Backend/WildSeed.slnx`
- Backend solution builds without warnings or errors: `dotnet build Backend/WildSeed.slnx --no-restore`
- Contract and canonical fingerprint tests pass: `dotnet test Backend.Tests/WildSeed.Simulation.Tests/WildSeed.Simulation.Tests.csproj --no-build --filter "FullyQualifiedName~SimulationContractTests|FullyQualifiedName~CanonicalStateFingerprintTests"`
- Architecture dependency tests remain green: `dotnet test Backend.Tests/WildSeed.Architecture.Tests/WildSeed.Architecture.Tests.csproj --no-build`

#### Manual Verification:

- Review confirms the canonical byte format is explicit, culture-independent, stable-order, and documented at the writer boundary.
- Review confirms Domain remains unchanged and Simulation contains no wall-clock, ASP.NET Core, SignalR, rendering, or BenchmarkDotNet dependency.

**Implementation Note**: After completing this phase and all automated verification passes, pause for human confirmation that the contract/version and canonical encoding rules are acceptable before generating version 1 golden fingerprints.

---

## Phase 2: Prove Repeatability and Observation Independence

### Overview

Create a small test-only deterministic scenario and use it to prove independent-run repeatability, cross-build stability through committed golden checkpoints, useful divergence diagnostics, and side-effect-free observation.

### Changes Required:

#### 1. Test-only deterministic scenario

**Files**:

- `Backend.Tests/WildSeed.Simulation.Tests/Fixtures/DeterministicProbe.cs`
- `Backend.Tests/WildSeed.Simulation.Tests/Fixtures/DeterministicProbeState.cs`

**Intent**: Supply a minimal versioned state machine that exercises seeded initialization, stable entity identity, deterministic updates, and observation without inventing production organisms or ecosystem rules.

**Contract**: Two separately constructed probes with the same seed, configuration, and contract version traverse identical states. The fixture exposes logical tick advancement and read-only canonical-state projection. Its fixed rules are verification data, not a production simulation abstraction.

#### 2. Version 1 golden checkpoints

**File**: `Backend.Tests/WildSeed.Simulation.Tests/Determinism/ContractV1GoldenFingerprints.cs`

**Intent**: Preserve reviewed expected results so compatibility within contract version 1 is stronger than merely comparing two executions of the same potentially regressed implementation.

**Contract**: Store immutable expected fingerprints for one named seed/configuration at tick 0, periodic ticks 10 and 100, and final tick 1,000. Intentional outcome changes require a new contract version and a separate golden set; tests never rewrite expected values.

#### 3. Determinism and observation-cadence tests

**File**: `Backend.Tests/WildSeed.Simulation.Tests/Determinism/DeterminismContractTests.cs`

**Intent**: Exercise the contract across independent executions and demonstrate that observing state cannot influence future state.

**Contract**: Compare initial, periodic, and final fingerprints for independent same-input runs; match version 1 golden vectors; prove different seeds and relevant state mutations diverge; and compare headless, every-tick-observed, and sparsely observed runs at agreed checkpoints. Failure output identifies the first divergent logical tick and both fingerprints.

#### 4. Architecture boundary reinforcement

**File**: `Backend.Tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs`

**Intent**: Preserve the execution-core boundary as verification infrastructure grows.

**Contract**: Extend the existing rules only where assembly references can reliably prove that Simulation remains free of API, ASP.NET Core, SignalR, and rendering dependencies. Avoid brittle source-text checks for forbidden calls.

### Success Criteria:

#### Automated Verification:

- Same seed, configuration, and contract version match at ticks 0, 10, 100, and 1,000 across independent runs.
- Version 1 golden fingerprints match without runtime regeneration.
- Different seeds and relevant state changes produce different fingerprints while reordered input canonicalizes identically.
- Headless, every-tick-observed, and sparsely observed runs produce identical agreed checkpoints.
- Repeated observation and fingerprint reads do not advance state or change later fingerprints.
- Full backend test suite passes: `dotnet test Backend/WildSeed.slnx --no-build`

#### Manual Verification:

- Temporarily induce and then revert a test-fixture divergence; confirm failure output identifies the first divergent tick and expected/actual fingerprints.
- Review confirms the fixture remains test-only and does not define provisional Domain entities or production behavior.

**Implementation Note**: After completing this phase and all automated verification passes, pause for human confirmation that golden-vector lifecycle and mismatch diagnostics are understandable before adding performance measurement.

---

## Phase 3: Measure the Versioned 5,000-Agent Synthetic Probe

### Overview

Add a dedicated BenchmarkDotNet executable that validates headless throughput measurement and records provisional reference-machine evidence without turning elapsed time into a generic correctness-test gate.

### Changes Required:

#### 1. Benchmark project integration

**Files**:

- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/WildSeed.Simulation.Benchmarks.csproj`
- `Backend/WildSeed.slnx`

**Intent**: Isolate performance tooling as an outer consumer of Simulation while including it in normal solution restore and Release build verification.

**Contract**: A .NET 10 console project references Simulation and pins BenchmarkDotNet 0.15.8. The solution groups it under `/benchmarks/`; neither Domain nor Simulation references BenchmarkDotNet or the benchmark project.

#### 2. Benchmark entry point and reporting configuration

**Files**:

- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Program.cs`
- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Benchmarking/PerformanceContractConfig.cs`
- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Benchmarking/RealtimeMultiplierColumn.cs`

**Intent**: Provide selectable benchmark execution, memory diagnostics, machine-readable artifacts, and derived contract metrics.

**Contract**: Use BenchmarkDotNet's assembly switcher, default statistical job, memory diagnoser, GitHub Markdown and JSON export, and columns for ticks per second and realtime multiple. A dry job is explicitly smoke-only. Acceptance reports retain mean, error, standard deviation, allocations per tick, and the derived threshold result.

#### 3. Immutable synthetic workload version 1

**Files**:

- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Benchmarks/SyntheticPopulation5000V1Benchmark.cs`
- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Workloads/SyntheticPopulation5000V1Scenario.cs`
- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/Workloads/SyntheticPopulation5000V1Configuration.cs`

**Intent**: Exercise a representative but explicitly provisional headless workload over exactly 5,000 active synthetic agents.

**Contract**: The versioned scenario uses a fixed seed, fixed population, stable IDs, contiguous/preallocated state, deterministic read/write buffers, bounded need/position updates, and bounded fixed-grid local-neighbor queries. Setup is outside measurement; one invocation batches 200 logical ticks and declares 200 operations; state complexity does not grow; and the benchmark consumes a checksum. It excludes world generation, snapshots, fingerprinting, serialization, I/O, rendering, births, deaths, genomes, behavior scoring, combat, pathfinding, and API transport.

#### 4. Runbook and reference-machine evidence

**Files**:

- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/README.md`
- `Backend.Benchmarks/WildSeed.Simulation.Benchmarks/reference-machine.md`
- `.gitignore`

**Intent**: Make smoke and authoritative acceptance runs reproducible and prevent volatile benchmark output from entering source control.

**Contract**: Document distinct Release smoke and default acceptance commands, artifact paths, scenario/version/seed, 5,000-agent count, 100 ms tick, 200-tick batch, and the 200-ticks-per-second/5-ms-per-tick threshold. Record CPU, cores, RAM, OS/build, architecture, .NET SDK/runtime, BenchmarkDotNet version, GC/JIT, power mode, run date, source revision, and result. Label synthetic results provisional; later S-02 adds a separate real survival-loop benchmark rather than mutating V1. Ignore `artifacts/benchmarks/` and default BenchmarkDotNet artifact directories.

### Success Criteria:

#### Automated Verification:

- Release solution restore and build include the benchmark project: `dotnet restore Backend/WildSeed.slnx` then `dotnet build Backend/WildSeed.slnx --configuration Release --no-restore`
- Benchmark smoke job completes for exactly 5,000 agents without enforcing the absolute threshold: `dotnet run --project Backend.Benchmarks/WildSeed.Simulation.Benchmarks/WildSeed.Simulation.Benchmarks.csproj --configuration Release --no-build -- --filter *SyntheticPopulation5000V1* --job Dry --artifacts artifacts/benchmarks/smoke`
- Smoke output includes mean time, allocation data, ticks per second, and realtime multiple and is labeled non-authoritative.
- Full backend test suite remains green after benchmark integration: `dotnet test Backend/WildSeed.slnx --configuration Release --no-build`
- Benchmark artifacts are excluded by repository ignore rules.

#### Manual Verification:

- Run the default Release benchmark on the documented reference machine under the recorded power/debugger/background-process conditions.
- Confirm the reference result sustains at least 200 ticks per second and at least 20 times realtime, with mean time no greater than 5 ms per tick.
- Confirm the committed evidence records environment, source revision, raw artifact location, allocation baseline, and an explicit synthetic/provisional label.

**Implementation Note**: After completing this phase and all automated verification passes, pause for human confirmation of the reference-machine acceptance result. A failure below 20 times realtime is a valid measured outcome requiring optimization or contract review; it must not be relabeled as success.

---

## Testing Strategy

### Unit Tests:

- Lock logical tick and realtime-multiplier arithmetic.
- Lock every canonical primitive encoding, null/count delimiter, stable-ID ordering, version preamble, and digest representation.
- Prove relevant-field sensitivity and observation purity.
- Preserve contract version 1 through immutable golden checkpoint vectors.

### Integration Tests:

- Run independent deterministic fixtures from identical seed/configuration through ticks 0, 10, 100, and 1,000.
- Compare headless and differently sampled observation runs at the same logical checkpoints.
- Run the complete solution test suite to preserve architecture boundaries.
- Build and dry-run the benchmark executable as an integration smoke check, without treating dry timing as acceptance evidence.

### Manual Testing Steps:

1. Inspect the canonical schema and confirm each simulation-relevant field must be written explicitly in stable order.
2. Introduce a temporary deterministic mismatch and verify diagnostics identify the first divergent tick, then revert it.
3. Run the full BenchmarkDotNet job in Release on the documented reference machine without a debugger and with the recorded power profile.
4. Inspect Markdown/JSON output and verify the derived ticks-per-second and realtime-multiple arithmetic.
5. Record the reference result and confirm it is labeled as synthetic harness evidence rather than final ecosystem certification.

## Performance Considerations

The correctness fingerprint is intentionally outside the benchmark's timed region because F-01 measures simulated work, not verification overhead. The synthetic workload uses fixed-size, preallocated state and bounded per-agent operations so repeated benchmark invocations retain constant complexity. Memory diagnostics establish an allocation baseline, but no allocation pass/fail budget is imposed until a representative production workload exists.

Use BenchmarkDotNet's default warmup and repeated measurements for acceptance. The dry job verifies wiring only. Acceptance is authoritative solely when the recorded machine metadata matches the reference profile; generic local or future hosted-runner results are informative. A conservative report should retain mean plus measurement error alongside the simple mean-at-or-below-5-ms threshold so marginal outcomes are visible.

## Migration Notes

There is no existing persisted state or public simulation contract to migrate. Contract version 1 becomes the baseline. When an intentional outcome-affecting change occurs, increment the contract version and add a new golden vector set while retaining version 1 history. Do not silently rewrite version 1 hashes.

Keep `synthetic-population-5000-v1` immutable as harness evidence. S-01 may add a procedural-world workload, and S-02 must add a distinct benchmark that drives the real headless survival loop. Only that production scenario can certify the final 5,000-organism and 20-times-realtime product requirement.

## References

- Product requirements: `context/foundation/prd.md`
- Roadmap foundation F-01: `context/foundation/roadmap.md`
- Original MAX and deterministic system-order semantics: `context/foundation/brief.md`
- Architecture rules: `AGENTS.md`
- Existing dependency tests: `Backend.Tests/WildSeed.Architecture.Tests/DependencyRulesTests.cs`
- Existing Simulation test project: `Backend.Tests/WildSeed.Simulation.Tests/WildSeed.Simulation.Tests.csproj`
- BenchmarkDotNet execution guidance: `https://benchmarkdotnet.org/articles/guides/how-to-run.html`
- BenchmarkDotNet measurement model: `https://benchmarkdotnet.org/articles/guides/how-it-works.html`
- BenchmarkDotNet good practices: `https://benchmarkdotnet.org/articles/guides/good-practices.html`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Define the Versioned Deterministic Contract

#### Automated

- [x] 1.1 Backend solution restores successfully
- [x] 1.2 Backend solution builds without warnings or errors
- [x] 1.3 Contract and canonical fingerprint tests pass
- [x] 1.4 Architecture dependency tests remain green

#### Manual

- [x] 1.5 Canonical byte format is explicit, culture-independent, stable-order, and documented
- [x] 1.6 Domain remains unchanged and Simulation contains no forbidden outer-layer dependencies

### Phase 2: Prove Repeatability and Observation Independence

#### Automated

- [x] 2.1 Independent runs match at ticks 0, 10, 100, and 1,000
- [x] 2.2 Version 1 golden fingerprints match without runtime regeneration
- [x] 2.3 Fingerprint sensitivity and canonical ordering tests pass
- [x] 2.4 Observation cadence and repeated reads do not change simulation results
- [x] 2.5 Full backend test suite passes

#### Manual

- [x] 2.6 Induced mismatch diagnostic identifies the first divergent tick and fingerprints
- [x] 2.7 Deterministic fixture remains test-only and free of provisional domain behavior

### Phase 3: Measure the Versioned 5,000-Agent Synthetic Probe

#### Automated

- [x] 3.1 Release solution restore and build include the benchmark project
- [x] 3.2 Benchmark smoke job completes for exactly 5,000 agents without threshold enforcement
- [x] 3.3 Smoke output reports timing, allocations, ticks per second, and realtime multiple
- [x] 3.4 Full Release backend test suite remains green
- [x] 3.5 Benchmark artifacts are excluded by repository ignore rules

#### Manual

- [x] 3.6 Default Release benchmark runs under documented reference-machine conditions
- [x] 3.7 Reference result meets 200 ticks per second and 20 times realtime
- [x] 3.8 Evidence records environment, revision, artifacts, allocations, and provisional status
