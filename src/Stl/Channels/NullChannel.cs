using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Channels
{
    public class NullChannel<T> : Channel<T, T>
    {
        public static readonly NullChannel<T> Instance = new NullChannel<T>();

        private class NullChannelReader : ChannelReader<T>
        {
            public override Task Completion => TaskEx.InfiniteUnitTask;

            public override bool TryRead(out T item)
            {
                item = default!;
                return false;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new CancellationToken()) 
                => ValueTaskEx.FalseTask;
        }

        private class NullChannelWriter : ChannelWriter<T>
        {
            public override bool TryComplete(Exception? error = null) 
                => false;

            public override bool TryWrite(T item) 
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
