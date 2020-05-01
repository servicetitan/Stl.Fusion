using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Publication
{
    public interface IRenewalPolicy
    {
        ValueTask<bool> RenewAsync(IComputed computed, CancellationToken cancellationToken);
    }

    public abstract class RenewalPolicyBase : IRenewalPolicy
    {
        public abstract ValueTask<bool> RenewAsync(IComputed computed, CancellationToken cancellationToken);
    }

    public class RenewalPolicy : RenewalPolicyBase
    {
        public static readonly IRenewalPolicy Always = new RenewalPolicy((c, ct) => ValueTaskEx.TrueTask);
        public static readonly IRenewalPolicy Never = new RenewalPolicy((c, ct) => ValueTaskEx.FalseTask);

        private readonly Func<IComputed, CancellationToken, ValueTask<bool>> _handler;

        public RenewalPolicy(Func<IComputed, CancellationToken, ValueTask<bool>> handler) 
            => _handler = handler;

        public override ValueTask<bool> RenewAsync(IComputed computed, CancellationToken cancellationToken) 
            => _handler.Invoke(computed, cancellationToken);
    }
}
