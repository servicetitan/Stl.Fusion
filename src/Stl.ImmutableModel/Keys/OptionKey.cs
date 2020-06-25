using System;
using Stl.ImmutableModel.Internal;
using Stl.Text;
using Symbol = Stl.Text.Symbol;

namespace Stl.ImmutableModel 
{
    public sealed class OptionKey : Key, IEquatable<OptionKey>
    {
        public static readonly string Tag = GetTypeTag(typeof(OptionKey));  
        public Symbol Symbol { get; }
        public string Value => Symbol.Value;

        public OptionKey(Symbol value, Key? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Symbol = value;

        public override void FormatTo(ref ListFormatter formatter)
        {
            formatter.Append(Tag);
            formatter.Append(Symbol.Value);
            Continuation?.FormatTo(ref formatter);
        }

        public bool Equals(OptionKey? other) => !ReferenceEquals(other, null) 
            && Symbol.Equals(other.Symbol)
            && Equals(Continuation, other.Continuation);
        public override bool Equals(Key? other) => Equals(other as OptionKey);
        public override bool Equals(object? other) => Equals(other as OptionKey);
        public override int GetHashCode() => HashCode;

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
                return new OptionKey(value, continuation);
            }
        }
    }
}
