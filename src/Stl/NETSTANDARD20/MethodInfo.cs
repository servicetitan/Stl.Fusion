namespace System.Reflection
{
    public static class MethodInfoEx
    {
#if !NETSTANDARD2_0
        public static bool IsConstructedGenericMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsConstructedGenericMethod;
        }
#else
        public static bool IsConstructedGenericMethod(this MethodInfo methodInfo)
        {
            return methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition;
        }
#endif
    }
}