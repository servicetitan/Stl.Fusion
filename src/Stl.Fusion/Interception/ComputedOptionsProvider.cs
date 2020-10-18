using System.Reflection;
using Stl.Reflection;

namespace Stl.Fusion.Interception
{
    public interface IComputedOptionsProvider
    {
        ComputedOptions? GetComputedOptions(InterceptorBase interceptor, MethodInfo methodInfo);
    }

    public class ComputedOptionsProvider : IComputedOptionsProvider
    {
        public virtual ComputedOptions? GetComputedOptions(InterceptorBase interceptor, MethodInfo methodInfo)
        {
            var attribute = GetInterceptedMethodAttribute(methodInfo);
            if (attribute == null)
                return null;
            var swapAttribute = GetSwapAttribute(methodInfo);
            return ComputedOptions.FromAttribute(attribute, swapAttribute);
        }

        protected InterceptedMethodAttribute? GetInterceptedMethodAttribute(MethodInfo methodInfo)
            => methodInfo.GetAttribute<InterceptedMethodAttribute>(true, true);

        protected SwapAttribute? GetSwapAttribute(MethodInfo methodInfo)
            => methodInfo.GetAttribute<SwapAttribute>(true, true);
    }
}
