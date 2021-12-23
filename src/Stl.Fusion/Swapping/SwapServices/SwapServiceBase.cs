using Stl.Fusion.Interception;

namespace Stl.Fusion.Swapping;

public abstract class SwapServiceBase : ISwapService
{
    protected Func<ITextSerializer<object>> SerializerFactory { get; set; } = null!;

    protected SwapServiceBase() { }
    protected SwapServiceBase(Func<ITextSerializer<object>> serializerFactory)
        => SerializerFactory = serializerFactory;

    public async ValueTask<IResult?> Load(
        (ComputeMethodInput Input, LTag Version) key,
        CancellationToken cancellationToken = default)
    {
        var serializedKey = SerializeKey(key.Input, key.Version);
        var data = await Load(serializedKey, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return null;
        return SerializerFactory().Read(data) as IResult;
    }

    public async ValueTask Store(
        (ComputeMethodInput Input, LTag Version) key, IResult value,
        CancellationToken cancellationToken = default)
    {
        var serializedKey = SerializeKey(key.Input, key.Version);
        if (await Renew(serializedKey, cancellationToken).ConfigureAwait(false))
            return;
        var data = SerializerFactory().Write(value);
        await Store(serializedKey, data, cancellationToken).ConfigureAwait(false);
    }

    // Protected methods

    protected abstract ValueTask<string?> Load(string key, CancellationToken cancellationToken);
    protected abstract ValueTask<bool> Renew(string key, CancellationToken cancellationToken);
    protected abstract ValueTask Store(string key, string value, CancellationToken cancellationToken);

    protected virtual string SerializeKey(ComputeMethodInput input, LTag version)
    {
        using var f = ListFormat.Default.CreateFormatter();
        var method = input.Method;
        f.Append(method.InvocationTargetHandler.ToStringFunc(input.Target));
        f.Append(version.ToString());
        var arguments = input.Arguments;
        for (var i = 0; i < method.ArgumentHandlers.Length; i++) {
            var handler = method.ArgumentHandlers[i];
            f.Append(handler.ToStringFunc(arguments[i]));
        }
        f.AppendEnd();
        return f.Output;
    }
}
