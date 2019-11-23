using System;
using Stl.Text;
using Symbol = Stl.Text.Symbol;

namespace Stl.ImmutableModel 
{
    public sealed class SymbolKey : KeyBase
    {
        public Symbol Value { get; }

        public SymbolKey(Symbol value, KeyBase? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Value = value;

        public override void FormatTo(ref ListFormatter listFormatter)
        {
            listFormatter.AppendWithEscape(Value.Value);
            base.FormatTo(ref listFormatter);
        }

        public override bool Equals(KeyBase other) 
            => other is SymbolKey k2 && Value.Equals(k2.Value);

        // Parser

        public static IKeyParser CreateParser() => new KeyParser();

        private class KeyParser : KeyParserBase
        {
            public KeyParser() : base(typeof(SymbolKey)) { }

            public override KeyBase Parse(ref ListParser listParser)
            {
                throw new NotImplementedException();
            }
        }
    }
}
