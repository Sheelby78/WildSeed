# Determinism and Performance Verification Contract — Plan Brief

> Full plan: `context/changes/determinism-performance-contract/plan.md`

## What & Why

Establish an executable foundation for proving that Wild Seed produces repeatable outcomes and can measure rendering-independent headless throughput before simulation behavior expands. Without this contract, later mechanics could hide nondeterminism or performance coupling and invalidate experiments built on them.

## Starting Point

Domain and Simulation contain only assembly markers, and the only existing backend behavior is scaffold API code. The PRD already requires repeatable seed/configuration results, 5,000 active organisms, rendering independence, and at least 20-times-realtime headless execution, but no engine or benchmark exists to verify those properties.

## Desired End State

Simulation exposes a versioned 100 ms logical-tick and canonical SHA-256 state-fingerprint contract. Tests preserve version 1 outcomes through initial, periodic, and final golden checkpoints and prove that observation cadence cannot change results. A dedicated BenchmarkDotNet project reports provisional throughput for an immutable 5,000-agent synthetic probe using a documented reference-machine protocol.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| Logical time | 100 ms per tick | Provides useful behavioral resolution and makes realtime performance arithmetic explicit. |
| Result equivalence | Canonical full-state fingerprint | Detects subtle divergence without producing enormous assertion output. |
| Checkpoints | Initial, periodic, and final | Localizes divergence while avoiding per-tick hashing overhead in normal tests. |
| Compatibility | Stable within a simulation contract version | Enforces repeatability while allowing intentional future rule changes through version bumps. |
| Pre-engine workload | Versioned 5,000-agent synthetic probe | Makes throughput measurable without inventing production ecosystem entities. |
| Performance authority | Documented reference machine | Avoids flaky absolute thresholds on heterogeneous machines. |
| Tooling | Dedicated BenchmarkDotNet project | Separates statistical performance measurement from correctness tests. |
| CI | No workflow in F-01 | Keeps this change focused while leaving CI commands ready for later adoption. |

## Scope

**In scope:**

- Version 1 compatibility identity and 100 ms logical tick.
- Explicit canonical binary encoding and versioned SHA-256 fingerprints.
- Golden deterministic checkpoints and first-divergence diagnostics.
- Observation-cadence independence tests.
- Dedicated BenchmarkDotNet executable and 5,000-agent synthetic workload.
- Throughput/allocation reporting and reference-machine evidence.

**Out of scope:**

- Real worlds, organisms, ecology, rendering, SignalR, API behavior, or speed controls.
- Final certification of the completed ecosystem's performance.
- CI workflows or wall-clock assertions in xUnit.
- Persistence, replay compatibility across contract versions, or an allocation budget.

## Architecture / Approach

Domain remains unchanged. Simulation owns only reusable logical-time, canonical-encoding, and fingerprint contracts. Test-only fixtures prove correctness; benchmark-only synthetic agents exercise representative fixed-cost work. BenchmarkDotNet surrounds the headless workload with wall-clock measurement, while wall time and rendering never enter simulation state.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Deterministic contract | Versioned tick, canonical encoding, and fingerprint primitives | Freezing an ambiguous byte contract before tests cover ordering and numeric rules. |
| 2. Repeatability proof | Golden checkpoints, divergence diagnostics, and observation-independence tests | Accidentally treating the fixture as a production domain model. |
| 3. Synthetic throughput | Dedicated benchmark, 5,000-agent probe, and reference evidence | Mistaking provisional synthetic performance for final product certification. |

**Prerequisites:** None; this is roadmap foundation F-01.

**Estimated effort:** Approximately 2–3 focused implementation sessions across three phases, plus one reference-machine benchmark run.

## Open Risks & Assumptions

- The current development machine becomes the first documented reference machine; its representativeness as a “typical modern computer” should be reassessed when production benchmarking begins.
- Future floating-point state needs an explicit normalization or fixed-point policy before joining the canonical schema.
- The synthetic probe can validate measurement mechanics but cannot predict the cost of perception, behavior scoring, reproduction, combat, or real spatial indexing.
- Version 1 golden hashes are durable compatibility evidence and must not be silently regenerated after intentional outcome changes.

## Success Criteria (Summary)

- Independent same-input runs match reviewed fingerprints at ticks 0, 10, 100, and 1,000 regardless of observation cadence.
- The benchmark smoke path reliably measures exactly 5,000 synthetic agents and reports timing, allocations, ticks per second, and realtime multiple.
- On the documented reference machine, the acceptance run sustains at least 200 ticks per second (20 times realtime) and is explicitly labeled provisional synthetic evidence.
