namespace Stl.Rpc.Infrastructure;

public class RpcInboundCallActivityMiddleware(IServiceProvider services)
    : RpcInboundMiddleware(services)
{
    public Sampler Sampler { get; init; } = Sampler.RandomShared(0.1);

    public override void BeforeCall(RpcInboundCall call)
    {
        if (!Sampler.Next.Invoke())
            return;

        var activity = call.MethodDef.Service.ActivitySource.StartActivity(call.MethodDef.FullName);
        if (activity == null)
            return;

        _ = call.UntypedResultTask.ContinueWith(
            _ => activity.Dispose(),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
