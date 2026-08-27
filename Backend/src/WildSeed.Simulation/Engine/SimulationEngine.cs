using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.Lifecycle;
using WildSeed.Simulation.Movement;
using WildSeed.Simulation.Perception;
using WildSeed.Simulation.Resources;

namespace WildSeed.Simulation.Engine;

public sealed class SimulationEngine
{
    private readonly SimulationState _state;
    private readonly PerceptionService _perception = new();
    private readonly ActionScorer _scorer = new();
    private readonly MovementResolver _movement = new();
    private readonly VegetationResolver _vegetation = new();

    public SimulationEngine(SimulationState state) => _state = state;
    public SimulationState State => _state;
    public SimulationSnapshot Snapshot() => new(_state.Tick, _state.Organisms.OrderBy(item => item.Id).Select(item => new SimulationOrganismSnapshot(item.Id, item.Species, item.X, item.Y, item.AgeTicks, item.Needs, item.Action)).ToArray());
    public SimulationTickResult AdvanceTick()
    {
        _state.Tick++;
        for (int i = 0; i < _state.Vegetation.Length; i++) _state.Vegetation[i] = _state.Vegetation[i].Regrow(SurvivalRulesV2.VegetationRegrowthPerTick);
        foreach (var organism in _state.Organisms) { organism.AgeTicks++; organism.Needs = organism.Needs.Metabolize(SurvivalRulesV2.MetabolismHunger, SurvivalRulesV2.MetabolismThirst, 0); }
        var intents = _state.Organisms.OrderBy(item => item.Id).Select(organism => _scorer.Score(organism, _perception.Perceive(_state, organism))).ToArray();
        foreach (var intent in intents)
        {
            var organism = _state.Organisms.First(item => item.Id == intent.OrganismId);
            organism.Action = intent.Action;
            if (intent.Action is OrganismAction.Explore or OrganismAction.SeekFood or OrganismAction.SeekWater) { _movement.Move(_state, organism, intent.Target); organism.Needs = organism.Needs.Metabolize(0, 0, SurvivalRulesV2.MovementEnergyCost); }
            else if (intent.Action == OrganismAction.Drink) organism.Needs = organism.Needs.Drink(SurvivalRulesV2.DrinkThirstGain);
            else if (intent.Action == OrganismAction.Rest) organism.Needs = organism.Needs.Rest(SurvivalRulesV2.RestEnergyGain);
        }
        _vegetation.Resolve(_state, intents);
        IReadOnlyList<SimulationEvent> events = DeathResolver.Resolve(_state);
        return new SimulationTickResult(Snapshot(), events);
    }
    public SimulationTickResult AdvanceTicks(int count) { ArgumentOutOfRangeException.ThrowIfNegative(count); SimulationTickResult result = new(Snapshot(), []); for (int i = 0; i < count; i++) result = AdvanceTick(); return result; }
}
