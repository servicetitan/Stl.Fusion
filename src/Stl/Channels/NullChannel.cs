using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Channels
{
    public class NullChannel<TMessage> : Channel<TMessage, TMessage>
    {
        public static readonly NullChannel<TMessage> Instance = new NullChannel<TMessage>();

        private class NullChannelReader : ChannelReader<TMessage>
        {
            public override Task Completion => TaskEx.InfiniteUnitTask;

            public override bool TryRead(out TMessage item)
            {
                item = default!;
                return false;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new CancellationToken()) 
                => ValueTaskEx.FalseTask;
        }

        private class NullChannelWriter : ChannelWriter<TMessage>
        {
            public override bool TryComplete(Exception? error = null) 
                => false;

            public override bool TryWrite(TMessage item) 
                => true;
            
            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = new CancellationToken()) 
                => ValueTaskEx.TrueTask;
        }

        private NullChannel()
        {
            Reader = new NullChannelReader(); 
            Writer = new NullChannelWriter(); 
        }
    }
}
