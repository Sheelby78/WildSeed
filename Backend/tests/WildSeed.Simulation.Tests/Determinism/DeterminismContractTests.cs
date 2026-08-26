using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Tests.Fixtures;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class DeterminismContractTests
{
    [Fact]
    public void Independent_runs_with_same_seed_match_at_all_checkpoints()
    {
        const ulong seed = 1337UL;
        var probe1 = new DeterministicProbe(seed);
        var probe2 = new DeterministicProbe(seed);

        Assert.Equal(probe1.ComputeFingerprint(), probe2.ComputeFingerprint());

        probe1.AdvanceTicks(10);
        probe2.AdvanceTicks(10);
        Assert.Equal(probe1.ComputeFingerprint(), probe2.ComputeFingerprint());

        probe1.AdvanceTicks(90);
        probe2.AdvanceTicks(90);
        Assert.Equal(probe1.ComputeFingerprint(), probe2.ComputeFingerprint());

        probe1.AdvanceTicks(900);
        probe2.AdvanceTicks(900);
        Assert.Equal(probe1.ComputeFingerprint(), probe2.ComputeFingerprint());
    }

    [Fact]
    public void Golden_fingerprints_match_version_1_checkpoints()
    {
        var probe = new DeterministicProbe(ContractV1GoldenFingerprints.GoldenSeed);

        Assert.Equal(ContractV1GoldenFingerprints.Tick0, probe.ComputeFingerprint());

        probe.AdvanceTicks(10);
        Assert.Equal(ContractV1GoldenFingerprints.Tick10, probe.ComputeFingerprint());

        probe.AdvanceTicks(90);
        Assert.Equal(ContractV1GoldenFingerprints.Tick100, probe.ComputeFingerprint());

        probe.AdvanceTicks(900);
        Assert.Equal(ContractV1GoldenFingerprints.Tick1000, probe.ComputeFingerprint());
    }

    [Fact]
    public void Different_seeds_produce_different_fingerprints()
    {
        var probeA = new DeterministicProbe(100UL);
        var probeB = new DeterministicProbe(200UL);

        Assert.NotEqual(probeA.ComputeFingerprint(), probeB.ComputeFingerprint());

        probeA.AdvanceTicks(50);
        probeB.AdvanceTicks(50);
        Assert.NotEqual(probeA.ComputeFingerprint(), probeB.ComputeFingerprint());
    }

    [Fact]
    public void Observation_cadence_does_not_affect_simulation_state()
    {
        const ulong seed = 42UL;
        const int targetTicks = 200;

        var headlessProbe = new DeterministicProbe(seed);
        var everyTickObservedProbe = new DeterministicProbe(seed);
        var sparselyObservedProbe = new DeterministicProbe(seed);

        for (int i = 0; i < targetTicks; i++)
        {
            headlessProbe.AdvanceTick();

            everyTickObservedProbe.AdvanceTick();
            _ = everyTickObservedProbe.GetState();
            _ = everyTickObservedProbe.ComputeFingerprint();

            sparselyObservedProbe.AdvanceTick();
            if (i % 25 == 0)
            {
                _ = sparselyObservedProbe.GetState();
                _ = sparselyObservedProbe.ComputeFingerprint();
            }
        }

        var headlessFp = headlessProbe.ComputeFingerprint();
        var everyTickFp = everyTickObservedProbe.ComputeFingerprint();
        var sparselyFp = sparselyObservedProbe.ComputeFingerprint();

        Assert.Equal(headlessFp, everyTickFp);
        Assert.Equal(headlessFp, sparselyFp);
    }

    [Fact]
    public void Repeated_observation_and_fingerprint_reads_do_not_advance_state()
    {
        var probe = new DeterministicProbe(99UL);
        probe.AdvanceTicks(15);

        var fp1 = probe.ComputeFingerprint();
        _ = probe.GetState();
        _ = probe.GetState();
        var fp2 = probe.ComputeFingerprint();
        var fp3 = probe.ComputeFingerprint();

        Assert.Equal(15, probe.CurrentTick);
        Assert.Equal(fp1, fp2);
        Assert.Equal(fp2, fp3);
    }

    [Fact]
    public void Mismatch_diagnostic_identifies_first_divergent_tick()
    {
        var probeA = new DeterministicProbe(555UL);
        var probeB = new DeterministicProbe(555UL);

        long? firstDivergenceTick = null;
        StateFingerprint? fpA = null;
        StateFingerprint? fpB = null;

        for (int tick = 1; tick <= 50; tick++)
        {
            probeA.AdvanceTick();
            probeB.AdvanceTick();

            if (tick == 23)
            {
                probeB.AdvanceTick();
            }

            var currentFpA = probeA.ComputeFingerprint();
            var currentFpB = probeB.ComputeFingerprint();

            if (currentFpA != currentFpB)
            {
                firstDivergenceTick = tick;
                fpA = currentFpA;
                fpB = currentFpB;
                break;
            }
        }

        Assert.Equal(23, firstDivergenceTick);
        Assert.NotNull(fpA);
        Assert.NotNull(fpB);
        Assert.NotEqual(fpA, fpB);
    }
}
