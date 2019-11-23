using System;
using System.Text;
using Stl.Text;

namespace Stl.ImmutableModel 
{
    public abstract class KeyBase : IEquatable<KeyBase>
    {
        protected internal static ListFormatHelper FormatHelper = ListFormatHelper.Default; 

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
            var listFormatter = FormatHelper.CreateFormatter();
            FormatTo(ref listFormatter);
            return listFormatter.Output;
        }

        public virtual void FormatTo(ref ListFormatter listFormatter) 
            => Continuation?.FormatTo(ref listFormatter);

        public static KeyBase Parse(string source)
            => KeyParser.Instance.Parse(source);

        // Equality

        public abstract bool Equals(KeyBase other);
        public override int GetHashCode() => HashCode;
        public static bool operator ==(KeyBase left, KeyBase right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(KeyBase left, KeyBase right) => !(left?.Equals(right) ?? ReferenceEquals(right, null));
    }
}
