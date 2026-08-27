using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Random;

namespace WildSeed.Simulation.Movement;

public sealed class MovementResolver
{
    private static readonly (float X, float Y)[] Directions =
    [
        (1f, 0f),
        (1f, 1f),
        (0f, 1f),
        (-1f, 1f),
        (-1f, 0f),
        (-1f, -1f),
        (0f, -1f),
        (1f, -1f)
    ];

    private static readonly int[] HeadingOffsets = [0, 1, 7, 2, 6, 3, 5, 4];

    public void Move(SimulationState state, OrganismState organism, (int X, int Y)? target, bool isFleeing = false, float speedMultiplier = 1.0f)
    {
        float distance = organism.Genome.Speed * speedMultiplier * (float)SimulationContract.LogicalTickDuration.TotalSeconds;

        if (target is { } tile)
        {
            float dx = tile.X + 0.5f - organism.X;
            float dy = tile.Y + 0.5f - organism.Y;
            if (isFleeing)
            {
                dx = -dx;
                dy = -dy;
            }

            if (!TryStep(state, organism, dx, dy, distance) && isFleeing)
            {
                float perpX1 = -dy;
                float perpY1 = dx;
                if (!TryStep(state, organism, perpX1, perpY1, distance))
                {
                    TryStep(state, organism, -perpX1, -perpY1, distance);
                }
            }
            return;
        }

        long epoch = state.Tick / SurvivalRulesV3.ExplorationCadenceTicks;
        int baseHeading = DeterministicRandom.NextInt((ulong)(uint)state.World.Configuration.Seed, epoch, organism.Id, RandomChannel.ExplorationHeading, 0, 8);

        foreach (int offset in HeadingOffsets)
        {
            int heading = (baseHeading + offset) % 8;
            var (dx, dy) = Directions[heading];
            if (TryStep(state, organism, dx, dy, distance))
            {
                break;
            }
        }
    }

    private static bool TryStep(SimulationState state, OrganismState organism, float dx, float dy, float distance)
    {
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0) return false;

        float x = Math.Clamp(organism.X + (dx / length) * distance, 0, state.World.Width - 0.001f);
        float y = Math.Clamp(organism.Y + (dy / length) * distance, 0, state.World.Height - 0.001f);

        if (state.World.Tiles[(int)x, (int)y].Terrain is not (TerrainType.DeepWater or TerrainType.ShallowWater))
        {
            organism.X = x;
            organism.Y = y;
            return true;
        }

        return false;
    }
}
