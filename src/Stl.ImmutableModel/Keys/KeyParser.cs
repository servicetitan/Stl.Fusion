using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stl.Collections;
using Stl.Internal;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public interface IKeyParser
    {
        string Tag { get; }
        Key Parse(ref ListParser parser);
    }

    public abstract class KeyParserBase : IKeyParser
    {
        public string Tag { get; }

        protected KeyParserBase(string tag) => Tag = tag; 

        public abstract Key Parse(ref ListParser parser);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Key? ParseContinuation(ref ListParser parser) 
            => KeyParser.Parse(ref parser);
    }

    public sealed class KeyParser
    {
        private static volatile KeyParser _instance;

        static KeyParser()
        {
            _instance = new KeyParser(ImmutableDictionary<string, IKeyParser>.Empty);
            RegisterKeyType<StringKey>();
            RegisterKeyType<TypeKey>();
            RegisterKeyType<PropertyKey>();
            RegisterKeyType<OptionKey>();
        }

        public static void RegisterKeyType<TKey>()
            where TKey : Key 
            => RegisterKeyType(typeof(TKey));

        public static void RegisterKeyType(Type keyType)
        {
            if (!typeof(Key).IsAssignableFrom(keyType))
                throw new ArgumentOutOfRangeException(nameof(keyType));

            var createParserMethodName = nameof(StringKey.CreateParser);
            var bindingFlags = BindingFlags.Public | BindingFlags.Static;
            var createParser = keyType.GetMethod(createParserMethodName, bindingFlags)
                ?? throw new MissingMethodException(keyType.FullName, createParserMethodName);
            var parser = (IKeyParser) createParser.Invoke(null, new object[0])!;

            var parsers = _instance._parsers.ToDictionary();
            if (parsers.ContainsKey(parser.Tag))
                throw Errors.KeyAlreadyExists();
            parsers.Add(parser.Tag, parser);
            _instance = new KeyParser(new ReadOnlyDictionary<string, IKeyParser>(parsers));
        }

        public static Key? Parse(in ReadOnlySpan<char> source)
        {
            var parser = Key.ListFormat.CreateParser(source);
            return _instance.ParseImpl(ref parser);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Key? Parse(ref ListParser parser)
            => _instance.ParseImpl(ref parser);

        private readonly IReadOnlyDictionary<string, IKeyParser> _parsers;
        private readonly IKeyParser _stringKeyParser;

        // Private

        private KeyParser(IReadOnlyDictionary<string, IKeyParser> parsers)
        {
            _parsers = parsers;
            parsers.TryGetValue(StringKey.Tag, out _stringKeyParser!);
        }

        private Key? ParseImpl(ref ListParser parser)
        {
            var prevSource = parser.Source;
            if (parser.Source.IsEmpty || !parser.TryParseNext())
                return null;
            var item = parser.Item;
            
            if (item.Length <= 1)
                return new StringKey(item, Parse(ref parser));

            var isEscaped = prevSource[0] == parser.Escape;
            if (!isEscaped) {
                var c0 = item[0];
                if (c0 == Key.TagPrefix)
                    return _parsers[item].Parse(ref parser);
                if (c0 == LongKey.NumberPrefix) {
                    var value = long.Parse(item.Substring(1), CultureInfo.InvariantCulture);
                    return new LongKey(value, Parse(ref parser));
                }
            }

            return new StringKey(item, Parse(ref parser));
        }
    }
}
