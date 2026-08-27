namespace WildSeed.Simulation.Resources;

public static class ProportionalResourceAllocator
{
    public static IReadOnlyDictionary<Guid, int> Allocate(int available, IEnumerable<(Guid Id, int Requested)> claims)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(available);
        var ordered = claims.OrderBy(claim => claim.Id).ToArray();
        if (ordered.Select(claim => claim.Id).Distinct().Count() != ordered.Length) throw new ArgumentException("Duplicate claim IDs are not allowed.", nameof(claims));
        int total = ordered.Sum(claim => Math.Max(0, claim.Requested));
        var grants = ordered.ToDictionary(claim => claim.Id, _ => 0);
        if (total == 0) return grants;
        int used = 0;
        foreach (var claim in ordered) { int grant = Math.Min(Math.Max(0, claim.Requested), available * Math.Max(0, claim.Requested) / total); grants[claim.Id] = grant; used += grant; }
        for (int i = 0; used < available && ordered.Length > 0; i++, used++) grants[ordered[i % ordered.Length].Id]++;
        return grants;
    }
}
