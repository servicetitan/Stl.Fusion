using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    public static class SymbolEx
    {
        public static bool IsValidOptionsKey(this Symbol key)
            => key.Value[0] == '@';
        
        public static void ThrowIfInvalidOptionsKey(this Symbol key)
        {
            if (!key.IsValidOptionsKey())
                throw Errors.InvalidOptionsKey();
        }
    }
}
