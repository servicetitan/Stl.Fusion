using System;
using Stl.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public abstract class KeyBase : IEquatable<KeyBase>
    {
        protected internal static readonly ListFormat ListFormat = ListFormat.Default;
        protected internal static readonly char TagPrefix = '@';

        protected int HashCode { get; }
        public KeyBase? Continuation { get; }
        public bool IsComposite => !ReferenceEquals(Continuation, null);
        public int Size => (Continuation?.Size ?? 0) + 1;

        protected KeyBase(int ownHashCode, KeyBase? continuation = null)
        {
            HashCode = unchecked(ownHashCode + 347 * continuation?.HashCode ?? 0);
            Continuation = continuation;
        }

        public override string ToString() => $"{GetType().Name}({Format()})";

        public string Format()
        {
            var listFormatter = ListFormat.CreateFormatter();
            FormatTo(ref listFormatter);
            listFormatter.AppendEnd();
            return listFormatter.Output;
        }

        public virtual void FormatTo(ref ListFormatter formatter) 
            => Continuation?.FormatTo(ref formatter);

        public static KeyBase Parse(string source) => KeyParser.Parse(source);
        public static KeyBase Parse(in ReadOnlySpan<char> source) => KeyParser.Parse(source);

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

        public abstract bool Equals(KeyBase other);
        public override int GetHashCode() => HashCode;
        public static bool operator ==(KeyBase left, KeyBase right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(KeyBase left, KeyBase right) => !(left?.Equals(right) ?? ReferenceEquals(right, null));
    }
}
