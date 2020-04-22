using Stl.Purifier.Internal;

namespace Stl.Purifier
{
    public static class ComputeContextEx
    {
        public static ComputeContextUseScope Use(this ComputeContext? context)
        {
            if (context != null)
                return new ComputeContextUseScope(context, false);
            context = ComputeContext.CurrentLocal.Value;
            if (context != null && context.TrySetIsUsed())
                return new ComputeContextUseScope(context, true);
            return new ComputeContextUseScope(ComputeContext.Default, false);
        }
    }
}
