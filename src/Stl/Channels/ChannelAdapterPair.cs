using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Channels
{
    public class ChannelAdapterPair<TIn, TOut> : AsyncProcessBase
    {
        public ChannelAdapter<TIn, TOut> ForwardAdapter { get; }
        public ChannelAdapter<TIn, TOut> BackwardAdapter { get; }

        protected ChannelAdapterPair(ChannelAdapter<TIn, TOut> forwardAdapter, ChannelAdapter<TIn, TOut> backwardAdapter)
        {
            ForwardAdapter = forwardAdapter;
            BackwardAdapter = backwardAdapter;
        }

        protected override Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var forwardTask = ForwardAdapter.RunAsync();
            var backwardTask = BackwardAdapter.RunAsync();
            return Task.WhenAll(forwardTask, backwardTask);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            var fTask = ForwardAdapter?.DisposeAsync() ?? ValueTaskEx.CompletedTask;
            var bTask = BackwardAdapter?.DisposeAsync() ?? ValueTaskEx.CompletedTask;
            await fTask.ConfigureAwait(false);
            await bTask.ConfigureAwait(false);
            await base.DisposeInternalAsync(disposing);
        }
    }

    public class ChannelAdapterPair<T> : ChannelAdapterPair<T, T>
    {
        protected ChannelAdapterPair(ChannelAdapter<T, T> forwardAdapter, ChannelAdapter<T, T> backwardAdapter) 
            : base(forwardAdapter, backwardAdapter) 
        { }
    }
}
