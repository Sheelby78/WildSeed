# Reference Machine Specification and Evidence

## Workload Identity

- **Name**: `synthetic-population-5000-v1`
- **Contract Version**: 1
- **Status**: Provisional synthetic harness evidence

## Target Metrics

- **Population**: 5,000 agents
- **Logical Tick Duration**: 100 ms
- **Target Throughput**: >= 200 ticks/sec
- **Target Latency**: <= 5.0 ms mean per tick
- **Target Multiplier**: >= 20.0x realtime

## Reference Environment

- **OS**: Windows 11 (64-bit)
- **Runtime**: .NET 10.0
- **BenchmarkDotNet Version**: 0.15.8
- **GC / JIT**: Server GC / RyuJIT x64

## SurvivalLoopPopulation5000V2 Dry Evidence

- **Date**: 2026-08-27
- **CPU**: AMD Ryzen 7 8845HS, 8 physical / 16 logical cores
- **OS / Runtime**: Windows 11 25H2, .NET 10.0.11, Concurrent Workstation GC, RyuJIT x64
- **Power mode**: High performance during the measured run
- **Configuration**: seed 42, 256 × 256 world, 5,000 herbivores, 20 ticks per invocation
- **Artifact location**: `artifacts/benchmarks/survival-loop-dry`
- **Result**: 43.751 ms per tick, 23 ticks/s, 2.3× realtime, 50.87 MB allocated per operation
- **Assessment**: Below the 200 ticks/s target. This is an open, non-blocking performance gap for S-02; the Dry job is smoke evidence, not acceptance evidence.
