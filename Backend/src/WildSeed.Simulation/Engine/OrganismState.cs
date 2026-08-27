using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Engine;

public sealed class OrganismState
{
    public OrganismState(
        Guid id,
        Species species,
        Genome genome,
        float x,
        float y,
        Guid? motherId = null,
        Guid? fatherId = null,
        int generation = 1,
        int reproductionCooldownTicks = 0)
    {
        Id = id;
        Species = species;
        Genome = genome;
        X = x;
        Y = y;
        Needs = new OrganismNeeds();
        Action = OrganismAction.Explore;
        MotherId = motherId;
        FatherId = fatherId;
        Generation = generation;
        ReproductionCooldownTicks = reproductionCooldownTicks;
    }

    public Guid Id { get; }
    public Species Species { get; }
    public Genome Genome { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int AgeTicks { get; set; }
    public OrganismNeeds Needs { get; set; }
    public OrganismAction Action { get; set; }
    public Guid? MotherId { get; }
    public Guid? FatherId { get; }
    public int Generation { get; }
    public int ReproductionCooldownTicks { get; set; }
}
