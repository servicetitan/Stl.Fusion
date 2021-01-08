using System.Reflection;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public interface IComputedOptionsProvider
    {
        ComputedOptions? GetComputedOptions(MethodInfo methodInfo);
        ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo);
        SwapAttribute? GetSwapAttribute(MethodInfo methodInfo);
    }

    public class ComputedOptionsProvider : IComputedOptionsProvider
    {
        public virtual ComputedOptions? GetComputedOptions(MethodInfo methodInfo)
        {
            var attribute = GetComputeMethodAttribute(methodInfo);
            if (attribute == null)
                return null;
            var swapAttribute = GetSwapAttribute(methodInfo);
            return ComputedOptions.FromAttribute(attribute, swapAttribute);
        }

        public virtual ComputeMethodAttribute? GetComputeMethodAttribute(MethodInfo methodInfo)
            => methodInfo.GetAttribute<ComputeMethodAttribute>(true, true);

        public virtual SwapAttribute? GetSwapAttribute(MethodInfo methodInfo)
            => methodInfo.GetAttribute<SwapAttribute>(true, true);
    }
}
