using Stl.Internal;

namespace Stl 
{
    public static class FreezableEx
    {
        // This method isn't a part of the interface mainly because otherwise
        // it's going to be a virtual generic method (i.e. w/ super slow invocation).
        public static T Defrost<T>(this T freezable, bool deep = false) 
            where T : IFreezable 
            => (T) freezable.BaseDefrost(deep); 

        // ThrowIfXxx

        public static void ThrowIfUnfrozen(this IFreezable freezable)
        {
            if (!freezable.IsFrozen) throw Errors.MustBeFrozen();
        }

        public static void ThrowIfUnfrozen(this IFreezable freezable, string paramName)
        {
            if (!freezable.IsFrozen) throw Errors.MustBeFrozen(paramName);
        }

        public static void ThrowIfFrozen(this IFreezable freezable)
        {
            if (freezable.IsFrozen) throw Errors.MustBeUnfrozen();
        }

        public static void ThrowIfFrozen(this IFreezable freezable, string paramName)
        {
            if (freezable.IsFrozen) throw Errors.MustBeUnfrozen(paramName);
        }
    }
}
