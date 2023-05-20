using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Stl.Serialization.Internal;

public class DefaultMessagePackResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new DefaultMessagePackResolver();

    public static IEnumerable<IFormatterResolver> Resolvers { get; set; } = new [] {
        StandardResolver.Instance,
        UnitMessagePackFormatter.Resolver,
    };

    private DefaultMessagePackResolver() { }

    public IMessagePackFormatter<T>? GetFormatter<T>()
        => Cache<T>.Formatter;

    private static class Cache<T>
    {
        public static IMessagePackFormatter<T>? Formatter;

        static Cache()
        {
            foreach (var resolver in Resolvers) {
                var formatter = resolver.GetFormatter<T>();
                if (formatter != null) {
                    Formatter = formatter;
                    return;
                }
            }
        }
    }
}
