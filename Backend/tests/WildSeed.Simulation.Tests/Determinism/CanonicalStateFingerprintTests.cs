using System.Buffers.Binary;
using System.Text;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class CanonicalStateFingerprintTests
{
    [Fact]
    public void Header_writes_magic_schema_contract_seed_and_tick_explicitly()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteHeader(seed: 0x123456789ABCDEF0UL, tick: 42L, contractVersion: 1);

        byte[] bytes = writer.ToByteArray();
        byte[] expectedMagic = "WILDSEED"u8.ToArray();

        Assert.Equal(expectedMagic, bytes.Take(8).ToArray());
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4)));
        Assert.Equal(0x123456789ABCDEF0UL, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16, 8)));
        Assert.Equal(42L, BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(24, 8)));
    }

    [Fact]
    public void Primitives_are_written_little_endian()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteInt16(0x1234)
              .WriteUInt16(0x5678)
              .WriteInt32(0x12345678)
              .WriteUInt32(0x9ABCDEF0)
              .WriteInt64(0x123456789ABCDEF0)
              .WriteUInt64(0xFEDCBA9876543210);

        byte[] bytes = writer.ToByteArray();

        Assert.Equal(0x1234, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(0, 2)));
        Assert.Equal((ushort)0x5678, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2)));
        Assert.Equal(0x12345678, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4)));
        Assert.Equal(0x9ABCDEF0, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8, 4)));
        Assert.Equal(0x123456789ABCDEF0, BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(12, 8)));
        Assert.Equal(0xFEDCBA9876543210, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(20, 8)));
    }

    [Fact]
    public void Boolean_and_presence_are_strictly_0x00_and_0x01()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteBoolean(false)
              .WriteBoolean(true)
              .WritePresence(false)
              .WritePresence(true);

        byte[] bytes = writer.ToByteArray();
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x01 }, bytes);
    }

    [Fact]
    public void Floating_point_normalization_normalizes_negative_zero()
    {
        using var writerPositive = new CanonicalStateWriter();
        writerPositive.WriteFloat(+0.0f).WriteDouble(+0.0d);

        using var writerNegative = new CanonicalStateWriter();
        writerNegative.WriteFloat(-0.0f).WriteDouble(-0.0d);

        Assert.Equal(writerPositive.ToByteArray(), writerNegative.ToByteArray());
    }

    [Fact]
    public void Floating_point_normalization_normalizes_different_nan_payloads()
    {
        float nan1 = float.NaN;
        float nan2 = BitConverter.Int32BitsToSingle(0x7FC00001);

        double dNan1 = double.NaN;
        double dNan2 = BitConverter.Int64BitsToDouble(0x7FF8000000000001L);

        using var writer1 = new CanonicalStateWriter();
        writer1.WriteFloat(nan1).WriteDouble(dNan1);

        using var writer2 = new CanonicalStateWriter();
        writer2.WriteFloat(nan2).WriteDouble(dNan2);

        Assert.Equal(writer1.ToByteArray(), writer2.ToByteArray());
    }

    [Fact]
    public void String_encodes_length_prefixed_utf8_and_handles_null()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteString("Wild Seed \u2764")
              .WriteString("")
              .WriteString(null);

        byte[] bytes = writer.ToByteArray();

        int len1 = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4));
        string str1 = Encoding.UTF8.GetString(bytes.AsSpan(4, len1));
        Assert.Equal("Wild Seed \u2764", str1);

        int offset = 4 + len1;
        int len2 = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));
        Assert.Equal(0, len2);

        offset += 4;
        int len3 = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset, 4));
        Assert.Equal(-1, len3);
    }

    [Fact]
    public void WriteOrdered_sorts_items_by_key_before_writing()
    {
        var entities = new[]
        {
            new { Id = 42, Name = "Agent 42" },
            new { Id = 7, Name = "Agent 7" },
            new { Id = 99, Name = "Agent 99" }
        };

        var reorderedEntities = new[]
        {
            new { Id = 99, Name = "Agent 99" },
            new { Id = 42, Name = "Agent 42" },
            new { Id = 7, Name = "Agent 7" }
        };

        using var writer1 = new CanonicalStateWriter();
        writer1.WriteOrdered(entities, e => e.Id, (w, e) => { w.WriteInt32(e.Id); w.WriteString(e.Name); });

        using var writer2 = new CanonicalStateWriter();
        writer2.WriteOrdered(reorderedEntities, e => e.Id, (w, e) => { w.WriteInt32(e.Id); w.WriteString(e.Name); });

        Assert.Equal(writer1.ToByteArray(), writer2.ToByteArray());
    }

    [Fact]
    public void StateFingerprint_formats_and_parses_correctly()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteHeader(seed: 12345, tick: 100);
        writer.WriteInt32(999);

        var fingerprint = StateFingerprint.Compute(writer);
        string formatted = fingerprint.ToString();

        Assert.StartsWith("v1:", formatted);
        Assert.Equal(67, formatted.Length);

        var parsed = StateFingerprint.Parse(formatted);
        Assert.Equal(fingerprint, parsed);
        Assert.Equal(1, parsed.ContractVersion);
        Assert.Equal(fingerprint.Digest, parsed.Digest);
    }

    [Fact]
    public void StateFingerprint_changes_on_any_state_field_change()
    {
        using var writerA = new CanonicalStateWriter();
        writerA.WriteHeader(seed: 1, tick: 0).WriteInt32(100);

        using var writerB = new CanonicalStateWriter();
        writerB.WriteHeader(seed: 1, tick: 0).WriteInt32(101);

        var fpA = StateFingerprint.Compute(writerA);
        var fpB = StateFingerprint.Compute(writerB);

        Assert.NotEqual(fpA, fpB);
        Assert.NotEqual(fpA.Digest, fpB.Digest);
    }

    [Fact]
    public void StateFingerprint_changes_when_contract_version_changes()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteHeader(seed: 1, tick: 0, contractVersion: 1).WriteInt32(100);

        var fpV1 = StateFingerprint.Compute(writer, contractVersion: 1);
        var fpV2 = StateFingerprint.Compute(writer, contractVersion: 2);

        Assert.NotEqual(fpV1, fpV2);
        Assert.Equal(1, fpV1.ContractVersion);
        Assert.Equal(2, fpV2.ContractVersion);
    }

    [Fact]
    public void StateFingerprint_is_repeatable_across_identical_computations()
    {
        using var writer1 = new CanonicalStateWriter();
        writer1.WriteHeader(seed: 42, tick: 500).WriteString("State Test").WriteFloat(3.14f);

        using var writer2 = new CanonicalStateWriter();
        writer2.WriteHeader(seed: 42, tick: 500).WriteString("State Test").WriteFloat(3.14f);

        var fp1 = StateFingerprint.Compute(writer1);
        var fp2 = StateFingerprint.Compute(writer2);

        Assert.Equal(fp1, fp2);
        Assert.Equal(fp1.Digest, fp2.Digest);
    }
}
