namespace System.Reflection
{
    internal static class MethodInfoCompatEx
    {
        public static bool IsConstructedGenericMethod(this MethodInfo methodInfo)
        {
#if !NETSTANDARD2_0
            return methodInfo.IsConstructedGenericMethod;
#else
            return methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition;
#endif
        }
    }
}