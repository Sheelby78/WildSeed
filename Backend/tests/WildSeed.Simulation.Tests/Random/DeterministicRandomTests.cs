using WildSeed.Simulation.Random;
using Xunit;

namespace WildSeed.Simulation.Tests.Random;

public sealed class DeterministicRandomTests
{
    [Fact]
    public void NextInt_ConsecutiveEpochs_DoNotFormStrictMod8Cycle()
    {
        ulong seed = 42UL;
        Guid id = Guid.NewGuid();
        var channel = RandomChannel.ExplorationHeading;

        var values = new int[16];
        for (int epoch = 0; epoch < 16; epoch++)
        {
            values[epoch] = DeterministicRandom.NextInt(seed, epoch, id, channel, 0, 8);
        }

        bool strictlyAlternatingOrCycling = true;
        int diff = (values[1] - values[0] + 8) % 8;
        for (int i = 2; i < values.Length; i++)
        {
            if ((values[i] - values[i - 1] + 8) % 8 != diff)
            {
                strictlyAlternatingOrCycling = false;
                break;
            }
        }

        Assert.False(strictlyAlternatingOrCycling);
    }

    [Fact]
    public void SameInputs_ProduceIdenticalOutputs()
    {
        ulong seed = 12345UL;
        Guid id = Guid.NewGuid();
        var channel = RandomChannel.ExplorationHeading;

        int valA = DeterministicRandom.NextInt(seed, 10, id, channel, 0, 8);
        int valB = DeterministicRandom.NextInt(seed, 10, id, channel, 0, 8);

        Assert.Equal(valA, valB);
    }

    [Fact]
    public void NextInt_AlwaysWithinSpecifiedRange()
    {
        ulong seed = 999UL;
        Guid id = Guid.NewGuid();

        for (int epoch = 0; epoch < 200; epoch++)
        {
            int val = DeterministicRandom.NextInt(seed, epoch, id, RandomChannel.ExplorationHeading, 3, 11);
            Assert.InRange(val, 3, 10);
        }
    }
}
