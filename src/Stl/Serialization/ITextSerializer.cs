using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Serialization;

public interface ITextSerializer : IByteSerializer
{
    bool PreferStringApi { get; }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    object? Read(string data, Type type);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    object? Read(ReadOnlyMemory<char> data, Type type);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    string Write(object? value, Type type);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    void Write(TextWriter textWriter, object? value, Type type);

    new ITextSerializer<T> ToTyped<T>(Type? serializedType = null);
}

public interface ITextSerializer<T> : IByteSerializer<T>
{
    bool PreferStringApi { get; }

    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    T Read(string data);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    T Read(ReadOnlyMemory<char> data);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    string Write(T value);
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
    void Write(TextWriter textWriter, T value);
}
