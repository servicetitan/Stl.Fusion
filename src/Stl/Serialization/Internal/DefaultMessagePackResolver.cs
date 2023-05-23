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
        => FormatterCache<T>.Formatter;

    private static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static FormatterCache()
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
