using System.Threading.Channels;
using Stl.Async;

namespace Stl.Channels
{
    public abstract class ChannelAdapter<TIn, TOut> : AsyncProcessBase
    {
        public ChannelReader<TIn> Reader { get; }
        public ChannelWriter<TOut> Writer { get; }

        protected ChannelAdapter(ChannelReader<TIn> reader, ChannelWriter<TOut> writer)
        {
            Reader = reader;
            Writer = writer;
        }
    }

    public abstract class ChannelAdapter<T> : ChannelAdapter<T, T>
    {
        protected ChannelAdapter(ChannelReader<T> reader, ChannelWriter<T> writer) 
            : base(reader, writer) 
        { }
    }
}
