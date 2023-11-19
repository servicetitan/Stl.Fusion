using System.Diagnostics.CodeAnalysis;
using System.Web.Http.Controllers;
using Stl.Internal;

namespace Stl.Rpc.Server;

public static class AssemblyExt
{
    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static IEnumerable<Type> GetControllerTypes(this Assembly assembly, string? fullNamePrefixFilter = null)
    {
        var q = assembly.GetExportedTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Where(t => typeof(IHttpController).IsAssignableFrom(t)
                || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase));
        if (fullNamePrefixFilter != null)
            q = q.Where(c => (c.FullName ?? string.Empty).StartsWith(fullNamePrefixFilter, StringComparison.Ordinal));
        return q;
    }
}
