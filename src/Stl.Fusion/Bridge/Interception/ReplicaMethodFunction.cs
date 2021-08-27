using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;
using Stl.Generators;
using Stl.Versioning;

namespace Stl.Fusion.Bridge.Interception
{
    public class ReplicaMethodFunction<T> : ComputeFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly bool IsLogDebugEnabled;
        protected readonly VersionGenerator<LTag> VersionGenerator;
        protected readonly IReplicator Replicator;

        public ReplicaMethodFunction(
            ComputeMethodDef method,
            IReplicator replicator,
            VersionGenerator<LTag> versionGenerator,
            ILogger<ReplicaMethodFunction<T>>? log = null)
            : base(method, ((IReplicatorImpl) replicator).Services)
        {
            Log = log ?? NullLogger<ReplicaMethodFunction<T>>.Instance;
            IsLogDebugEnabled = Log.IsEnabled(LogLevel.Debug);
            VersionGenerator = versionGenerator;
            Replicator = replicator;
        }

        protected override async ValueTask<IComputed<T>> Compute(
            ComputeMethodInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var method = input.Method;
            IReplica<T> replica;
            IReplicaComputed<T> replicaComputed;
            ReplicaMethodComputed<T> result;

            // 1. Trying to update the Replica first
            if (existing is IReplicaMethodComputed<T> rsc && rsc.Replica != null) {
                try {
                    replica = rsc.Replica;
                    replicaComputed = (IReplicaComputed<T>)
                        await replica.Computed.Update(cancellationToken).ConfigureAwait(false);
                    result = new (method.Options, input, replicaComputed);
                    ComputeContext.Current.TryCapture(result);
                    return result;
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    if (IsLogDebugEnabled)
                        Log.LogError(e, "ComputeAsync: error on Replica update");
                }
            }

            // 2. Replica update failed, let's refresh it
            Result<T> output;
            PublicationStateInfo? psi;
            using (var psiCapture = new PublicationStateInfoCapture()) {
                try {
                    var rpcResult = input.InvokeOriginalFunction(cancellationToken);
                    if (method.ReturnsValueTask) {
                        var task = (ValueTask<T>) rpcResult;
                        output = Result.Value(await task.ConfigureAwait(false));
                    }
                    else {
                        var task = (Task<T>) rpcResult;
                        output = Result.Value(await task.ConfigureAwait(false));
                    }
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    if (IsLogDebugEnabled)
                        Log.LogError(e, "ComputeAsync: error on update");
                    if (e is AggregateException ae)
                        e = ae.GetFirstInnerException();
                    output = Result.Error<T>(e);
                }
                psi = psiCapture.Captured;
            }

            if (psi == null) {
                output = new Result<T>(default!, Errors.NoPublicationStateInfoCaptured());
                // We need a unique LTag here, so we use a range that's supposed to be unused by LTagGenerators.
                var version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
                result = new (method.Options, input, output.Error!, version);
                ComputeContext.Current.TryCapture(result);
                return result;
            }
            if (output.HasError) {
                // Try to pull the actual error first
                var errorPsi = (PublicationStateInfo<object>) psi;
                if (errorPsi.Output.HasError)
                    output = Result.Error<T>(errorPsi.Output.Error!);
                // We need a unique LTag here, so we use a range that's supposed
                // to be unused by LTagGenerators.
                if (psi.Version == default)
                    psi.Version = new LTag(VersionGenerator.NextVersion().Value ^ (1L << 62));
            }
            replica = Replicator.GetOrAdd(new PublicationStateInfo<T>(psi, output));
            replicaComputed = (IReplicaComputed<T>)
                await replica.Computed.Update(cancellationToken).ConfigureAwait(false);
            result = new ReplicaMethodComputed<T>(method.Options, input, replicaComputed);
            ComputeContext.Current.TryCapture(result);
            return result;
        }
    }
}
