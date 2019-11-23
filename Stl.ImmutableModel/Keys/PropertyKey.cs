using System;
using Stl.Text;
using Symbol = Stl.Text.Symbol;

namespace Stl.ImmutableModel 
{
    public sealed class PropertyKey : KeyBase
    {
        public static readonly string Tag = GetTypeTag(typeof(PropertyKey));  
        public Symbol Symbol { get; }
        public string Value => Symbol.Value;

        public PropertyKey(Symbol value, KeyBase? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Symbol = value;

        public override void FormatTo(ref ListFormatter formatter)
        {
            formatter.Append(Tag);
            formatter.Append(Symbol.Value);
            base.FormatTo(ref formatter);
        }

        public override bool Equals(KeyBase other) 
            => other is StringKey k2 && Symbol.Equals(k2.Symbol);

        // Parser

        public static IKeyParser CreateParser() => new Parser(Tag);

        private class Parser : KeyParserBase
        {
            public Parser(string tag) : base(tag) { }

            public override KeyBase Parse(ref ListParser parser)
            {
                parser.ParseNext();
                var value = parser.Item;
                var continuation = ParseContinuation(ref parser);
                return new StringKey(value, continuation);
            }
        }
    }
}
