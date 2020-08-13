using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Interception.Internal;
using Stl.Generators;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : InterceptedFunctionBase<T>
    {
        protected readonly ILogger Log;
        protected readonly Generator<LTag> VersionGenerator;

        public ComputeServiceFunction(
            InterceptedMethod method,
            Generator<LTag> versionGenerator,
            IComputedRegistry computedRegistry,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(method, computedRegistry)
        {
            Log = log ??= NullLogger<ComputeServiceFunction<T>>.Instance;
            VersionGenerator = versionGenerator;
        }

        protected override async ValueTask<IComputed<T>> ComputeAsync(
            InterceptedInput input, IComputed<T>? cached,
            CancellationToken cancellationToken)
        {
            var tag = VersionGenerator.Next();
            var method = Method;
            var output = new Computed<InterceptedInput, T>(method.Options, input, tag);
            try {
                using var _ = Computed.ChangeCurrent(output);
                var resultTask = input.InvokeOriginalFunction(cancellationToken);
                if (method.ReturnsValueTask) {
                    var task = (ValueTask<T>) resultTask;
                    var value = await task.ConfigureAwait(false);
                    output.TrySetOutput(value!);
                }
                else {
                    var task = (Task<T>) resultTask;
                    var value = await task.ConfigureAwait(false);
                    output.TrySetOutput(value!);
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                output.TrySetOutput(Result.Error<T>(e));
                // Weird case: if the output is already set, all we can
                // is to ignore the exception we've just caught;
                // throwing it further will probably make it just worse,
                // since the the caller have to take this scenario into acc.
            }
            return output;
        }
    }
}
