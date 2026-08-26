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
