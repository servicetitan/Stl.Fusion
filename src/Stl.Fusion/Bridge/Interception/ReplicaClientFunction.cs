using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Interception.Internal;
using Stl.Fusion.Internal;
using Stl.Generators;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaClientFunction<T> : InterceptedFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly bool IsLogDebugEnabled;
        protected readonly Generator<LTag> VersionGenerator;
        protected readonly IReplicator Replicator;

        public ReplicaClientFunction(
            InterceptedMethod method,
            IReplicator replicator,
            Generator<LTag> versionGenerator,
            ILogger<ReplicaClientFunction<T>>? log = null)
            : base(((IReplicatorImpl) replicator).ServiceProvider, method)
        {
            Log = log ??= NullLogger<ReplicaClientFunction<T>>.Instance;
            IsLogDebugEnabled = Log.IsEnabled(LogLevel.Debug);
            VersionGenerator = versionGenerator;
            Replicator = replicator;
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var method = input.Method;
            IReplica<T> replica;
            IReplicaComputed<T> replicaComputed;

            // 1. Trying to update the Replica first
            if (existing is IReplicaClientComputed<T> rsc && rsc.Replica != null) {
                try {
                    replica = rsc.Replica;
                    replicaComputed = (IReplicaComputed<T>) await replica.Computed
                        .UpdateAsync(true, cancellationToken).ConfigureAwait(false);
                    return new ReplicaClientComputed<T>(method.Options, input, replicaComputed);
                }
                catch (OperationCanceledException) {
                    if (IsLogDebugEnabled)
                        Log.LogDebug($"{nameof(ComputeAsync)}: Cancelled (1).");
                    throw;
                }
                catch (Exception e) {
                    if (IsLogDebugEnabled)
                        Log.LogError(e, $"{nameof(ComputeAsync)}: Error on Replica update.");
                }
            }

            // 2. Replica update failed, let's refresh it
            using var psiCapture = new PublicationStateInfoCapture();
            Result<T> output;
            try {
                var result = input.InvokeOriginalFunction(cancellationToken);
                if (method.ReturnsValueTask) {
                    var task = (ValueTask<T>) result;
                    output = new Result<T>(await task.ConfigureAwait(false), null);
                }
                else {
                    var task = (Task<T>) result;
                    output = new Result<T>(await task.ConfigureAwait(false), null);
                }
            }
            catch (OperationCanceledException) {
                if (IsLogDebugEnabled)
                    Log.LogDebug($"{nameof(ComputeAsync)}: Cancelled (2).");
                throw;
            }
            catch (Exception e) {
                if (IsLogDebugEnabled)
                    Log.LogError(e, $"{nameof(ComputeAsync)}: Error on update.");
                output = new Result<T>(default!, e);
            }

            var psi = psiCapture.Captured;
            if (psi == null) {
                output = new Result<T>(default!, Errors.NoPublicationStateInfoCaptured());
                // We need a unique LTag here, so we use a range that's supposed to be unused by LTagGenerators.
                var version = new LTag(VersionGenerator.Next().Value ^ (1L << 62));
                return new ReplicaClientComputed<T>(method.Options, input, output.Error!, version);
            }
            if (output.HasError)
                // We need a unique LTag here, so we use a range that's supposed to be unused by LTagGenerators.
                psi.Version = new LTag(VersionGenerator.Next().Value ^ (1L << 62));
            replica = Replicator.GetOrAdd(new PublicationStateInfo<T>(psi, output));
            replicaComputed = (IReplicaComputed<T>) await replica.Computed
                .UpdateAsync(true, cancellationToken).ConfigureAwait(false);
            return new ReplicaClientComputed<T>(method.Options, input, replicaComputed);
        }
    }
}
