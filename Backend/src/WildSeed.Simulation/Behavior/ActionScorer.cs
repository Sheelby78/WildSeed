using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Perception;

namespace WildSeed.Simulation.Behavior;

public sealed class ActionScorer
{
    public ActionIntent Score(OrganismState organism, PerceptionResult perception)
    {
        var candidates = new List<(OrganismAction Action, int Score, (int X, int Y)? Target)> { (OrganismAction.Explore, 1, null) };
        if (organism.Needs.Energy < SurvivalRulesV2.CriticalNeed / 3) candidates.Add((OrganismAction.Rest, SurvivalRulesV2.CriticalNeed - organism.Needs.Energy, null));
        if (organism.Needs.Thirst >= SurvivalRulesV2.ActionNeedThreshold && perception.WaterTile is { } water) candidates.Add((IsAt(organism, water) ? OrganismAction.Drink : OrganismAction.SeekWater, organism.Needs.Thirst * 2, water));
        if (organism.Needs.Hunger >= SurvivalRulesV2.ActionNeedThreshold && organism.Species == Species.Herbivore && perception.FoodTile is { } food) candidates.Add((IsAt(organism, food) ? OrganismAction.Eat : OrganismAction.SeekFood, organism.Needs.Hunger, food));
        var winner = candidates.OrderByDescending(candidate => candidate.Score).ThenBy(candidate => Priority(candidate.Action)).First();
        return new ActionIntent(organism.Id, winner.Action, winner.Target);
    }

    private static bool IsAt(OrganismState organism, (int X, int Y) tile) => (int)MathF.Floor(organism.X) == tile.X && (int)MathF.Floor(organism.Y) == tile.Y;
    private static int Priority(OrganismAction action) => action switch { OrganismAction.Drink => 0, OrganismAction.Eat => 1, OrganismAction.SeekWater => 2, OrganismAction.SeekFood => 3, OrganismAction.Rest => 4, _ => 5 };
}
