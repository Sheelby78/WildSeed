namespace WildSeed.Domain.Organisms;

public sealed class Organism
{
    public Guid Id { get; }
    public Species Species { get; }
    public Genome Genome { get; }
    public float X { get; }
    public float Y { get; }
    public bool IsAlive { get; }
    public Guid? MotherId { get; }
    public Guid? FatherId { get; }
    public int Generation { get; }

    public Organism(Guid id, Species species, Genome genome, float x, float y, bool isAlive = true, Guid? motherId = null, Guid? fatherId = null, int generation = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);

        Id = id;
        Species = species;
        Genome = genome;
        X = x;
        Y = y;
        IsAlive = isAlive;
        MotherId = motherId;
        FatherId = fatherId;
        Generation = generation;
    }
}
