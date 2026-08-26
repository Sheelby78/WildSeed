# Wild Seed Simulation Benchmarks

## Overview

Executes headless performance benchmarks for Wild Seed simulation workloads. Validates throughput and allocation profiles against simulation timing contracts.

## Workload: SyntheticPopulation5000V1

- **Population**: 5,000 active synthetic agents
- **Logical Tick**: 100 ms (0.1 simulated seconds)
- **Batch Size**: 200 ticks per invocation
- **Target Acceptance**: >= 200 ticks/sec (<= 5.0 ms mean per tick, 20x realtime)
- **Status**: Provisional synthetic harness benchmark

## Commands

### Smoke Run (Dry Job, non-authoritative)

```bash
dotnet run --project Backend.Benchmarks/WildSeed.Simulation.Benchmarks/WildSeed.Simulation.Benchmarks.csproj --configuration Release -- --filter *SyntheticPopulation5000V1* --job Dry --artifacts artifacts/benchmarks/smoke
```

### Full Acceptance Benchmark

```bash
dotnet run --project Backend.Benchmarks/WildSeed.Simulation.Benchmarks/WildSeed.Simulation.Benchmarks.csproj --configuration Release -- --filter *SyntheticPopulation5000V1* --artifacts artifacts/benchmarks/acceptance
```
