using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Spatial;

namespace WildSeed.Simulation.Perception;

public sealed class PerceptionService
{
    public PerceptionResult Perceive(SimulationState state, OrganismState organism, SpatialGrid? spatialGrid = null)
    {
        (int X, int Y)? food = null;
        (int X, int Y)? water = null;
        float foodDistance = float.PositiveInfinity;
        float waterDistance = float.PositiveInfinity;

        bool needFood = organism.Species == Species.Herbivore && organism.Needs.Hunger >= SurvivalRulesV4.ActionNeedThreshold;
        bool needWater = organism.Needs.Thirst >= SurvivalRulesV4.ActionNeedThreshold;

        if (needFood || needWater)
        {
            int centerX = (int)MathF.Floor(organism.X);
            int centerY = (int)MathF.Floor(organism.Y);
            int radius = (int)MathF.Ceiling(organism.Genome.Vision);
            float visionSq = organism.Genome.Vision * organism.Genome.Vision;

            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(state.World.Height - 1, centerY + radius);
            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(state.World.Width - 1, centerX + radius);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float distance = DistanceSquared(organism.X, organism.Y, x + 0.5f, y + 0.5f);
                if (distance <= visionSq)
                {
                    if (needFood && state.GetVegetation(x, y).Current > 0 && IsCloser(distance, foodDistance, (x, y), food))
                    {
                        food = (x, y);
                        foodDistance = distance;
                    }
                    if (needWater && state.IsDrinkingTile(x, y) && IsCloser(distance, waterDistance, (x, y), water))
                    {
                        water = (x, y);
                        waterDistance = distance;
                    }
                }
            }
        }

        (float X, float Y)? nearestThreat = null;
        (float X, float Y)? nearestPrey = null;
        Guid? preyId = null;
        (float X, float Y)? nearestMate = null;
        Guid? mateId = null;

        if (spatialGrid is not null)
        {
            if (organism.Species == Species.Herbivore)
            {
                float threatVisionRadius = Math.Min(organism.Genome.Vision, (float)SurvivalRulesV4.DangerPerceptionRadius);
                var threat = spatialGrid.FindNearest(organism.X, organism.Y, threatVisionRadius, Species.Carnivore, state.Organisms);
                if (threat is not null)
                {
                    nearestThreat = (threat.X, threat.Y);
                }
            }
            else if (organism.Species == Species.Carnivore)
            {
                float huntVisionRadius = Math.Max(organism.Genome.Vision, (float)SurvivalRulesV4.HuntPerceptionRadius);
                var prey = spatialGrid.FindNearest(organism.X, organism.Y, huntVisionRadius, Species.Herbivore, state.Organisms);
                if (prey is not null)
                {
                    nearestPrey = (prey.X, prey.Y);
                    preyId = prey.Id;
                }
                else if (organism.Needs.Hunger >= SurvivalRulesV4.ActionNeedThreshold)
                {
                    // Long-range scent: track closest prey across entire map to navigate back to herds
                    var distantPrey = spatialGrid.FindNearest(organism.X, organism.Y, float.PositiveInfinity, Species.Herbivore, state.Organisms);
                    if (distantPrey is not null)
                    {
                        nearestPrey = (distantPrey.X, distantPrey.Y);
                        preyId = distantPrey.Id;
                    }
                }
            }

            if (organism.AgeTicks >= SurvivalRulesV4.MaturationAgeTicks &&
                organism.ReproductionCooldownTicks <= 0 &&
                organism.Needs.Energy >= SurvivalRulesV4.MatingEnergyThreshold &&
                organism.Needs.Hunger < SurvivalRulesV4.CriticalNeed &&
                organism.Needs.Thirst < SurvivalRulesV4.CriticalNeed)
            {
                float mateVisionRadius = organism.Species == Species.Carnivore
                    ? Math.Max(organism.Genome.Vision, (float)SurvivalRulesV4.HuntPerceptionRadius)
                    : organism.Genome.Vision;
                var mate = spatialGrid.FindNearestEligibleMate(organism.X, organism.Y, mateVisionRadius, organism.Species, organism.Id, state.Organisms);

                if (mate is null && organism.Species == Species.Carnivore)
                {
                    // Long-range scent for solitary carnivores seeking mates
                    mate = spatialGrid.FindNearestEligibleMate(organism.X, organism.Y, float.PositiveInfinity, organism.Species, organism.Id, state.Organisms);
                }

                if (mate is not null)
                {
                    nearestMate = (mate.X, mate.Y);
                    mateId = mate.Id;
                }
            }
        }

        return new PerceptionResult(food, water, nearestThreat, nearestPrey, preyId, nearestMate, mateId);
    }

    private static float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static bool IsCloser(float distance, float currentDistance, (int X, int Y) candidate, (int X, int Y)? current) =>
        distance < currentDistance || (distance == currentDistance && (current is null || candidate.Y < current.Value.Y || candidate.Y == current.Value.Y && candidate.X < current.Value.X));

    public static bool IsDrinkingTile(SimulationState state, int x, int y) => state.IsDrinkingTile(x, y);
}
