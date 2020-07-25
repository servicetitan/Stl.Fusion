using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Concurrency;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaServiceFunction<T> : InterceptedFunctionBase<T>
    {
        private readonly ILogger _log;
        private readonly bool _isLogDebugEnabled;
        protected Generator<LTag> LTagGenerator { get; }

        public ReplicaServiceFunction(
            InterceptedMethod method,
            Generator<LTag> lTagGenerator,
            IComputedRegistry computedRegistry,
            ILogger<ReplicaServiceFunction<T>>? log = null)
            : base(method, computedRegistry)
        {
            _log = log ??= NullLogger<ReplicaServiceFunction<T>>.Instance;
            _isLogDebugEnabled = _log.IsEnabled(LogLevel.Debug);
            LTagGenerator = lTagGenerator;
            InvalidatedHandler = null;
        }

        public override IComputed<T>? TryGetCached(InterceptedInput input, IComputed? usedBy)
        {
            if (!(ComputedRegistry.TryGet(input) is IReplicaServiceComputed<T> computed))
                return null;
            var replica = computed.Replica;
            if (replica == null || replica.UpdateError != null || replica.DisposalState != DisposalState.Active) {
                ComputedRegistry.Remove(computed);
                return null;
            }
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
            return computed;
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? cached,
            CancellationToken cancellationToken)
        {
            var method = input.Method;

            // 1. Trying to update the Replica first
            if (cached is IReplicaServiceComputed<T> rsc && rsc.Replica != null) {
                try {
                    var replica = rsc.Replica;
                    var computed = await replica.Computed
                        .UpdateAsync(true, cancellationToken).ConfigureAwait(false);
                    var replicaComputed = (IReplicaComputed<T>) computed;
                    var output = new ReplicaServiceComputed<T>(
                        method.Options, replicaComputed, input);
                    return output;
                }
                catch (OperationCanceledException) {
                    if (_isLogDebugEnabled)
                        _log.LogDebug($"{nameof(ComputeAsync)}: Cancelled (1).");
                    throw;
                }
                catch (Exception e) {
                    if (_isLogDebugEnabled)
                        _log.LogError(e, $"{nameof(ComputeAsync)}: Error on Replica update.");
                }
            }

            // 2. Replica update failed, let's refresh it
            try {
                using var replicaCapture = new ReplicaCapture();
                var result = input.InvokeOriginalFunction(cancellationToken);
                if (method.ReturnsComputed) {
                    if (method.ReturnsValueTask) {
                        var task = (ValueTask<IComputed<T>>) result;
                        await task.ConfigureAwait(false);
                    }
                    else {
                        var task = (Task<IComputed<T>>) result;
                        await task.ConfigureAwait(false);
                    }
                }
                else {
                    if (method.ReturnsValueTask) {
                        var task = (ValueTask<T>) result;
                        await task.ConfigureAwait(false);
                    }
                    else {
                        var task = (Task<T>) result;
                        await task.ConfigureAwait(false);
                    }
                }
                var replica = replicaCapture.GetCapturedReplica<T>();
                var computed = await replica.Computed
                    .UpdateAsync(true, cancellationToken).ConfigureAwait(false);
                var replicaComputed = (IReplicaComputed<T>) computed;
                var output = new ReplicaServiceComputed<T>(
                    method.Options, replicaComputed, input);
                return output;
            }
            catch (OperationCanceledException) {
                if (_isLogDebugEnabled)
                    _log.LogDebug($"{nameof(ComputeAsync)}: Cancelled (2).");
                throw;
            }
            catch (Exception e) {
                if (_isLogDebugEnabled)
                    _log.LogError(e, $"{nameof(ComputeAsync)}: Error on update.");
                // We need a unique LTag here, so we use a range that's supposed
                // to be unused by LTagGenerators.
                var lTag = new LTag(LTagGenerator.Next().Value ^ (1L << 62));
                var output = new ReplicaServiceComputed<T>(
                    method.Options, null, input, new Result<T>(default!, e), lTag);
                return output;
            }
        }
    }
}
