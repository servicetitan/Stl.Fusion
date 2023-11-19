using System.Buffers;

namespace Stl.Collections;

// List-like struct that typically requires zero allocations
// (it relies on MemoryPool<T>.Shared & disposes its buffer);
// it is supposed to be used as a temp. buffer in various
// enumeration scenarios.
// ArrayBuffer<T> vs MemoryBuffer<T>: they are almost identical, but
// ArrayBuffer isn't a ref struct, so you can store it in fields.
public ref struct MemoryBuffer<T>
{
    public const int MinCapacity = 8;
    public const int MaxCapacity = 1 << 30;
    private static readonly MemoryPool<T> Pool = MemoryPool<T>.Shared;

    private IMemoryOwner<T> _lease;
    private int _count;

    public Memory<T> BufferMemory => _lease.Memory;
    public Span<T> BufferSpan { get; private set; }
    public Span<T> Span => BufferSpan[..Count];
    public bool MustClean { get; }
    public int Capacity => BufferSpan.Length;
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
        get => index < Count
            ? BufferSpan[index]
#pragma warning disable MA0012
#pragma warning disable CA2201
            : throw new IndexOutOfRangeException();
#pragma warning restore CA2201
#pragma warning restore MA0012
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if (index >= Count)
#pragma warning disable MA0012
#pragma warning disable CA2201
                throw new IndexOutOfRangeException();
#pragma warning restore CA2201
#pragma warning restore MA0012
            BufferSpan[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MemoryBuffer(bool mustClean, int capacity)
    {
        MustClean = mustClean;
        capacity = ComputeCapacity(capacity, MinCapacity);
        _lease = Pool.Rent(capacity);
        _count = 0;
        BufferSpan = _lease.Memory.Span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBuffer<T> Lease(bool mustClean, int capacity = MinCapacity)
        => new(mustClean, capacity);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryBuffer<T> LeaseAndSetCount(bool mustClean, int count)
        => new(mustClean, count) { Count = count };

    public void Release()
    {
        if (MustClean)
            _lease.Memory.Span.Clear();
        _lease?.Dispose();
        _lease = null!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray() => Span.ToArray();

    public List<T> ToList()
    {
        var list = new List<T>(Count);
        foreach (var item in Span)
            list.Add(item);
        return list;
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    public void AddRange(IReadOnlyCollection<T> items)
    {
        EnsureCapacity(Count + items.Count);
        foreach (var item in items)
            Add(item);
    }

    public void AddSpan(ReadOnlySpan<T> span)
    {
        EnsureCapacity(_count + span.Length);
        span.CopyTo(BufferSpan[_count..]);
        _count += span.Length;
    }

    public void Add(T item)
    {
        if (Count >= Capacity)
            EnsureCapacity(Count + 1);
        BufferSpan[Count++] = item;
    }

    public void Insert(int index, T item)
    {
        if (Count >= Capacity)
            EnsureCapacity(Count + 1);
        var copyLength = Count - index;
        if (copyLength < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        var span = BufferSpan[..++Count];
        var source = span.Slice(index, copyLength);
        var target = span[(index + 1)..];
        source.CopyTo(target);
        span[index] = item;
    }

    public void RemoveAt(int index)
    {
        var copyLength = Count - index - 1;
        if (copyLength < 0)
            throw new ArgumentOutOfRangeException(nameof(index));
        var span = BufferSpan[..Count--];
        var source = span.Slice(index + 1, copyLength);
        var target = span[index..];
        source.CopyTo(target);
    }

    public void Clear()
    {
        ChangeLease(Pool.Rent(MinCapacity));
        Count = 0;
    }

    public void CopyTo(T[] array, int arrayIndex)
        => BufferSpan.CopyTo(array.AsSpan(arrayIndex));

    public void EnsureCapacity(int capacity)
    {
        capacity = ComputeCapacity(capacity, Capacity);
        var newLease = Pool.Rent(capacity);
        Span.CopyTo(newLease.Memory.Span);
        ChangeLease(newLease);
    }

    // Private methods

    private static int ComputeCapacity(int capacity, int minCapacity)
    {
        if (capacity < minCapacity)
            capacity = minCapacity;
        else if (capacity > MaxCapacity)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        return (int) Bits.GreaterOrEqualPowerOf2((uint) capacity);
    }

    private void ChangeLease(IMemoryOwner<T> newLease)
    {
        if (MustClean)
            _lease.Memory.Span.Clear();
        _lease.Dispose();
        _lease = newLease;
        BufferSpan = _lease.Memory.Span;
    }
}
