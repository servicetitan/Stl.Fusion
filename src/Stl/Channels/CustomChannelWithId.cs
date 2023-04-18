namespace Stl.Channels;

public class CustomChannelWithId<TId, TWrite, TRead> : Channel<TWrite, TRead>, IHasId<TId>
{
    public TId Id { get; }

    public CustomChannelWithId(TId id, ChannelReader<TRead> reader, ChannelWriter<TWrite> writer)
    {
        Id = id;
        Reader = reader;
        Writer = writer;
    }
}

public class CustomChannelWithId<TId, T> : Channel<T>, IHasId<TId>
{
    public TId Id { get; }

    public CustomChannelWithId(TId id, ChannelReader<T> reader, ChannelWriter<T> writer)
    {
        Id = id;
        Reader = reader;
        Writer = writer;
    }
}
