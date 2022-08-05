using Stl.Serialization.Internal;

namespace Stl.Serialization;

public static class TextSerializer
{
    public static ITextSerializer Default { get; set; } = SystemJsonSerializer.Default;
    public static ITextSerializer None { get; } = NoneTextSerializer.Instance;
    public static ITextSerializer Null { get; } = NullTextSerializer.Instance;

    public static ITextSerializer NewAsymmetric(ITextSerializer reader, ITextSerializer writer, bool? preferStringApi = null)
        => new AsymmetricTextSerializer(reader, writer, preferStringApi);
}

public static class TextSerializer<T>
{
    public static ITextSerializer<T> Default { get; } = TextSerializer.Default.ToTyped<T>();
    public static ITextSerializer<T> None { get; } = NoneTextSerializer<T>.Instance;
    public static ITextSerializer<T> Null { get; } = NullTextSerializer<T>.Instance;

    public static ITextSerializer<T> New(Func<string, T> reader, Func<T, string> writer)
        => new FuncTextSerializer<T>(reader, writer);
    public static ITextSerializer<T> NewAsymmetric(ITextSerializer<T> reader, ITextSerializer<T> writer, bool? preferStringApi = null)
        => new AsymmetricTextSerializer<T>(reader, writer, preferStringApi);
}
