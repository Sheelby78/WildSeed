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

    private static readonly float[] StandardAngleOffsets =
    [
        0f,
        MathF.PI / 8f, -MathF.PI / 8f,
        MathF.PI / 4f, -MathF.PI / 4f,
        3f * MathF.PI / 8f, -3f * MathF.PI / 8f,
        MathF.PI / 2f, -MathF.PI / 2f,
        5f * MathF.PI / 8f, -5f * MathF.PI / 8f,
        3f * MathF.PI / 4f, -3f * MathF.PI / 4f,
        7f * MathF.PI / 8f, -7f * MathF.PI / 8f,
        MathF.PI
    ];

    private static readonly float[] StuckAngleOffsets =
    [
        MathF.PI,
        3f * MathF.PI / 4f, -3f * MathF.PI / 4f,
        5f * MathF.PI / 8f, -5f * MathF.PI / 8f,
        MathF.PI / 2f, -MathF.PI / 2f,
        3f * MathF.PI / 8f, -3f * MathF.PI / 8f,
        MathF.PI / 4f, -MathF.PI / 4f,
        0f
    ];

    public void Move(SimulationState state, OrganismState organism, (int X, int Y)? target, bool isFleeing = false, float speedMultiplier = 1.0f)
    {
        float effectiveSpeed = organism.Genome.Speed / MathF.Sqrt(organism.Genome.Size);
        float distance = effectiveSpeed * speedMultiplier * (float)SimulationContract.LogicalTickDuration.TotalSeconds;

        if (target is { } tile)
        {
            float dx = tile.X + 0.5f - organism.X;
            float dy = tile.Y + 0.5f - organism.Y;
            if (isFleeing)
            {
                dx = -dx;
                dy = -dy;
            }

            float baseAngle = MathF.Atan2(dy, dx);
            float prevX = organism.X;
            float prevY = organism.Y;

            var angleOffsets = organism.StuckTicks >= 2 ? StuckAngleOffsets : StandardAngleOffsets;
            bool preferPositive = ((organism.Id.GetHashCode() ^ (int)(state.Tick / 6)) & 1) == 0;

            foreach (float offset in angleOffsets)
            {
                float testAngle = preferPositive ? baseAngle + offset : baseAngle - offset;
                float testDx = MathF.Cos(testAngle);
                float testDy = MathF.Sin(testAngle);

                if (TryStep(state, organism, testDx, testDy, distance))
                {
                    break;
                }
            }

            if (MathF.Abs(organism.X - prevX) < 0.0001f && MathF.Abs(organism.Y - prevY) < 0.0001f)
            {
                organism.StuckTicks++;
            }
            else
            {
                organism.StuckTicks = Math.Max(0, organism.StuckTicks - 1);
            }

            return;
        }

        long epoch = state.Tick / SurvivalRulesV3.ExplorationCadenceTicks;
        int baseHeading = DeterministicRandom.NextInt((ulong)(uint)state.World.Configuration.Seed, epoch, organism.Id, RandomChannel.ExplorationHeading, 0, 8);

        float prevExploreX = organism.X;
        float prevExploreY = organism.Y;

        foreach (int offset in HeadingOffsets)
        {
            int heading = (baseHeading + offset) % 8;
            var (dx, dy) = Directions[heading];
            if (TryStep(state, organism, dx, dy, distance))
            {
                break;
            }
        }

        if (MathF.Abs(organism.X - prevExploreX) < 0.0001f && MathF.Abs(organism.Y - prevExploreY) < 0.0001f)
        {
            organism.StuckTicks++;
        }
        else
        {
            organism.StuckTicks = 0;
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
