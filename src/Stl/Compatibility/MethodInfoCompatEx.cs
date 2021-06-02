// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    public static class MethodInfoCompatEx
    {
        public static bool IsConstructedGenericMethod(this MethodInfo methodInfo)
#if !NETSTANDARD2_0
            => methodInfo.IsConstructedGenericMethod;
#else
            => methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition;
#endif
    }
}
