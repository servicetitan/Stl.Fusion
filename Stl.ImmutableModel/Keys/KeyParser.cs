using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using Stl.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public interface IKeyParser
    {
        string Tag { get; }
        KeyBase Parse(ref ListParser listParser);
    }

    public abstract class KeyParserBase : IKeyParser
    {
        public string Tag { get; }

        protected KeyParserBase(Type keyType) : this(keyType.ToMethodName()) { } 
        protected KeyParserBase(string tag) => Tag = tag; 

        public abstract KeyBase Parse(ref ListParser listParser);

        protected KeyBase? ParseContinuation(ref ListParser listParser) 
            => listParser.Source.IsEmpty ? null : KeyParser.Instance.Parse(ref listParser);
    }

    public class KeyParser
    {
        public static KeyParser Instance { get; private set; }

        static KeyParser()
        {
            Instance = new KeyParser(ImmutableDictionary<string, IKeyParser>.Empty);
            RegisterKeyType<SymbolKey>();
        }

        public static void RegisterKeyType<TKey>()
            where TKey : KeyBase 
            => RegisterKeyType(typeof(TKey));

        public static void RegisterKeyType(Type keyType)
        {
            if (!typeof(KeyBase).IsAssignableFrom(keyType))
                throw new ArgumentOutOfRangeException(nameof(keyType));

            var createParserMethodName = nameof(SymbolKey.CreateParser);
            var bindingFlags = BindingFlags.Public | BindingFlags.Static;
            var createParser = keyType.GetMethod(createParserMethodName, bindingFlags)
                ?? throw new MissingMethodException(keyType.FullName, createParserMethodName);
            var parser = (IKeyParser) createParser.Invoke(null, new object[0])!;

            var parsers = Instance.Parsers.ToDictionary();
            if (parsers.ContainsKey(parser.Tag))
                throw Errors.KeyAlreadyExists();
            parsers.Add(parser.Tag, parser);
            Instance = new KeyParser(new ReadOnlyDictionary<string, IKeyParser>(parsers));
        }

        public IReadOnlyDictionary<string, IKeyParser> Parsers { get; }

        private KeyParser(IReadOnlyDictionary<string, IKeyParser> parsers) 
            => Parsers = parsers;

        public KeyBase Parse(in ReadOnlySpan<char> source)
        {
            var listParser = KeyBase.ListFormat.CreateParser(source);
            return Parse(ref listParser);
        }

        public KeyBase Parse(ref ListParser parser)
        {
            throw new NotImplementedException();
        }
    }
}
