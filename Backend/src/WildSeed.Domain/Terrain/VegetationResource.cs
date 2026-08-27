namespace WildSeed.Domain.Terrain;

public readonly record struct VegetationResource
{
    public int Current { get; }
    public int Capacity { get; }

    public VegetationResource(int current, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Capacity = capacity;
        Current = Math.Clamp(current, 0, capacity);
    }

    public VegetationResource Consume(int units) => new(Current - Math.Max(0, units), Capacity);

    public VegetationResource Regrow(int units) => new(Current + Math.Max(0, units), Capacity);
}
