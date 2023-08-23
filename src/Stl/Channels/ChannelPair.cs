namespace Stl.Channels;

public class ChannelPair<T>(Channel<T> channel1, Channel<T> channel2)
{
    public static readonly ChannelPair<T> Null = new(NullChannel<T>.Instance, NullChannel<T>.Instance);

    public Channel<T> Channel1 { get; protected init; } = channel1;
    public Channel<T> Channel2 { get; protected init; } = channel2;

    protected ChannelPair() : this(null!, null!) { }
}

public static class ChannelPair
{
    public static ChannelPair<T> Create<T>(Channel<T> channel1, Channel<T> channel2)
        => new(channel1, channel2);

    public static ChannelPair<T> CreateTwisted<T>(Channel<T> channel1, Channel<T> channel2)
        => new(
            new CustomChannel<T>(channel1.Reader, channel2.Writer),
            new CustomChannel<T>(channel2.Reader, channel1.Writer));
}
