namespace WildSeed.Simulation.Random;

public sealed class SimulationRandom
{
    private readonly System.Random _random;

    public SimulationRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);
        return _random.Next(minInclusive, maxExclusive);
    }

    public float NextFloat()
    {
        return _random.NextSingle();
    }

    public float NextFloat(float min, float max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, max);
        return min + _random.NextSingle() * (max - min);
    }
}
