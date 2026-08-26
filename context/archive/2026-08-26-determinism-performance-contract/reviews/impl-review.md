<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Determinism and Performance Verification Contract

- **Plan**: context/changes/determinism-performance-contract/plan.md
- **Scope**: Full plan (Phases 1 to 3 of 3)
- **Date**: 2026-08-26
- **Verdict**: APPROVED
- **Findings**: 0 critical, 1 warning, 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | PASS |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Incorrect project path in benchmark runbook commands

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Scope Discipline
- **Location**: Backend/benchmarks/WildSeed.Simulation.Benchmarks/README.md:20
- **Detail**: CLI commands in `README.md` reference `--project Backend.Benchmarks/WildSeed.Simulation.Benchmarks/WildSeed.Simulation.Benchmarks.csproj` using `Backend.Benchmarks` instead of the actual directory path `Backend/benchmarks/WildSeed.Simulation.Benchmarks/...`. Running the command from repository root fails with `MSB1009: Project file does not exist`.
- **Fix**: Update `Backend.Benchmarks` to `Backend/benchmarks` in `Backend/benchmarks/WildSeed.Simulation.Benchmarks/README.md`.
- **Decision**: FIXED (Updated project paths in README.md to Backend/benchmarks/...)

### F2 — CanonicalStateWriter.WriteOrdered uses unstable sorting on key collisions

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs:181
- **Detail**: `WriteOrdered` sorts elements using `List<T>.Sort(...)`, which implements unstable introsort. If the caller provides a collection where multiple items share the exact same key (`keySelector(a) == keySelector(b)`), their relative order in the output could vary depending on input order. In current usage, callers provide unique entity IDs.
- **Fix**: Ensure keys passed to `WriteOrdered` are strictly unique, or add a secondary tie-breaker if non-unique keys are ever introduced.
- **Decision**: ACCEPTED (Unique entity ID key requirement is accepted as invariant rule for caller contracts)

### F3 — Heap allocation for strings in CanonicalStateWriter

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Backend/src/WildSeed.Simulation/Determinism/CanonicalStateWriter.cs:154
- **Detail**: `WriteString` allocates a byte array via `Encoding.UTF8.GetBytes(value)`. Because canonical state fingerprints are generated periodically at checkpoints/snapshots (and not inside the per-tick hot loop), this does not impact simulation throughput.
- **Fix**: No action needed now; consider using `Encoding.UTF8.GetBytes(ReadOnlySpan<char>, Span<byte>)` with pooled buffers if strings enter the per-tick hot loop in future slices.
- **Decision**: ACCEPTED (String allocations are acceptable for periodic checkpoints outside the hot tick loop)
