using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel 
{
    public static class KeyEx
    {
        public static bool IsUndefined(this Key key) => Key.Undefined.Equals(key);
        
        public static Key ThrowIfUndefined(this Key key) 
            => key.IsUndefined()
                ? throw Errors.KeyIsUndefined()
                : key;    
    }
}
