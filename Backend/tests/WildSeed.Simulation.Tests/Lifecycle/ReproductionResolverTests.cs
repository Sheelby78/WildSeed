using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Lifecycle;
using Xunit;

namespace WildSeed.Simulation.Tests.Lifecycle;

public sealed class ReproductionResolverTests
{
    private static SimulationState CreateState(int popCap = 5000, float mutationProb = 0.0f, float mutationStrength = 0.0f)
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(
            seed: 12345,
            width: 64,
            height: 64,
            initialHerbivores: 2,
            initialCarnivores: 0,
            vegetationDensity: 0.5f,
            waterLevel: 0.1f,
            mutationProbability: mutationProb,
            mutationStrength: mutationStrength);

        var world = new WorldMap(config, tiles, []);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void Resolve_ExecutesMating_BlendsTraitsAndDeductsEnergy()
    {
        var state = CreateState(mutationProb: 0.0f);
        var orgA = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f, 1.0f, 8.0f), 5.0f, 5.0f, generation: 1)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 800),
            ReproductionCooldownTicks = 0
        };

        var orgB = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(2.0f, 2.0f, 12.0f), 5.2f, 5.2f, generation: 2)
        {
            AgeTicks = 220,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 750),
            ReproductionCooldownTicks = 0
        };

        state.Organisms.Add(orgA);
        state.Organisms.Add(orgB);

        var scored = new (OrganismState Organism, ActionIntent Intent)[]
        {
            (orgA, new ActionIntent(orgA.Id, OrganismAction.Mate, (5, 5), orgB.Id)),
            (orgB, new ActionIntent(orgB.Id, OrganismAction.Mate, (5, 5), orgA.Id))
        };

        var births = ReproductionResolver.Resolve(state, scored);

        Assert.Single(births);
        var birth = births[0];

        Assert.Equal(Species.Herbivore, birth.Species);
        Assert.Equal(3, birth.Generation);
        Assert.Equal(1.5f, birth.Genome.Speed, precision: 2);
        Assert.Equal(1.5f, birth.Genome.Size, precision: 2);
        Assert.Equal(10.0f, birth.Genome.Vision, precision: 2);

        Assert.Equal(600, orgA.Needs.Energy);
        Assert.Equal(550, orgB.Needs.Energy);
        Assert.Equal(SurvivalRulesV4.MatingCooldownTicks, orgA.ReproductionCooldownTicks);
        Assert.Equal(SurvivalRulesV4.MatingCooldownTicks, orgB.ReproductionCooldownTicks);
        Assert.Equal(3, state.Organisms.Count);
    }

    [Fact]
    public void Resolve_PreventsDoubleMating_InSameTick()
    {
        var state = CreateState();
        var orgA = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 5.0f, 5.0f)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(energy: 800)
        };
        var orgB = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 5.1f, 5.0f)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(energy: 800)
        };
        var orgC = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 5.2f, 5.0f)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(energy: 800)
        };

        state.Organisms.Add(orgA);
        state.Organisms.Add(orgB);
        state.Organisms.Add(orgC);

        var scored = new (OrganismState Organism, ActionIntent Intent)[]
        {
            (orgA, new ActionIntent(orgA.Id, OrganismAction.Mate, (5, 5), orgB.Id)),
            (orgC, new ActionIntent(orgC.Id, OrganismAction.Mate, (5, 5), orgB.Id)),
            (orgB, new ActionIntent(orgB.Id, OrganismAction.Mate, (5, 5), orgA.Id))
        };

        var births = ReproductionResolver.Resolve(state, scored);

        Assert.Single(births);
    }

    [Fact]
    public void Resolve_EnforcesPopulationCap()
    {
        var state = CreateState();
        for (int i = 0; i < SurvivalRulesV4.MaxPopulationCap; i++)
        {
            state.Organisms.Add(new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 5.0f, 5.0f));
        }

        var orgA = state.Organisms[0];
        orgA.AgeTicks = 250;
        orgA.Needs = new OrganismNeeds(energy: 800);

        var orgB = state.Organisms[1];
        orgB.AgeTicks = 250;
        orgB.Needs = new OrganismNeeds(energy: 800);

        var scored = new (OrganismState Organism, ActionIntent Intent)[]
        {
            (orgA, new ActionIntent(orgA.Id, OrganismAction.Mate, (5, 5), orgB.Id))
        };

        var births = ReproductionResolver.Resolve(state, scored);

        Assert.Empty(births);
        Assert.Equal(SurvivalRulesV4.MaxPopulationCap, state.Organisms.Count);
    }

    [Fact]
    public void Resolve_WithMutations_ProducesBoundedVariations()
    {
        var state = CreateState(mutationProb: 1.0f, mutationStrength: 0.2f);
        var orgA = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f, 1.0f, 8.0f), 5.0f, 5.0f)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(energy: 800)
        };
        var orgB = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f, 1.0f, 8.0f), 5.1f, 5.0f)
        {
            AgeTicks = 250,
            Needs = new OrganismNeeds(energy: 800)
        };

        state.Organisms.Add(orgA);
        state.Organisms.Add(orgB);

        var scored = new (OrganismState Organism, ActionIntent Intent)[]
        {
            (orgA, new ActionIntent(orgA.Id, OrganismAction.Mate, (5, 5), orgB.Id))
        };

        var births = ReproductionResolver.Resolve(state, scored);
        Assert.Single(births);
        var birth = births[0];

        Assert.InRange(birth.Genome.Speed, SurvivalRulesV4.MinSpeed, SurvivalRulesV4.MaxSpeed);
        Assert.InRange(birth.Genome.Size, SurvivalRulesV4.MinSize, SurvivalRulesV4.MaxSize);
        Assert.InRange(birth.Genome.Vision, SurvivalRulesV4.MinVision, SurvivalRulesV4.MaxVision);
    }
}
