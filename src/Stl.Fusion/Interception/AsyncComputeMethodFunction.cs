using Stl.Fusion.Internal;
using Stl.Fusion.Swapping;
using Stl.Versioning;

namespace Stl.Fusion.Interception;

public interface IAsyncComputeMethodFunction : IComputeMethodFunction
{ }

public class AsyncComputeMethodFunction<T> : ComputeMethodFunctionBase<T>, IAsyncComputeMethodFunction
{
    public AsyncComputeMethodFunction(
        ComputeMethodDef methodDef,
        IServiceProvider services,
        VersionGenerator<LTag> versionGenerator)
        : base(methodDef, services, versionGenerator)
    {
        if (!methodDef.ComputedOptions.IsAsyncComputed)
            throw Stl.Internal.Errors.InternalError(
                $"This type shouldn't be used with {nameof(Fusion.ComputedOptions)}.{nameof(Fusion.ComputedOptions.IsAsyncComputed)} == false option.");
    }

    public override async Task<T> InvokeAndStrip(
        ComputedInput input, IComputed? usedBy, ComputeContext? context,
        CancellationToken cancellationToken = default)
    {
        var typedInput = (ComputeMethodInput) input;
        context ??= ComputeContext.Current;
        ResultBox<T>? output;

        // Read-Lock-RetryRead-Compute-Store pattern

        var asyncComputed = GetExistingAsyncComputed(typedInput);
        if (asyncComputed != null) {
            output = await asyncComputed.TryUseExisting(context, usedBy, cancellationToken).ConfigureAwait(false);
            if (output != null)
                return output.Value;
        }

        using var @lock = await InputLocks.Lock(input, cancellationToken).ConfigureAwait(false);

        asyncComputed = GetExistingAsyncComputed(typedInput);
        if (asyncComputed != null) {
            output = await asyncComputed.TryUseExisting(context, usedBy, cancellationToken).ConfigureAwait(false);
            if (output != null)
                return output.Value;
        }

        var computed = await Compute(input, (Computed<T>?) asyncComputed, cancellationToken).ConfigureAwait(false);
        var rOutput = computed.Output; // RenewTimeouts isn't called yet, so it's ok
        computed.UseNew(context, usedBy);
        return rOutput!.Value;
    }

    protected override Computed<T> CreateComputed(ComputeMethodInput input, LTag tag)
        => new SwappingComputed<T>(ComputedOptions, input, tag);

    protected IAsyncComputed<T>? GetExistingAsyncComputed(ComputeMethodInput input)
    {
        var computed = ComputedRegistry.Instance.Get(input);
        return computed as IAsyncComputed<T>;
    }
}
