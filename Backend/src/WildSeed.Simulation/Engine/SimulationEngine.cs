using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.Lifecycle;
using WildSeed.Simulation.Movement;
using WildSeed.Simulation.Perception;
using WildSeed.Simulation.Resources;
using WildSeed.Simulation.Spatial;

namespace WildSeed.Simulation.Engine;

public sealed class SimulationEngine
{
    private readonly SimulationState _state;
    private readonly PerceptionService _perception = new();
    private readonly ActionScorer _scorer = new();
    private readonly MovementResolver _movement = new();
    private readonly VegetationResolver _vegetation = new();
    private readonly SpatialGrid _spatialGrid;

    public SimulationEngine(SimulationState state)
    {
        _state = state;
        _spatialGrid = new SpatialGrid(state.World.Width, state.World.Height);
    }

    public SimulationState State => _state;

    public SimulationSnapshot Snapshot() => new(
        _state.Tick,
        _state.Organisms.OrderBy(item => item.Id)
            .Select(item => new SimulationOrganismSnapshot(item.Id, item.Species, item.X, item.Y, item.AgeTicks, item.Needs, item.Action))
            .ToArray());

    public SimulationTickResult AdvanceTick()
    {
        _state.Tick++;

        for (int i = 0; i < _state.Vegetation.Length; i++)
        {
            _state.Vegetation[i] = _state.Vegetation[i].Regrow(SurvivalRulesV3.VegetationRegrowthPerTick);
        }

        int thirstIncrement = _state.Tick % SurvivalRulesV3.ThirstMetabolismCadenceTicks == 0 ? SurvivalRulesV3.MetabolismThirst : 0;
        foreach (var organism in _state.Organisms)
        {
            organism.AgeTicks++;
            organism.Needs = organism.Needs.Metabolize(SurvivalRulesV3.MetabolismHunger, thirstIncrement, 0);
        }

        _spatialGrid.Rebuild(_state.Organisms);

        var scored = _state.Organisms
            .OrderBy(item => item.Id)
            .Select(organism => (Organism: organism, Intent: _scorer.Score(organism, _perception.Perceive(_state, organism, _spatialGrid))))
            .ToArray();

        foreach (var (organism, intent) in scored)
        {
            organism.Action = intent.Action;

            if (intent.Action is OrganismAction.Explore or OrganismAction.SeekFood or OrganismAction.SeekWater)
            {
                _movement.Move(_state, organism, intent.Target);
                organism.Needs = organism.Needs.Metabolize(0, 0, SurvivalRulesV3.MovementEnergyCost);
            }
            else if (intent.Action == OrganismAction.Hunt)
            {
                _movement.Move(_state, organism, intent.Target, isFleeing: false, speedMultiplier: SurvivalRulesV3.SprintSpeedMultiplier);
                organism.Needs = organism.Needs.Metabolize(0, 0, SurvivalRulesV3.SprintEnergyCost);
            }
            else if (intent.Action == OrganismAction.Flee)
            {
                _movement.Move(_state, organism, intent.Target, isFleeing: true, speedMultiplier: SurvivalRulesV3.SprintSpeedMultiplier);
                organism.Needs = organism.Needs.Metabolize(0, 0, SurvivalRulesV3.SprintEnergyCost);
            }
            else if (intent.Action == OrganismAction.Attack)
            {
                organism.Needs = organism.Needs.Metabolize(0, 0, SurvivalRulesV3.MovementEnergyCost);
            }
            else if (intent.Action == OrganismAction.Drink)
            {
                organism.Needs = organism.Needs.Drink(SurvivalRulesV3.DrinkThirstGain);
            }
            else if (intent.Action == OrganismAction.Rest)
            {
                organism.Needs = organism.Needs.Rest(SurvivalRulesV3.RestEnergyGain);
            }
        }

        var predationDeaths = PredationResolver.Resolve(_state, scored);
        _vegetation.Resolve(_state, scored.Select(item => item.Intent));
        var environmentalDeaths = DeathResolver.Resolve(_state);

        var allEvents = predationDeaths
            .Concat(environmentalDeaths.Cast<OrganismDied>())
            .OrderBy(item => item.OrganismId)
            .Cast<SimulationEvent>()
            .ToArray();

        return new SimulationTickResult(Snapshot(), allEvents);
    }

    public SimulationTickResult AdvanceTicks(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        SimulationTickResult result = new(Snapshot(), []);
        for (int i = 0; i < count; i++)
        {
            result = AdvanceTick();
        }
        return result;
    }
}
