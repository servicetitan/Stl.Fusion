using System;
using System.Globalization;
using Stl.ImmutableModel.Internal;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public sealed class LongKey : Key, IEquatable<LongKey>
    {
        internal static readonly char NumberPrefix ='#';
        internal static readonly string NumberPrefixString = NumberPrefix.ToString();

        public static readonly string Tag = TagPrefix + NumberPrefixString;
        public long Value { get; }

        public LongKey(long value, Key? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Value = value;

        public override void FormatTo(ref ListFormatter formatter)
        {
            formatter.Append(NumberPrefixString);
            formatter.OutputBuilder.Append(Value.ToString(CultureInfo.InvariantCulture));
            Continuation?.FormatTo(ref formatter);
        }

        public bool Equals(LongKey? other) => !ReferenceEquals(other, null) 
            && Value.Equals(other.Value) 
            && Equals(Continuation, other.Continuation);
        public override bool Equals(Key? other) => Equals(other as LongKey);
        public override bool Equals(object? other) => Equals(other as LongKey);
        public override int GetHashCode() => HashCode;

        public static implicit operator LongKey(long value) => new LongKey(value);

        // Parser

        public static IKeyParser CreateParser() => new Parser(Tag);

        private class Parser : KeyParserBase
        {
            public Parser(string tag) : base(tag) { }

            public override Key Parse(ref ListParser parser)
            {
                if (!parser.TryParseNext())
                    throw Errors.InvalidKeyFormat();
                var value = long.Parse(parser.Item, CultureInfo.InvariantCulture);
                var continuation = ParseContinuation(ref parser);
                return new LongKey(value, continuation);
            }
        }
    }
}
