namespace Stl.Channels;

public interface IEmptyChannel;

public class EmptyChannel<T> : Channel<T, T>, IEmptyChannel
{
    public static readonly EmptyChannel<T> Instance = new();

    private EmptyChannel()
    {
        var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
            SingleReader = false,
            SingleWriter = false,
        });
        channel.Writer.TryComplete();
        Reader = channel.Reader;
        Writer = channel.Writer;
    }
}
