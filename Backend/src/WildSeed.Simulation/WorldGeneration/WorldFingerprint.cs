using WildSeed.Domain.Organisms;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;

namespace WildSeed.Simulation.WorldGeneration;

public static class WorldFingerprint
{
    public static StateFingerprint Compute(WorldMap world)
    {
        ArgumentNullException.ThrowIfNull(world);

        using var writer = new CanonicalStateWriter();
        writer.WriteHeader((ulong)(uint)world.Configuration.Seed, tick: 0, SimulationContract.Version1);
        writer.WriteInt32(world.Width);
        writer.WriteInt32(world.Height);

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                var tile = world.Tiles[x, y];
                writer.WriteByte((byte)tile.Terrain);
                writer.WriteFloat(tile.VegetationDensity);
            }
        }

        writer.WriteOrdered(
            world.Organisms,
            o => o.Id,
            (w, o) =>
            {
                Span<byte> guidBytes = stackalloc byte[16];
                o.Id.TryWriteBytes(guidBytes);
                w.WriteBytes(guidBytes);
                w.WriteByte((byte)o.Species);
                w.WriteFloat(o.Genome.Speed);
                w.WriteFloat(o.X);
                w.WriteFloat(o.Y);
                w.WriteBoolean(o.IsAlive);
            });

        return StateFingerprint.Compute(writer, SimulationContract.Version1);
    }
}
