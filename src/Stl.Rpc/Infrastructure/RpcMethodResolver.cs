#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public class RpcMethodResolver
{
    private static readonly ConcurrentDictionary<(Type Service, string Method), RpcMethodDef?> Cache = new();

    public virtual RpcMethodDef? Resolve(Type serviceType, string methodName)
    {
        var result = Cache.GetOrAdd((serviceType, methodName), key => {
            var (tService, methodName1) = key;
            switch (methodName1.Split(':')) {
                case [var mName, var sArgumentCount]:
#if NET7_0_OR_GREATER
                if (!int.TryParse(sArgumentCount, CultureInfo.InvariantCulture, out var argumentCount))
#else
#pragma warning disable MA0011
                if (!int.TryParse(sArgumentCount, out var argumentCount))
#pragma warning restore MA0011
#endif
                        return null;

                    var candidateMethods = tService.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                    var method = candidateMethods
                        .SingleOrDefault(m =>
                            Equals(m.Name, mName)
                            && m.GetParameters().Length == argumentCount
                            && !m.IsGenericMethodDefinition);
                    return method == null ? null : new RpcMethodDef(tService, method);
                default:
                    return null;
            }
        });
        return result?.IsValid == true ? result : null;
    }
}
