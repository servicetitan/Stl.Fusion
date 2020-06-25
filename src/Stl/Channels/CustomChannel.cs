using System.Threading.Channels;

namespace Stl.Channels
{
    public class CustomChannel<TWrite, TRead> : Channel<TWrite, TRead>
    {
        public CustomChannel(ChannelReader<TRead> reader, ChannelWriter<TWrite> writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }

    public class CustomChannel<T> : Channel<T>
    {
        public CustomChannel(ChannelReader<T> reader, ChannelWriter<T> writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }

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
}
