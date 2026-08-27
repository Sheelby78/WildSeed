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
        int centerX = (int)MathF.Floor(organism.X);
        int centerY = (int)MathF.Floor(organism.Y);
        int radius = SurvivalRulesV3.PerceptionRadius;

        for (int y = Math.Max(0, centerY - radius); y <= Math.Min(state.World.Height - 1, centerY + radius); y++)
        for (int x = Math.Max(0, centerX - radius); x <= Math.Min(state.World.Width - 1, centerX + radius); x++)
        {
            float distance = DistanceSquared(organism.X, organism.Y, x + 0.5f, y + 0.5f);
            if (organism.Species == Species.Herbivore && state.GetVegetation(x, y).Current > 0 && IsCloser(distance, foodDistance, (x, y), food))
            {
                food = (x, y);
                foodDistance = distance;
            }
            if (IsDrinkingTile(state, x, y) && IsCloser(distance, waterDistance, (x, y), water))
            {
                water = (x, y);
                waterDistance = distance;
            }
        }

        (float X, float Y)? nearestThreat = null;
        (float X, float Y)? nearestPrey = null;
        Guid? preyId = null;

        if (spatialGrid is not null)
        {
            if (organism.Species == Species.Herbivore)
            {
                var threat = spatialGrid.FindNearest(organism.X, organism.Y, SurvivalRulesV3.DangerPerceptionRadius, Species.Carnivore, state.Organisms);
                if (threat is not null)
                {
                    nearestThreat = (threat.X, threat.Y);
                }
            }
            else if (organism.Species == Species.Carnivore)
            {
                var prey = spatialGrid.FindNearest(organism.X, organism.Y, SurvivalRulesV3.HuntPerceptionRadius, Species.Herbivore, state.Organisms);
                if (prey is not null)
                {
                    nearestPrey = (prey.X, prey.Y);
                    preyId = prey.Id;
                }
            }
        }

        return new PerceptionResult(food, water, nearestThreat, nearestPrey, preyId);
    }

    private static float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static bool IsCloser(float distance, float currentDistance, (int X, int Y) candidate, (int X, int Y)? current) =>
        distance < currentDistance || (distance == currentDistance && (current is null || candidate.Y < current.Value.Y || candidate.Y == current.Value.Y && candidate.X < current.Value.X));

    private static readonly (int Dx, int Dy)[] CardinalOffsets = [(0, -1), (-1, 0), (1, 0), (0, 1)];

    public static bool IsDrinkingTile(SimulationState state, int x, int y)
    {
        if (state.World.Tiles[x, y].Terrain is TerrainType.DeepWater or TerrainType.ShallowWater) return false;
        foreach (var (dx, dy) in CardinalOffsets)
        {
            int nx = x + dx; int ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < state.World.Width && ny < state.World.Height && state.World.Tiles[nx, ny].Terrain is TerrainType.DeepWater or TerrainType.ShallowWater) return true;
        }
        return false;
    }
}
