using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Interception;

public abstract class ArgumentListWriter
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract T OnStruct<T>(int index);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract object? OnObject(Type type, int index);
}
