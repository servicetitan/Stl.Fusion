using System;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public interface IAsyncChannel<T>
    {
        bool IsEmpty { get; }
        bool IsFull { get; }
        bool IsPutCompleted { get; }
        bool CompletePut();
        ValueTask PutAsync(T item, CancellationToken cancellationToken = default);
        ValueTask<Option<T>> PullAsync(CancellationToken cancellationToken = default);
        ValueTask<Option<T>> PullJustLastAsync(CancellationToken cancellationToken = default);
        ValueTask PutAsync(ReadOnlyMemory<T> source, CancellationToken cancellationToken = default);
        ValueTask<int> PullAsync(Memory<T> source, CancellationToken cancellationToken = default);
    }

    public sealed class AsyncChannel<T> : IAsyncChannel<T>
    {
        private readonly Memory<T> _buffer;
        private int _readPosition;
        private int _writePosition;
        private readonly TaskCreationOptions _taskCreationOptions;
        private TaskSource<Unit> _pullHappened;
        private TaskSource<Unit> _putHappened;

        public int Size { get; }
        public int FreeCount => Size - Count;
        public int Count {
            get {
                lock (Lock) {
                    return CountNoLock;
                }
            }
        }
        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == Size;
        public bool IsPutCompleted { get; private set; }

        private int CountNoLock {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                var diff = _writePosition - _readPosition;
                return diff >= 0 ? diff : diff + _buffer.Length;
            }
        }

        private int FreeCountNoLock {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Size - CountNoLock;
        }

        private object Lock => this;

        public AsyncChannel(int size, TaskCreationOptions taskCreationOptions = default)
            : this(new T[size + 1], taskCreationOptions)
        { }

        public AsyncChannel(Memory<T> buffer, TaskCreationOptions taskCreationOptions = default)
        {
            if (buffer.Length <= 1)
                throw Errors.BufferLengthMustBeGreaterThanOne(nameof(buffer));
            Size = buffer.Length - 1; // To make sure that "buffer is full" != "buffer is empty"
            _buffer = buffer;
            _taskCreationOptions = taskCreationOptions;
            _pullHappened = TaskSource.New<Unit>(_taskCreationOptions);
            _putHappened = TaskSource.New<Unit>(_taskCreationOptions);
        }

        public bool CompletePut()
        {
            if (IsPutCompleted)
                return false;
            lock (Lock) {
                if (IsPutCompleted)
                    return false;
                IsPutCompleted = true;
                _putHappened.TrySetResult(default);
                return true;
            }
        }

        public async ValueTask PutAsync(T item, CancellationToken cancellationToken = default)
        {
            while (true) {
                if (FreeCount == 0)
                    await WaitForDequeueAsync(cancellationToken).ConfigureAwait(false);
                lock (Lock) {
                    if (IsPutCompleted)
                        throw Errors.EnqueueCompleted();
                    if (FreeCountNoLock == 0)
                        continue;
                    _buffer.Span[_writePosition] = item;
                    _writePosition = (_writePosition + 1) % _buffer.Span.Length;
                    _putHappened.TrySetResult(default);
                    return;
                }
            }
        }

        public async ValueTask PutAsync(ReadOnlyMemory<T> source, CancellationToken cancellationToken = default)
        {
            while (source.Length > 0) {
                if (FreeCount == 0)
                    await WaitForDequeueAsync(cancellationToken).ConfigureAwait(false);
                lock (Lock) {
                    if (IsPutCompleted)
                        throw Errors.EnqueueCompleted();
                    var availableLength = FreeCountNoLock;
                    if (availableLength == 0)
                        continue;
                    var chunkLength = Math.Min(source.Length, availableLength);
                    var chunk1Length = Math.Min(chunkLength, _buffer.Length - _writePosition);
                    var chunk2Length = chunkLength - chunk1Length;
                    if (chunk1Length > 0) {
                        source.Span.Slice(0, chunk1Length).CopyTo(_buffer.Span.Slice(_writePosition, chunk1Length));
                        source = source.Slice(chunk1Length);
                    }
                    if (chunk2Length > 0) {
                        source.Span.Slice(0, chunk2Length).CopyTo(_buffer.Span.Slice(0, chunk2Length));
                        source = source.Slice(chunk2Length);
                    }
                    _writePosition = (_writePosition + chunkLength) % _buffer.Length;
                    _putHappened.TrySetResult(default);
                }
            }
        }

        public async ValueTask<Option<T>> PullAsync(CancellationToken cancellationToken = default)
        {
            while (true) {
                if (IsEmpty)
                    await WaitForEnqueueAsync(cancellationToken).ConfigureAwait(false);
                lock (Lock) {
                    if (CountNoLock == 0) {
                        if (IsPutCompleted)
                            return Option<T>.None;
                        continue;
                    }
                    var item = _buffer.Span[_readPosition];
                    _readPosition = (_readPosition + 1) % _buffer.Length;
                    _pullHappened.TrySetResult(default);
                    return item!;
                }
            }
        }

        public async ValueTask<Option<T>> PullJustLastAsync(CancellationToken cancellationToken = default)
        {
            while (true) {
                if (IsEmpty)
                    await WaitForEnqueueAsync(cancellationToken).ConfigureAwait(false);
                lock (Lock) {
                    if (CountNoLock == 0) {
                        if (IsPutCompleted)
                            return Option<T>.None;
                        continue;
                    }
                    var item = _buffer.Span[(_writePosition - 1 + _buffer.Length) % _buffer.Length];
                    _readPosition = _writePosition;
                    _pullHappened.TrySetResult(default);
                    return item!;
                }
            }
        }

        public async ValueTask<int> PullAsync(Memory<T> target, CancellationToken cancellationToken = default)
        {
            if (target.Length <= 0)
                throw Errors.BufferLengthMustBeGreaterThanZero(nameof(target));
            var readLength = 0;
            while (target.Length > 0) {
                if (IsEmpty) {
                    if (readLength > 0)
                        // Don't await for enqueue if there is already something to return
                        return readLength;
                    await WaitForEnqueueAsync(cancellationToken).ConfigureAwait(false);
                }
                lock (Lock) {
                    var availableLength = CountNoLock;
                    if (availableLength == 0) {
                        if (IsPutCompleted)
                            return readLength;
                        continue;
                    }
                    var chunkLength = Math.Min(target.Length, availableLength);
                    var chunk1Length = Math.Min(chunkLength, _buffer.Length - readLength);
                    var chunk2Length = chunkLength - chunk1Length;
                    if (chunk1Length > 0) {
                        _buffer.Span.Slice(_readPosition, chunkLength).CopyTo(target.Span);
                        target = target.Slice(chunkLength);
                    }
                    if (chunk2Length > 0) {
                        _buffer.Span.Slice(_readPosition, chunkLength).CopyTo(target.Span);
                        target = target.Slice(chunkLength);
                    }
                    readLength += chunkLength;
                    _readPosition = (_readPosition + chunkLength) % _buffer.Length;
                    _pullHappened.TrySetResult(default);
                }
            }
            return readLength;
        }

        private async Task WaitForDequeueAsync(CancellationToken cancellationToken = default)
        {
            TaskSource<Unit> ts;
            lock (Lock) {
                cancellationToken.ThrowIfCancellationRequested();
                ts = _pullHappened;
                if (ts.Task.IsCompleted && FreeCountNoLock > 0)
                    return;
                ts = TaskSource.New<Unit>(_taskCreationOptions);
                _pullHappened = ts;
            }

            await Task.WhenAny(ts.Task, cancellationToken.ToTask(true)).ConfigureAwait(false);
        }

        private async Task WaitForEnqueueAsync(CancellationToken cancellationToken = default)
        {
            TaskSource<Unit> ts;
            lock (Lock) {
                cancellationToken.ThrowIfCancellationRequested();
                ts = _putHappened;
                if (ts.Task.IsCompleted && (CountNoLock > 0 || IsPutCompleted))
                    return;
                ts = TaskSource.New<Unit>(_taskCreationOptions);
                _putHappened = ts;
            }

            await Task.WhenAny(ts.Task, cancellationToken.ToTask(true)).ConfigureAwait(false);
        }
    }
}
