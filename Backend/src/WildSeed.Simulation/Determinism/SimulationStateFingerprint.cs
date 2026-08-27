using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Determinism;

public static class SimulationStateFingerprint
{
    public static StateFingerprint Compute(SimulationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        using var writer = new CanonicalStateWriter();
        writer.WriteHeader((ulong)(uint)state.World.Configuration.Seed, state.Tick, SimulationContract.CurrentVersion);
        var config = state.World.Configuration;
        writer.WriteInt32(config.Width).WriteInt32(config.Height).WriteInt32(config.InitialHerbivores).WriteInt32(config.InitialCarnivores)
            .WriteFloat(config.VegetationDensity).WriteFloat(config.WaterLevel).WriteFloat(config.MutationProbability).WriteFloat(config.MutationStrength);
        for (int y = 0; y < state.World.Height; y++)
        for (int x = 0; x < state.World.Width; x++)
        {
            var tile = state.World.Tiles[x, y];
            var vegetation = state.GetVegetation(x, y);
            writer.WriteByte((byte)tile.Terrain).WriteInt32(vegetation.Current).WriteInt32(vegetation.Capacity);
        }
        writer.WriteOrdered(state.Organisms, item => item.Id, (target, organism) =>
        {
            Span<byte> bytes = stackalloc byte[16]; organism.Id.TryWriteBytes(bytes);
            target.WriteBytes(bytes).WriteByte((byte)organism.Species).WriteFloat(organism.Genome.Speed).WriteFloat(organism.X).WriteFloat(organism.Y)
                .WriteInt32(organism.AgeTicks).WriteInt32(organism.Needs.Hunger).WriteInt32(organism.Needs.Thirst).WriteInt32(organism.Needs.Energy).WriteByte((byte)organism.Action);
        });
        return StateFingerprint.Compute(writer, SimulationContract.CurrentVersion);
    }
}
