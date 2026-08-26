using WildSeed.Simulation.Determinism;

namespace WildSeed.Simulation.Tests.Determinism;

public static class ContractV1GoldenFingerprints
{
    public const ulong GoldenSeed = 1337UL;
    public const int GoldenPopulation = 5;

    public static readonly StateFingerprint Tick0 = StateFingerprint.Parse("v1:1854c6b4a8c83375290d24a65ac3099ad92b72d26e076a99716a7db33fda8644");
    public static readonly StateFingerprint Tick10 = StateFingerprint.Parse("v1:40def33b6ed7c040a3a9a29f1dd795be7a8e08a247e1f48af41d20c0ce606123");
    public static readonly StateFingerprint Tick100 = StateFingerprint.Parse("v1:47ae5fb6e6f9a025bb6f33307fa2ec3b641f3027ee2d9cf7679b5b1d511d28ca");
    public static readonly StateFingerprint Tick1000 = StateFingerprint.Parse("v1:06999a14a03177503c8a4db73c43f6dad69b0f6f4579f2c898ae5c543dccd994");
}
