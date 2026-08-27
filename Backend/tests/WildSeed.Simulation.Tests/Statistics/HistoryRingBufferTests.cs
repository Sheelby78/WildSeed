using WildSeed.Simulation.Statistics;
using Xunit;

namespace WildSeed.Simulation.Tests.Statistics;

public sealed class HistoryRingBufferTests
{
    [Fact]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryRingBuffer<int>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryRingBuffer<int>(-5));
    }

    [Fact]
    public void Add_UnderCapacity_AppendsInOrder()
    {
        var buffer = new HistoryRingBuffer<int>(5);
        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.Equal(3, buffer.Count);
        Assert.Equal(5, buffer.Capacity);
        Assert.Equal(10, buffer[0]);
        Assert.Equal(20, buffer[1]);
        Assert.Equal(30, buffer[2]);
        Assert.Equal([10, 20, 30], buffer.ToArray());
    }

    [Fact]
    public void Add_ExceedingCapacity_OverwritesOldestAndMaintainsOrder()
    {
        var buffer = new HistoryRingBuffer<int>(3);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        Assert.Equal(3, buffer.Count);
        Assert.Equal([1, 2, 3], buffer.ToArray());

        buffer.Add(4);
        Assert.Equal(3, buffer.Count);
        Assert.Equal(2, buffer[0]);
        Assert.Equal(3, buffer[1]);
        Assert.Equal(4, buffer[2]);
        Assert.Equal([2, 3, 4], buffer.ToArray());

        buffer.Add(5);
        buffer.Add(6);
        Assert.Equal(3, buffer.Count);
        Assert.Equal([4, 5, 6], buffer.ToArray());
    }

    [Fact]
    public void Indexer_OutOfBounds_ThrowsArgumentOutOfRangeException()
    {
        var buffer = new HistoryRingBuffer<string>(3);
        buffer.Add("a");

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[3]);
    }

    [Fact]
    public void Clear_ResetsCountAndPreservesCapacity()
    {
        var buffer = new HistoryRingBuffer<int>(4);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);
        buffer.Add(5);

        Assert.Equal(4, buffer.Count);
        buffer.Clear();

        Assert.Empty(buffer);
        Assert.Equal(4, buffer.Capacity);

        buffer.Add(42);
        Assert.Single(buffer);
        Assert.Equal(42, buffer[0]);
    }
}
