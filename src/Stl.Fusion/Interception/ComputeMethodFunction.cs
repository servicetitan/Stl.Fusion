using System;
using Microsoft.Extensions.Logging;
using Stl.Generators;
using Stl.Internal;
using Stl.Versioning;

namespace Stl.Fusion.Interception
{
    public class ComputeMethodFunction<T> : ComputeMethodFunctionBase<T>
    {
        public ComputeMethodFunction(
            ComputeMethodDef method,
            VersionGenerator<LTag> versionGenerator,
            IServiceProvider services,
            ILogger<ComputeMethodFunction<T>>? log = null)
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
