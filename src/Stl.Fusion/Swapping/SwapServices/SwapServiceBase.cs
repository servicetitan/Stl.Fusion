using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Interception;
using Stl.Serialization;
using Stl.Text;

namespace Stl.Fusion.Swapping
{
    public abstract class SwapServiceBase<TSerialized> : ISwapService
    {
        protected Func<ISerializer<TSerialized>> SerializerFactory { get; set; } = null!;

        protected SwapServiceBase() { }
        protected SwapServiceBase(Func<ISerializer<TSerialized>> serializerFactory)
            => SerializerFactory = serializerFactory;

        public async ValueTask<IResult?> Load(
            (ComputeMethodInput Input, LTag Version) key,
            CancellationToken cancellationToken = default)
        {
            var serializedKey = SerializeKey(key.Input, key.Version);
            var serializedValueOpt = await Load(serializedKey, cancellationToken).ConfigureAwait(false);
            if (!serializedValueOpt.IsSome(out var serializedValue))
                return null!;
            return SerializerFactory.Invoke().Deserialize<object>(serializedValue) as IResult;
        }

        public async ValueTask Store(
            (ComputeMethodInput Input, LTag Version) key, IResult value,
            CancellationToken cancellationToken = default)
        {
            var serializedKey = SerializeKey(key.Input, key.Version);
            if (await Renew(serializedKey, cancellationToken).ConfigureAwait(false))
                return;
            var serializedValue = SerializerFactory.Invoke().Serialize<object>(value);
            await Store(serializedKey, serializedValue, cancellationToken).ConfigureAwait(false);
        }

        // Protected methods

        protected abstract ValueTask<Option<TSerialized>> Load(string key, CancellationToken cancellationToken);
        protected abstract ValueTask<bool> Renew(string key, CancellationToken cancellationToken);
        protected abstract ValueTask Store(string key, TSerialized value, CancellationToken cancellationToken);

        protected virtual string SerializeKey(ComputeMethodInput input, LTag version)
        {
            var sb = StringBuilderEx.Acquire(256);
            var f = ListFormat.Default.CreateFormatter(sb);
            var method = input.Method;
            f.Append(method.InvocationTargetHandler.ToStringFunc.Invoke(input.Target));
            f.Append(version.ToString());
            var arguments = input.Arguments;
            for (var i = 0; i < method.ArgumentHandlers.Length; i++) {
                var handler = method.ArgumentHandlers[i];
                f.Append(handler.ToStringFunc.Invoke(arguments[i]));
            }
            f.AppendEnd();
            return sb.ToStringAndRelease();
        }
    }
}
