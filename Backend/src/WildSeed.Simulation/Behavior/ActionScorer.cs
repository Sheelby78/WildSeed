using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Perception;

namespace WildSeed.Simulation.Behavior;

public sealed class ActionScorer
{
    public ActionIntent Score(OrganismState organism, PerceptionResult perception)
    {
        var candidates = new List<(OrganismAction Action, int Score, (int X, int Y)? Target, Guid? TargetOrganismId)>
        {
            (OrganismAction.Explore, 1, null, null)
        };

        if (organism.Needs.Energy < 250)
        {
            candidates.Add((OrganismAction.Rest, 750 - organism.Needs.Energy, null, null));
        }
        else if (organism.Needs.Energy < 800 && organism.Needs.Hunger < SurvivalRulesV3.ActionNeedThreshold && organism.Needs.Thirst < SurvivalRulesV3.ActionNeedThreshold)
        {
            candidates.Add((OrganismAction.Rest, (800 - organism.Needs.Energy) / 4 + 10, null, null));
        }

        if (organism.Needs.Thirst >= SurvivalRulesV3.ActionNeedThreshold && perception.WaterTile is { } water)
        {
            candidates.Add((IsAt(organism, water) ? OrganismAction.Drink : OrganismAction.SeekWater, organism.Needs.Thirst * 2, water, null));
        }

        if (organism.Species == Species.Herbivore)
        {
            if (organism.Needs.Hunger >= SurvivalRulesV3.ActionNeedThreshold && perception.FoodTile is { } food)
            {
                candidates.Add((IsAt(organism, food) ? OrganismAction.Eat : OrganismAction.SeekFood, organism.Needs.Hunger * 2, food, null));
            }

            if (perception.NearestThreat is { } threat && organism.Needs.Thirst < SurvivalRulesV3.CriticalNeed && organism.Needs.Hunger < SurvivalRulesV3.CriticalNeed)
            {
                float dx = threat.X - organism.X;
                float dy = threat.Y - organism.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                int threatScore = Math.Max(500, (int)((SurvivalRulesV3.DangerPerceptionRadius - dist + 1.0f) * 150)) + 300;
                candidates.Add((OrganismAction.Flee, threatScore, ((int)MathF.Floor(threat.X), (int)MathF.Floor(threat.Y)), null));
            }
        }
        else if (organism.Species == Species.Carnivore)
        {
            if (organism.Needs.Hunger >= SurvivalRulesV3.ActionNeedThreshold && perception.NearestPrey is { } prey && perception.PreyId is { } preyId)
            {
                float dx = prey.X - organism.X;
                float dy = prey.Y - organism.Y;
                float distSq = dx * dx + dy * dy;
                var preyTile = ((int)MathF.Floor(prey.X), (int)MathF.Floor(prey.Y));

                if (distSq <= SurvivalRulesV3.AttackRangeSquared || IsAt(organism, preyTile))
                {
                    candidates.Add((OrganismAction.Attack, organism.Needs.Hunger * 3, preyTile, preyId));
                }
                else
                {
                    candidates.Add((OrganismAction.Hunt, organism.Needs.Hunger * 2, preyTile, preyId));
                }
            }
        }

        if (perception.NearestMate is { } mate && perception.MateId is { } mateId &&
            organism.AgeTicks >= SurvivalRulesV4.MaturationAgeTicks &&
            organism.ReproductionCooldownTicks <= 0 &&
            organism.Needs.Energy >= SurvivalRulesV4.MatingEnergyThreshold &&
            organism.Needs.Hunger < SurvivalRulesV4.CriticalNeed &&
            organism.Needs.Thirst < SurvivalRulesV4.CriticalNeed)
        {
            var mateTile = ((int)MathF.Floor(mate.X), (int)MathF.Floor(mate.Y));
            int matingScore = 600 + (organism.Needs.Energy - SurvivalRulesV4.MatingEnergyThreshold) / 2;
            candidates.Add((OrganismAction.Mate, matingScore, mateTile, mateId));
        }

        var winner = candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => Priority(candidate.Action))
            .First();

        return new ActionIntent(organism.Id, winner.Action, winner.Target, winner.TargetOrganismId);
    }

    private static bool IsAt(OrganismState organism, (int X, int Y) tile) => (int)MathF.Floor(organism.X) == tile.X && (int)MathF.Floor(organism.Y) == tile.Y;

    private static int Priority(OrganismAction action) => action switch
    {
        OrganismAction.Attack => 0,
        OrganismAction.Drink => 1,
        OrganismAction.Eat => 2,
        OrganismAction.Flee => 3,
        OrganismAction.Hunt => 4,
        OrganismAction.Mate => 5,
        OrganismAction.SeekWater => 6,
        OrganismAction.SeekFood => 7,
        OrganismAction.Rest => 8,
        _ => 9
    };
}
