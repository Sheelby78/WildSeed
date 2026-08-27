using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Perception;
using Xunit;

namespace WildSeed.Simulation.Tests.Behavior;

public sealed class MatingActionScorerTests
{
    private readonly ActionScorer _scorer = new();

    [Fact]
    public void Scorer_ChoosesMate_WhenOrganismIsEligibleAndMateIsPerceived()
    {
        var org = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 10.0f, 10.0f)
        {
            AgeTicks = SurvivalRulesV4.MaturationAgeTicks + 10,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 700),
            ReproductionCooldownTicks = 0
        };

        var mateId = Guid.NewGuid();
        var perception = new PerceptionResult(
            FoodTile: (15, 15),
            WaterTile: (20, 20),
            NearestThreat: null,
            NearestPrey: null,
            PreyId: null,
            NearestMate: (11.0f, 10.0f),
            MateId: mateId);

        var intent = _scorer.Score(org, perception);

        Assert.Equal(OrganismAction.Mate, intent.Action);
        Assert.Equal(mateId, intent.TargetOrganismId);
        Assert.Equal((11, 10), intent.Target);
    }

    [Fact]
    public void Scorer_SuppressesMate_WhenCriticalHungerOrThirst()
    {
        var org = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 10.0f, 10.0f)
        {
            AgeTicks = SurvivalRulesV4.MaturationAgeTicks + 10,
            Needs = new OrganismNeeds(hunger: SurvivalRulesV4.CriticalNeed + 50, thirst: 100, energy: 700),
            ReproductionCooldownTicks = 0
        };

        var mateId = Guid.NewGuid();
        var perception = new PerceptionResult(
            FoodTile: (10, 10),
            WaterTile: (20, 20),
            NearestThreat: null,
            NearestPrey: null,
            PreyId: null,
            NearestMate: (11.0f, 10.0f),
            MateId: mateId);

        var intent = _scorer.Score(org, perception);

        Assert.Equal(OrganismAction.Eat, intent.Action);
    }

    [Fact]
    public void Scorer_SuppressesMate_WhenThreatIsPresent()
    {
        var org = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 10.0f, 10.0f)
        {
            AgeTicks = SurvivalRulesV4.MaturationAgeTicks + 10,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 700),
            ReproductionCooldownTicks = 0
        };

        var mateId = Guid.NewGuid();
        var perception = new PerceptionResult(
            FoodTile: (15, 15),
            WaterTile: (20, 20),
            NearestThreat: (11.0f, 11.0f),
            NearestPrey: null,
            PreyId: null,
            NearestMate: (10.0f, 12.0f),
            MateId: mateId);

        var intent = _scorer.Score(org, perception);

        Assert.Equal(OrganismAction.Flee, intent.Action);
    }
}
