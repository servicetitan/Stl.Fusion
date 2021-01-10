using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public abstract class ComputeMethodFunctionBase<T> : ComputeFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;

        public ComputeMethodFunctionBase(
            ComputeMethodDef method,
            Generator<LTag> versionGenerator,
            IServiceProvider services,
            ILogger<ComputeMethodFunction<T>>? log = null)
            : base(method, services)
        {
            Log = log ??= NullLogger<ComputeMethodFunction<T>>.Instance;
            VersionGenerator = versionGenerator;
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            ComputeMethodInput input, IComputed<T>? existing,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var computed = CreateComputed(input, tag);
            try {
                using var _ = Computed.ChangeCurrent(computed);
                var result = input.InvokeOriginalFunction(cancellationToken);
                if (input.Method.ReturnsValueTask) {
                    var output = await ((ValueTask<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
                }
                else {
                    var output = await ((Task<T>) result).ConfigureAwait(false);
                    computed.TrySetOutput(output);
                }
            }
            catch (OperationCanceledException e) {
                computed.TrySetOutput(Result.Error<T>(e));
                computed.Invalidate();
                throw;
            }
            catch (Exception e) {
                if (e is AggregateException ae)
                    e = ae.GetFirstInnerException();
                computed.TrySetOutput(Result.Error<T>(e));
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            return computed;
        }

        protected abstract IComputed<T> CreateComputed(ComputeMethodInput input, LTag tag);
    }
}
