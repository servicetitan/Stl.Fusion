using System;
using Microsoft.Extensions.Logging;
using Stl.Generators;
using Stl.Internal;

namespace Stl.Fusion.Interception
{
    public class ComputeServiceFunction<T> : ComputeServiceFunctionBase<T>
    {
        public ComputeServiceFunction(
            ComputeMethodDef method,
            Generator<LTag> versionGenerator,
            IServiceProvider services,
            ILogger<ComputeServiceFunction<T>>? log = null)
            : base(method, versionGenerator, services, log)
        {
            if (method.Options.IsAsyncComputed)
                throw Errors.InternalError(
                    $"This type can't be used with {nameof(ComputedOptions)}.{nameof(ComputedOptions.IsAsyncComputed)} == true option.");
        }

        protected override IComputed<T> CreateComputed(ComputeMethodInput input, LTag tag)
            => new Computed<T>(Options, input, tag);
    }
}
