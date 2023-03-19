namespace Stl.Interception;

public interface IProxy
{
    Interceptor Interceptor { get; }

    void Bind(Interceptor interceptor);
}
