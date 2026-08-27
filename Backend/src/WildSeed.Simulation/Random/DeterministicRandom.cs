namespace WildSeed.Simulation.Random;

public static class DeterministicRandom
{
    public static ulong NextUInt64(ulong seed, long epoch, Guid organismId, RandomChannel channel)
    {
        Span<byte> id = stackalloc byte[16];
        organismId.TryWriteBytes(id);
        ulong high = BitConverter.ToUInt64(id[..8]);
        ulong low = BitConverter.ToUInt64(id[8..]);
        return Mix(seed ^ (ulong)epoch ^ high ^ RotateLeft(low, 17) ^ (ulong)channel * 0x9E3779B97F4A7C15UL);
    }

    public static int NextInt(ulong seed, long epoch, Guid organismId, RandomChannel channel, int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);
        return minInclusive + (int)(NextUInt64(seed, epoch, organismId, channel) % (uint)(maxExclusive - minInclusive));
    }

    public static float NextSingle(ulong seed, long epoch, Guid organismId, RandomChannel channel) =>
        (NextUInt64(seed, epoch, organismId, channel) >> 40) / (float)(1 << 24);

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    private static ulong RotateLeft(ulong value, int offset) => (value << offset) | (value >> (64 - offset));
}
