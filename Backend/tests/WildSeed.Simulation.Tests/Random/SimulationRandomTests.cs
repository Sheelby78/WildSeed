using WildSeed.Simulation.Random;
using Xunit;

namespace WildSeed.Simulation.Tests.Random;

public sealed class SimulationRandomTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalSequence()
    {
        var rngA = new SimulationRandom(42);
        var rngB = new SimulationRandom(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rngA.NextFloat(), rngB.NextFloat());
            Assert.Equal(rngA.NextInt(0, 1000), rngB.NextInt(0, 1000));
            Assert.Equal(rngA.NextFloat(10.0f, 50.0f), rngB.NextFloat(10.0f, 50.0f));
        }
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentSequence()
    {
        var rngA = new SimulationRandom(42);
        var rngB = new SimulationRandom(99);

        bool anyDifferent = false;
        for (int i = 0; i < 20; i++)
        {
            if (rngA.NextFloat() != rngB.NextFloat())
            {
                anyDifferent = true;
                break;
            }
        }

        Assert.True(anyDifferent);
    }

    [Fact]
    public void NextInt_Range_AlwaysReturnsWithinBounds()
    {
        var rng = new SimulationRandom(12345);

        for (int i = 0; i < 500; i++)
        {
            int val = rng.NextInt(10, 20);
            Assert.InRange(val, 10, 19);
        }
    }

    [Fact]
    public void NextFloat_Range_AlwaysReturnsWithinBounds()
    {
        var rng = new SimulationRandom(12345);

        for (int i = 0; i < 500; i++)
        {
            float val = rng.NextFloat(5.0f, 15.0f);
            Assert.InRange(val, 5.0f, 15.0f);
        }
    }
}
