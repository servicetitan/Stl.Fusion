using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping
{
    public class FakeSwapService : ISwapService
    {
        public ValueTask<IResult?> LoadAsync((InterceptedInput Input, LTag Version) key, CancellationToken cancellationToken = default)
            => ValueTaskEx.FromResult((IResult?) null);

        public ValueTask StoreOrRenewAsync((InterceptedInput Input, LTag Version) key, IResult value,
            CancellationToken cancellationToken = default)
            => ValueTaskEx.CompletedTask;
    }
}
