using System.Buffers.Binary;
using System.Text;
using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Determinism;

public sealed class CanonicalStateWriter : IDisposable
{
    private static readonly byte[] MagicHeader = "WILDSEED"u8.ToArray();
    public const int CurrentSchemaVersion = 1;

    private readonly MemoryStream _stream;

    public CanonicalStateWriter(int initialCapacity = 1024)
    {
        _stream = new MemoryStream(initialCapacity);
    }

    public int Length => (int)_stream.Length;

    public ReadOnlySpan<byte> WrittenSpan => _stream.GetBuffer().AsSpan(0, (int)_stream.Length);

    public CanonicalStateWriter WriteHeader(ulong seed, long tick, int contractVersion = SimulationContract.Version)
    {
        WriteBytes(MagicHeader);
        WriteInt32(CurrentSchemaVersion);
        WriteInt32(contractVersion);
        WriteUInt64(seed);
        WriteInt64(tick);
        return this;
    }

    public CanonicalStateWriter WriteByte(byte value)
    {
        _stream.WriteByte(value);
        return this;
    }

    public CanonicalStateWriter WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _stream.Write(bytes);
        return this;
    }

    public CanonicalStateWriter WriteBoolean(bool value)
    {
        _stream.WriteByte(value ? (byte)0x01 : (byte)0x00);
        return this;
    }

    public CanonicalStateWriter WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteUInt16(ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteUInt32(uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteInt64(long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteUInt64(ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        _stream.Write(buffer);
        return this;
    }

    public CanonicalStateWriter WriteFloat(float value)
    {
        int bits;
        if (float.IsNaN(value))
        {
            bits = 0x7FC00000;
        }
        else if (value == 0.0f)
        {
            bits = 0;
        }
        else
        {
            bits = BitConverter.SingleToInt32Bits(value);
        }

        return WriteInt32(bits);
    }

    public CanonicalStateWriter WriteDouble(double value)
    {
        long bits;
        if (double.IsNaN(value))
        {
            bits = 0x7FF8000000000000L;
        }
        else if (value == 0.0d)
        {
            bits = 0L;
        }
        else
        {
            bits = BitConverter.DoubleToInt64Bits(value);
        }

        return WriteInt64(bits);
    }

    public CanonicalStateWriter WritePresence(bool isPresent)
    {
        return WriteBoolean(isPresent);
    }

    public CanonicalStateWriter WriteString(string? value)
    {
        if (value is null)
        {
            return WriteInt32(-1);
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt32(byteCount);

        if (byteCount > 0)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(value);
            _stream.Write(utf8Bytes, 0, utf8Bytes.Length);
        }

        return this;
    }

    public CanonicalStateWriter WriteCollectionCount(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Collection count cannot be negative.");
        }

        return WriteInt32(count);
    }

    public CanonicalStateWriter WriteOrdered<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> keySelector,
        Action<CanonicalStateWriter, T> writeItem,
        IComparer<TKey>? keyComparer = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(writeItem);

        var list = items.ToList();
        list.Sort((a, b) => (keyComparer ?? Comparer<TKey>.Default).Compare(keySelector(a), keySelector(b)));

        WriteCollectionCount(list.Count);
        foreach (var item in list)
        {
            writeItem(this, item);
        }

        return this;
    }

    public void Reset()
    {
        _stream.SetLength(0);
    }

    public byte[] ToByteArray()
    {
        return _stream.ToArray();
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
