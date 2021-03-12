using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping
{
    public class NoSwapService : ISwapService
    {
        public static ISwapService Instance { get; } = new NoSwapService();

        public ValueTask<IResult?> Load((ComputeMethodInput Input, LTag Version) key, CancellationToken cancellationToken = default)
            => ValueTaskEx.FromResult((IResult?) null);

        public ValueTask Store((ComputeMethodInput Input, LTag Version) key, IResult value,
            CancellationToken cancellationToken = default)
            => ValueTaskEx.CompletedTask;
    }
}
