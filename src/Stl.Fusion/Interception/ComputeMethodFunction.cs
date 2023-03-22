using Stl.Versioning;

namespace Stl.Fusion.Interception;

public class ComputeMethodFunction<T> : ComputeMethodFunctionBase<T>
{
    public ComputeMethodFunction(
        ComputeMethodDef methodDef,
        IServiceProvider services,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, services, versionGenerator)
    { }

    protected override Computed<T> CreateComputed(ComputeMethodInput input, LTag tag)
        => new ComputeMethodComputed<T>(ComputedOptions, input, tag);
}
