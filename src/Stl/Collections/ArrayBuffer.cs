using System.Buffers;
using Stl.Mathematics;

namespace Stl.Collections;

// List-like struct that typically requires zero allocations
// (it relies on ArrayPool<T>.Shared & disposes its buffer);
// it is supposed to be used as a temp. buffer in various
// enumeration scenarios.
// ArrayBuffer<T> vs MemoryBuffer<T>: they are almost identical, but
// ArrayBuffer isn't a ref struct, so you can store it in fields.
public struct ArrayBuffer<T> : IDisposable
{
    public const int MinCapacity = 8;
    public const int MaxCapacity = 1 << 30;
    private static readonly ArrayPool<T> Pool = ArrayPool<T>.Shared;

    private int _count;

    public T[] Buffer { get; private set; }
    public Span<T> Span => Buffer.AsSpan(0, Count);
    public int Capacity => Buffer.Length;
    public bool MustClean { get; }
    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if (value < 0 || value > Capacity)
                throw new ArgumentOutOfRangeException(nameof(value));
            _count = value;
        }
    }

    public T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable MA0012
        get => index < Count ? Buffer[index] : throw new IndexOutOfRangeException();
#pragma warning restore MA0012
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ArrayBuffer(bool mustClean, int capacity)
    {
        MustClean = mustClean;
        capacity = ComputeCapacity(capacity, MinCapacity);
        Buffer = Pool.Rent(capacity);
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayBuffer<T> Lease(bool mustClean, int capacity = MinCapacity)
        => new ArrayBuffer<T>(mustClean, capacity);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayBuffer<T> LeaseAndSetCount(bool mustClean, int count)
        => new ArrayBuffer<T>(mustClean, count) {Count = count};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (Buffer != null)
            Pool.Return(Buffer, MustClean);
        Buffer = null!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray() => Span.ToArray();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<T> ToList()
    {
        var list = new List<T>(Count);
        foreach (var item in Span)
            list.Add(item);
        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetItem(int index, T item)
    {
        if (index >= Count)
#pragma warning disable MA0015
            throw new IndexOutOfRangeException();
#pragma warning restore MA0015
        Buffer[index] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IReadOnlyCollection<T> items)
    {
        EnsureCapacity(Count + items.Count);
        foreach (var item in items)
            Add(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (Count >= Capacity)
            EnsureCapacity(Count + 1);
        Buffer[Count++] = item;
    }

    public void Insert(int index, T item)
    {
        if (Count >= Capacity)
            EnsureCapacity(Count + 1);
        var copyLength = Count - index;
        if (copyLength < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        var span = Buffer.AsSpan(0, ++Count);
        var source = span.Slice(index, copyLength);
        var target = span.Slice(index + 1);
        source.CopyTo(target);
        span[index] = item;
    }

    public void RemoveAt(int index)
    {
        var copyLength = Count - index - 1;
        if (copyLength < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        var span = Buffer.AsSpan(0, Count--);
        var source = span.Slice(index + 1, copyLength);
        var target = span.Slice(index);
        source.CopyTo(target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        ChangeLease(Pool.Rent(MinCapacity));
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array, int arrayIndex)
        => Buffer.CopyTo(array.AsSpan().Slice(arrayIndex));

    public void EnsureCapacity(int capacity)
    {
        capacity = ComputeCapacity(capacity, Capacity);
        var newLease = Pool.Rent(capacity);
        Span.CopyTo(newLease.AsSpan());
        ChangeLease(newLease);
    }

    // Private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeCapacity(int requestedCapacity, int minCapacity)
    {
        if (requestedCapacity < minCapacity)
            requestedCapacity = minCapacity;
        else if (requestedCapacity > MaxCapacity)
            throw new ArgumentOutOfRangeException(nameof(requestedCapacity));
        return (int) Bits.GreaterOrEqualPowerOf2((uint) requestedCapacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ChangeLease(T[] newLease)
    {
        Pool.Return(Buffer, MustClean);
        Buffer = newLease;
    }
}
