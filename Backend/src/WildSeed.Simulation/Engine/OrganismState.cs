using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Engine;

public sealed class OrganismState
{
    public OrganismState(Guid id, Species species, Genome genome, float x, float y)
    {
        Id = id;
        Species = species;
        Genome = genome;
        X = x;
        Y = y;
        Needs = new OrganismNeeds();
        Action = OrganismAction.Explore;
    }

    public Guid Id { get; }
    public Species Species { get; }
    public Genome Genome { get; }
    public float X { get; set; }
    public float Y { get; set; }
    public int AgeTicks { get; set; }
    public OrganismNeeds Needs { get; set; }
    public OrganismAction Action { get; set; }
}
