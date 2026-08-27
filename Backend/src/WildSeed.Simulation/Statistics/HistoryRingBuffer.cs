using System.Collections;

namespace WildSeed.Simulation.Statistics;

public sealed class HistoryRingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _count;

    public HistoryRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        _buffer = new T[capacity];
    }

    public int Capacity { get; }

    public int Count => _count;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non-negative and less than the size of the buffer.");
            }

            int actualIndex = (_start + index) % Capacity;
            return _buffer[actualIndex];
        }
    }

    public void Add(T item)
    {
        if (_count < Capacity)
        {
            int insertIndex = (_start + _count) % Capacity;
            _buffer[insertIndex] = item;
            _count++;
        }
        else
        {
            _buffer[_start] = item;
            _start = (_start + 1) % Capacity;
        }
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _start = 0;
        _count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
