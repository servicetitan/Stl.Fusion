namespace Stl.Channels;

public class NullChannel<T> : Channel<T>
{
    public static readonly NullChannel<T> Instance = new();

    private sealed class NullChannelReader : ChannelReader<T>
    {
        public override Task Completion => TaskExt.NeverEndingUnitTask;

        public override bool TryRead(out T item)
        {
            item = default!;
            return false;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new CancellationToken())
            => ValueTaskExt.FalseTask;
    }

    private sealed class NullChannelWriter : ChannelWriter<T>
    {
        public override bool TryComplete(Exception? error = null)
            => false;

        public override bool TryWrite(T item)
            => true;

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = new CancellationToken())
            => ValueTaskExt.TrueTask;
    }

    private NullChannel()
    {
        Reader = new NullChannelReader();
        Writer = new NullChannelWriter();
    }
}
