using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel 
{
    public static class KeyEx
    {
        public static bool IsNull(this Key? key) => ReferenceEquals(key, null);
        public static bool IsUndefined(this Key key) => Key.Undefined.Equals(key);
        public static bool IsNullOrUndefined(this Key? key) => ReferenceEquals(key, null) || Key.Undefined.Equals(key);
        
        public static Key ThrowIfUndefined(this Key key) 
            => key.IsUndefined() ? throw Errors.KeyIsUndefined(nameof(key)) : key;    
        public static Key ThrowIfNullOrUndefined(this Key? key) 
            => key.IsNull() ? throw Errors.KeyIsNull(nameof(key)) : key.ThrowIfUndefined();
    }
}
