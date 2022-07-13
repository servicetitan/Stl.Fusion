using Stl.Versioning;

namespace Stl.Fusion.Interception;

public abstract class ComputeMethodFunctionBase<T> : ComputeFunctionBase<T>
{
    protected readonly VersionGenerator<LTag> VersionGenerator;

    protected ComputeMethodFunctionBase(
        ComputeMethodDef methodDef,
        IServiceProvider services,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, services)
    {
        VersionGenerator = versionGenerator;
    }

    protected override async ValueTask<IComputed<T>> Compute(
        ComputeMethodInput input, IComputed<T>? existing,
        CancellationToken cancellationToken)
    {
        var version = VersionGenerator.NextVersion(existing?.Version ?? default);
        var computed = CreateComputed(input, version);
        try {
            using var _ = Computed.ChangeCurrent(computed);
            var result = input.InvokeOriginalFunction(cancellationToken);
            if (input.MethodDef.ReturnsValueTask) {
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
