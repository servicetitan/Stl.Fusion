using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Serialization;
using Stl.Internal;

namespace Stl.Serialization.Internal;

#if !NET5_0
[RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#endif
public class CrossPlatformSerializationBinder : SerializationBinder
{
    private static readonly string CoreLibName = "System.Private.CoreLib";
    private static readonly Regex CoreLibRe = new("^" + Regex.Escape(CoreLibName));
    private static readonly string MsCorLibName = "mscorlib";
    private static readonly Regex MsCorLibRe = new("^" + Regex.Escape(MsCorLibName));
    private static bool IsMonoPlatform { get; }

    public static new readonly ISerializationBinder Instance = new CrossPlatformSerializationBinder();

    static CrossPlatformSerializationBinder()
    {
        var coreLibName = typeof(string).Assembly.FullName;
        IsMonoPlatform = MsCorLibRe.IsMatch(coreLibName!);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    protected override Type? ResolveType((string? AssemblyName, string TypeName) key)
    {
        var (assemblyName, typeName) = key;
        if (assemblyName != null)
            assemblyName = IsMonoPlatform
                ? CoreLibRe.Replace(assemblyName, MsCorLibName)
                : MsCorLibRe.Replace(assemblyName, CoreLibName);
        return base.ResolveType((assemblyName, typeName));
    }
}
