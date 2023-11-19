using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Text;

public static class JsonFormatter
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public static string Format(object value)
        => SystemJsonSerializer.Pretty.Write(value);
}
