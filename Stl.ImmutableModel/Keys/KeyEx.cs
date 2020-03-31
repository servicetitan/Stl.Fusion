using System.Runtime.CompilerServices;
using Stl.ImmutableModel.Internal;
using Stl.Text;

namespace Stl.ImmutableModel
{
    public static class KeyEx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this Key? key) => ReferenceEquals(key, null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Key ThrowIfNull(this Key? key) 
            => key.IsNull() ? throw Errors.KeyIsNull(nameof(key)) : key!;

        public static string Format(this Key? key)
        {
            if (ReferenceEquals(key, null))
                return Key.NullKeyFormat;
            var formatter = Key.ListFormat.CreateFormatter();
            key.FormatTo(ref formatter);
            formatter.AppendEnd();
            return formatter.Output;
        }
    }
}
