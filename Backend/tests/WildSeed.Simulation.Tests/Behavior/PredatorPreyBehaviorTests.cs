using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Perception;

namespace WildSeed.Simulation.Tests.Behavior;

public sealed class PredatorPreyBehaviorTests
{
    private readonly ActionScorer _scorer = new();

    [Fact]
    public void Carnivore_ScoresHunt_WhenHungryAndPreyPerceived()
    {
        var carnivore = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 10.0f, 10.0f)
        {
            Needs = new OrganismNeeds(hunger: 300, thirst: 0, energy: 1000)
        };

        var preyId = Guid.NewGuid();
        var perception = new PerceptionResult(FoodTile: null, WaterTile: null, NearestThreat: null, NearestPrey: (14.0f, 14.0f), PreyId: preyId);

        var intent = _scorer.Score(carnivore, perception);

        Assert.Equal(OrganismAction.Hunt, intent.Action);
        Assert.Equal(preyId, intent.TargetOrganismId);
        Assert.Equal((14, 14), intent.Target);
    }

    [Fact]
    public void Carnivore_ScoresAttack_WhenPreyIsWithinAttackRange()
    {
        var carnivore = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 10.0f, 10.0f)
        {
            Needs = new OrganismNeeds(hunger: 300, thirst: 0, energy: 1000)
        };

        var preyId = Guid.NewGuid();
        var perception = new PerceptionResult(FoodTile: null, WaterTile: null, NearestThreat: null, NearestPrey: (10.5f, 10.5f), PreyId: preyId);

        var intent = _scorer.Score(carnivore, perception);

        Assert.Equal(OrganismAction.Attack, intent.Action);
        Assert.Equal(preyId, intent.TargetOrganismId);
    }

    [Fact]
    public void Herbivore_ScoresFlee_WhenPredatorIsNear()
    {
        var herbivore = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 20.0f, 20.0f)
        {
            Needs = new OrganismNeeds(hunger: 200, thirst: 200, energy: 1000)
        };

        var perception = new PerceptionResult(FoodTile: (21, 21), WaterTile: null, NearestThreat: (22.0f, 20.0f), NearestPrey: null, PreyId: null);

        var intent = _scorer.Score(herbivore, perception);

        Assert.Equal(OrganismAction.Flee, intent.Action);
        Assert.Equal((22, 20), intent.Target);
    }

    [Fact]
    public void Herbivore_IgnoresFlee_WhenInCriticalAgonalThirst()
    {
        var herbivore = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 20.0f, 20.0f)
        {
            Needs = new OrganismNeeds(hunger: 100, thirst: SurvivalRulesV3.CriticalNeed + 50, energy: 1000)
        };

        var perception = new PerceptionResult(FoodTile: null, WaterTile: (20, 20), NearestThreat: (22.0f, 20.0f), NearestPrey: null, PreyId: null);

        var intent = _scorer.Score(herbivore, perception);

        Assert.Equal(OrganismAction.Drink, intent.Action);
    }
}
