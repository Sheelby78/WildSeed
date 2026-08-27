using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Random;

namespace WildSeed.Simulation.Movement;

public sealed class MovementResolver
{
    public void Move(SimulationState state, OrganismState organism, (int X, int Y)? target)
    {
        float dx; float dy;
        if (target is { } tile) { dx = tile.X + 0.5f - organism.X; dy = tile.Y + 0.5f - organism.Y; }
        else
        {
            int heading = DeterministicRandom.NextInt((ulong)(uint)state.World.Configuration.Seed, state.Tick, organism.Id, RandomChannel.ExplorationHeading, 0, 8);
            (dx, dy) = new[] { (1f, 0f), (1f, 1f), (0f, 1f), (-1f, 1f), (-1f, 0f), (-1f, -1f), (0f, -1f), (1f, -1f) }[heading];
        }
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0) return;
        float distance = organism.Genome.Speed * (float)SimulationContract.LogicalTickDuration.TotalSeconds;
        float x = Math.Clamp(organism.X + dx / length * distance, 0, state.World.Width - 0.001f);
        float y = Math.Clamp(organism.Y + dy / length * distance, 0, state.World.Height - 0.001f);
        if (state.World.Tiles[(int)x, (int)y].Terrain is not (TerrainType.DeepWater or TerrainType.ShallowWater)) { organism.X = x; organism.Y = y; }
    }
}
