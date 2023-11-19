using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Interception;

public abstract class ArgumentListReader
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract void OnStruct<T>(T item, int index);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    public abstract void OnObject(Type type, object? item, int index);
}
