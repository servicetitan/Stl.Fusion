using System;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public abstract class KeyBase : IEquatable<KeyBase>
    {
        public static readonly UndefinedKey Undefined = new UndefinedKey(); 
        public static readonly StringKey DefaultRootKey = new StringKey("@"); 

        protected internal static readonly ListFormat ListFormat = ListFormat.Default;
        protected internal static readonly char TagPrefix = '@';

        protected int HashCode { get; }
        public KeyBase? Continuation { get; }
        public bool IsComposite => !ReferenceEquals(Continuation, null);
        public int Size => (Continuation?.Size ?? 0) + 1;

        protected KeyBase(int ownHashCode, KeyBase? continuation = null)
        {
            if (continuation is UndefinedKey)
                throw Errors.ContinuationCannotBeUndefinedKey(nameof(continuation));
            HashCode = unchecked(ownHashCode + 347 * continuation?.HashCode ?? 0);
            Continuation = continuation;
        }

        // Operations

        public override string ToString() => Format();

        public string Format()
        {
            var formatter = ListFormat.CreateFormatter();
            FormatTo(ref formatter);
            formatter.AppendEnd();
            return formatter.Output;
        }

        public abstract void FormatTo(ref ListFormatter formatter); 

        public static KeyBase Parse(string source) 
            => KeyParser.Parse(source) ?? throw new NullReferenceException();
        public static KeyBase Parse(in ReadOnlySpan<char> source) 
            => KeyParser.Parse(source) ?? throw new NullReferenceException();

        public static StringKey operator &(Symbol prefix, KeyBase? suffix) => new StringKey(prefix, suffix);

        // Protected

        protected static string GetTypeTag(Type type)
        {
            var tagName = type.ToMethodName();
            if (tagName.Length > 0)
                tagName = tagName.Substring(0, 1).ToLowerInvariant() + tagName.Substring(1);
            if (tagName.EndsWith("Key"))
                tagName = tagName.Substring(0, tagName.Length - 3);

            return TagPrefix + tagName;
        }

        // Equality

        public override bool Equals(object? other) => throw new NotImplementedException();
        public abstract bool Equals(KeyBase? other);
        public override int GetHashCode() => HashCode;
        public static bool operator ==(KeyBase? left, KeyBase? right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(KeyBase? left, KeyBase? right) => !(left?.Equals(right) ?? ReferenceEquals(right, null));
    }
}
