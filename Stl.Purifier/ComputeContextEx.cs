using Stl.Purifier.Internal;

namespace Stl.Purifier
{
    public static class ComputeContextEx
    {
        public static ComputedContextUseScope Use(this ComputeContext? context)
        {
            if (context != null)
                return new ComputedContextUseScope(context, false);
            context = ComputeContext.CurrentLocal.Value;
            if (context != null && context.TrySetIsUsed())
                return new ComputedContextUseScope(context, true);
            return new ComputedContextUseScope(ComputeContext.Default, false);
        }
    }
}
