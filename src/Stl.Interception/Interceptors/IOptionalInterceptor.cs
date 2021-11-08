using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public interface IOptionalInterceptor : IInterceptor
{
    Action<IInvocation>? GetHandler(IInvocation invocation);
    void ValidateType(Type type);
}
