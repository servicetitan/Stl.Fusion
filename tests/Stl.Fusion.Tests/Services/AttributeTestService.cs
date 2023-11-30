namespace Stl.Fusion.Tests.Services;

public abstract class AttributeTestService : IComputeService
{
    public Task<bool> PublicMethod()
        => ProtectedMethod();

    [ComputeMethod]
    protected abstract Task<bool> ProtectedMethod();
}

public class AttributeTestServiceImpl : AttributeTestService
{
    protected override Task<bool> ProtectedMethod()
        => TaskExt.TrueTask;
}
