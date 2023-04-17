namespace Stl.Channels;

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
