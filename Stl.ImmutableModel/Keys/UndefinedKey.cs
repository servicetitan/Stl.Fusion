using System;
using Stl.ImmutableModel.Internal;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public sealed class UndefinedKey : Key, IEquatable<UndefinedKey>
    {
        public static readonly string Tag = GetTypeTag(typeof(UndefinedKey));

        internal UndefinedKey() : base(0, null) { } 

        public override void FormatTo(ref ListFormatter formatter) 
            => formatter.Append(Tag);

        public bool Equals(UndefinedKey? other) => !ReferenceEquals(other, null);
        public override bool Equals(Key? other) => Equals(other as UndefinedKey);
        public override bool Equals(object? other) => Equals(other as UndefinedKey);
        public override int GetHashCode() => HashCode;

        // Parser

        public static IKeyParser CreateParser() => new Parser(Tag);

        private class Parser : KeyParserBase
        {
            public Parser(string tag) : base(tag) { }

            public override Key Parse(ref ListParser parser)
            {
                if (parser.TryParseNext())
                    throw Errors.InvalidKeyFormat();
                return Key.Undefined;
            }
        }
    }
}
