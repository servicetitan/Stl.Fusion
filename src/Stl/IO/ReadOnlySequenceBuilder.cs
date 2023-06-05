using System.Buffers;
using Stl.IO.Internal;

namespace Stl.IO;

public sealed class ReadOnlySequenceBuilder<T>
{
    private ReadOnlyMemorySegment<T>? _first;
    private ReadOnlyMemorySegment<T>? _last;

    public ReadOnlySequence<T> Sequence
        => _last == null
            ? ReadOnlySequence<T>.Empty
            : new ReadOnlySequence<T>(_first!, 0, _last, _last.Memory.Length);

    public ReadOnlySequenceBuilder() { }
    public ReadOnlySequenceBuilder(ReadOnlyMemory<T> buffer)
        => _last = _first = new ReadOnlyMemorySegment<T>(buffer);

    public void Append(ReadOnlyMemory<T> buffer)
    {
        if (_last != null)
            _last = _last.Append(buffer);
        else
            _last = _first = new ReadOnlyMemorySegment<T>(buffer);
    }

    public void Clear()
        => _last = _first = null;
}
