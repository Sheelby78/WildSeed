using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Perception;

public sealed class PerceptionService
{
    public PerceptionResult Perceive(SimulationState state, OrganismState organism)
    {
        (int X, int Y)? food = null;
        (int X, int Y)? water = null;
        float foodDistance = float.PositiveInfinity;
        float waterDistance = float.PositiveInfinity;
        int centerX = (int)MathF.Floor(organism.X);
        int centerY = (int)MathF.Floor(organism.Y);
        for (int y = Math.Max(0, centerY - SurvivalRulesV2.PerceptionRadius); y <= Math.Min(state.World.Height - 1, centerY + SurvivalRulesV2.PerceptionRadius); y++)
        for (int x = Math.Max(0, centerX - SurvivalRulesV2.PerceptionRadius); x <= Math.Min(state.World.Width - 1, centerX + SurvivalRulesV2.PerceptionRadius); x++)
        {
            float distance = DistanceSquared(organism.X, organism.Y, x + 0.5f, y + 0.5f);
            if (state.GetVegetation(x, y).Current > 0 && IsCloser(distance, foodDistance, (x, y), food))
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
        return new PerceptionResult(food, water);
    }

    private static float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static bool IsCloser(float distance, float currentDistance, (int X, int Y) candidate, (int X, int Y)? current) =>
        distance < currentDistance || (distance == currentDistance && (current is null || candidate.Y < current.Value.Y || candidate.Y == current.Value.Y && candidate.X < current.Value.X));

    public static bool IsDrinkingTile(SimulationState state, int x, int y)
    {
        if (state.World.Tiles[x, y].Terrain is TerrainType.DeepWater or TerrainType.ShallowWater) return false;
        foreach (var (dx, dy) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
        {
            int nx = x + dx; int ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < state.World.Width && ny < state.World.Height && state.World.Tiles[nx, ny].Terrain is TerrainType.DeepWater or TerrainType.ShallowWater) return true;
        }
        return false;
    }
}
