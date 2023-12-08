using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Collections;

public struct RingBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _buffer;
    private int _start;
    private int _end;

    public readonly int Count => (_end - _start) & Capacity;
    public readonly bool IsEmpty => _start == _end;
    public readonly bool IsFull => Count == Capacity;
    public int Capacity { get; }
    public readonly bool IsReadOnly => false;

    public T this[int index] {
        readonly get => _buffer[GetOffset(index)];
        set => _buffer[GetOffset(index)] = value;
    }

    public RingBuffer(int minCapacity)
        : this(new T[Bits.GreaterOrEqualPowerOf2((ulong)Math.Max(1, minCapacity + 1))])
    { }

    public RingBuffer(T[] buffer)
    {
        if (buffer.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(buffer));
        if (!Bits.IsPowerOf2((ulong)buffer.Length))
            throw new ArgumentOutOfRangeException(nameof(buffer));

        _buffer = buffer;
        Capacity = _buffer.Length - 1;
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public readonly IEnumerator<T> GetEnumerator()
    {
        var end = _end;
        if (end < _start)
            end += Capacity;
        for (var i = _start; i < end; i++)
            yield return _buffer[i & Capacity];
    }

    public readonly void GetSpans(out ReadOnlySpan<T> first, out ReadOnlySpan<T> second)
    {
        if (_end >= _start) {
            first = _buffer.AsSpan(_start, _end - _start);
            second = ReadOnlySpan<T>.Empty;
        }
        else {
            first = _buffer.AsSpan(_start);
            second = _buffer.AsSpan(0, _end);
        }
    }

    public readonly T[] ToArray()
    {
        if (Count == 0)
            return Array.Empty<T>();

        var result = new T[Count];
        var resultSpan = result.AsSpan();
        GetSpans(out var span1, out var span2);
        span1.CopyTo(resultSpan);
        span2.CopyTo(resultSpan[span1.Length..]);
        return result;
    }

    public void MoveHead(int skipCount)
    {
        if (skipCount < 0 || skipCount > Count)
            throw new ArgumentOutOfRangeException(nameof(skipCount));

        var newStart = (_start + skipCount) & Capacity;
        if (newStart >= _start)
            _buffer.AsSpan(_start, newStart - _start).Clear();
        else {
            _buffer.AsSpan(_start).Clear();
            _buffer.AsSpan(0, newStart).Clear();
        }
        _start = newStart;
    }

    public T PullHead()
        => TryPullHead(out var head) ? head! : throw Errors.CollectionIsEmpty();

#if NETSTANDARD2_0
    public bool TryPullHead(out T head)
#else
    public bool TryPullHead([MaybeNullWhen(false)] out T head)
#endif
    {
        if (IsEmpty) {
            head = default!;
            return false;
        }

        ref var headRef = ref _buffer[_start];
        head = headRef;
        headRef = default!;
        _start = (_start + 1) & Capacity;
        return true;
    }

    public T PullTail()
        => TryPullTail(out var tail) ? tail! : throw Errors.CollectionIsEmpty();

#if NETSTANDARD2_0
    public bool TryPullTail(out T tail)
#else
    public bool TryPullTail([MaybeNullWhen(false)] out T tail)
#endif
    {
        if (IsEmpty) {
            tail = default!;
            return false;
        }

        _end = (_end - 1) & Capacity;
        ref var tailRef = ref _buffer[_end];
        tail = tailRef;
        tailRef = default!;
        return true;
    }

    // Mutations

    public void PushHead(T head)
    {
        AssertNotFull();
        _start = (_start - 1) & Capacity;
        _buffer[_start] = head;
    }

    public void PushHeadAndMoveTailIfFull(T head)
    {
        if (IsFull) {
            _end = (_end - 1) & Capacity;
            _buffer[_end] = default!;
        }
        _start = (_start - 1) & Capacity;
        _buffer[_start] = head;
    }

    public void PushTail(T tail)
    {
        AssertNotFull();
        _buffer[_end] = tail;
        _end = (_end + 1) & Capacity;
    }

    public void PushTailAndMoveHeadIfFull(T tail)
    {
        if (IsFull) {
            _buffer[_start] = default!;
            _start = (_start + 1) & Capacity;
        }
        _buffer[_end] = tail;
        _end = (_end + 1) & Capacity;
    }

    public void Clear()
    {
        _end = _start = 0;
        _buffer.AsSpan().Clear();
    }

    // Private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void AssertNotEmpty()
    {
        if (Count == 0)
            throw Errors.CollectionIsEmpty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void AssertNotFull()
    {
        if (Count == Capacity)
            throw Errors.CollectionIsFull();
    }

    private readonly int GetOffset(int index)
        => index < 0 || index >= Count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : (_start + index) & Capacity;
}
