using System;
using Stl.ImmutableModel.Internal;
using Stl.Text;
using Symbol = Stl.Text.Symbol;

namespace Stl.ImmutableModel 
{
    public sealed class StringKey : Key, IEquatable<StringKey>
    {
        public static readonly string Tag = GetTypeTag(typeof(StringKey));
        public Symbol Symbol { get; }
        public string Value => Symbol.Value;

        public StringKey(Symbol value, Key? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Symbol = value;

        public override void FormatTo(ref ListFormatter formatter)
        {
            var value = Symbol.Value;
            if (value.Length > 1 && value[0] == TagPrefix || value[0] == LongKey.NumberPrefix)
                formatter.AppendWithEscape(value);
            else
                formatter.Append(value);
            Continuation?.FormatTo(ref formatter);
        }

        public bool Equals(StringKey? other) => !ReferenceEquals(other, null) 
            && Symbol.Equals(other.Symbol) 
            && Equals(Continuation, other.Continuation);
        public override bool Equals(Key? other) => Equals(other as StringKey);
        public override bool Equals(object? other) => Equals(other as StringKey);
        public override int GetHashCode() => HashCode;

        public static implicit operator StringKey(string value) => new StringKey(value);
        public static implicit operator StringKey(Symbol symbol) => new StringKey(symbol);

        // Parser

        public static IKeyParser CreateParser() => new Parser(Tag);

        private class Parser : KeyParserBase
        {
            public Parser(string tag) : base(tag) { }

            public override Key Parse(ref ListParser parser)
            {
                if (!parser.TryParseNext())
                    throw Errors.InvalidKeyFormat();
                var value = parser.Item;
                var continuation = ParseContinuation(ref parser);
                return new StringKey(value, continuation);
            }
        }
    }
}
