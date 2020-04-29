using System;
using System.Collections.Concurrent;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public sealed class TypeKey : Key, IEquatable<TypeKey>
    {
        private static readonly ConcurrentDictionary<Type, TypeKey> Cache =
            new ConcurrentDictionary<Type, TypeKey>();

        public static readonly string Tag = GetTypeTag(typeof(TypeKey));  
        public Type Value { get; }

        public TypeKey(Type value, Key? continuation = null) 
            : base(value.GetHashCode(), continuation) 
            => Value = value;

        public static TypeKey New<T>() => New(typeof(T));
        public static TypeKey New(Type type) => Cache.GetOrAddChecked(type, t => new TypeKey(t));

        public override void FormatTo(ref ListFormatter formatter)
        {
            formatter.Append(Tag);
            formatter.Append(new TypeRef(Value).AssemblyQualifiedName.Value);
            Continuation?.FormatTo(ref formatter);
        }

        public bool Equals(TypeKey? other) => !ReferenceEquals(other, null) 
            && Value == other.Value 
            && Equals(Continuation, other.Continuation);
        public override bool Equals(Key? other) => Equals(other as TypeKey);
        public override bool Equals(object? other) => Equals(other as TypeKey);
        public override int GetHashCode() => HashCode;

        public static implicit operator TypeKey(Type value) => new TypeKey(value);

        // Parser

        public static IKeyParser CreateParser() => new Parser(Tag);

        private class Parser : KeyParserBase
        {
            public Parser(string tag) : base(tag) { }

            public override Key Parse(ref ListParser parser)
            {
                if (!parser.TryParseNext())
                    throw Errors.InvalidKeyFormat();
                var value = new TypeRef(parser.Item).Resolve();
                var continuation = ParseContinuation(ref parser);
                return new TypeKey(value, continuation);
            }
        }
    }
}
