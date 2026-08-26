using System.Security.Cryptography;
using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Determinism;

public readonly record struct StateFingerprint : IEquatable<StateFingerprint>, IComparable<StateFingerprint>
{
    public int ContractVersion { get; }
    public string Digest { get; }

    public StateFingerprint(int contractVersion, string digest)
    {
        if (contractVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(contractVersion), "Contract version must be positive.");
        }

        ArgumentNullException.ThrowIfNull(digest);

        if (digest.Length != 64)
        {
            throw new ArgumentException("Digest must be a 64-character lowercase hex SHA-256 string.", nameof(digest));
        }

        ContractVersion = contractVersion;
        Digest = digest.ToLowerInvariant();
    }

    public static StateFingerprint Compute(ReadOnlySpan<byte> canonicalBytes, int contractVersion = SimulationContract.Version)
    {
        byte[] hash = SHA256.HashData(canonicalBytes);
        return new StateFingerprint(contractVersion, Convert.ToHexStringLower(hash));
    }

    public static StateFingerprint Compute(CanonicalStateWriter writer, int contractVersion = SimulationContract.Version)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return Compute(writer.WrittenSpan, contractVersion);
    }

    public override string ToString() => $"v{ContractVersion}:{Digest}";

    public static StateFingerprint Parse(string text)
    {
        if (!TryParse(text, out var result))
        {
            throw new FormatException($"Invalid state fingerprint format: '{text}'. Expected 'v<version>:<64-hex-digest>'.");
        }

        return result;
    }

    public static bool TryParse(string? text, out StateFingerprint fingerprint)
    {
        fingerprint = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!text.StartsWith('v') && !text.StartsWith('V'))
        {
            return false;
        }

        int colonIndex = text.IndexOf(':');
        if (colonIndex <= 1)
        {
            return false;
        }

        ReadOnlySpan<char> versionSpan = text.AsSpan(1, colonIndex - 1);
        if (!int.TryParse(versionSpan, out int version) || version < 1)
        {
            return false;
        }

        string digest = text[(colonIndex + 1)..];
        if (digest.Length != 64)
        {
            return false;
        }

        foreach (char c in digest)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }

        fingerprint = new StateFingerprint(version, digest);
        return true;
    }

    public int CompareTo(StateFingerprint other)
    {
        int versionComparison = ContractVersion.CompareTo(other.ContractVersion);
        if (versionComparison != 0)
        {
            return versionComparison;
        }

        return string.Compare(Digest, other.Digest, StringComparison.Ordinal);
    }
}
