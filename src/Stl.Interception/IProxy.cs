namespace Stl.Interception;

public interface IProxy : IRequiresAsyncProxy
{
    Interceptor Interceptor { get; }

    void Bind(Interceptor interceptor);
}
