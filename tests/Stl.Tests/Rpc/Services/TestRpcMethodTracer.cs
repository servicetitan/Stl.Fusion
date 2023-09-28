using Stl.Rpc;
using Stl.Rpc.Diagnostics;
using Stl.Rpc.Infrastructure;

namespace Stl.Tests.Rpc;

public class TestRpcMethodTracer : RpcMethodTracer
{
    private long _enterCount;
    private long _exitCount;
    private long _errorCount;

    public long EnterCount => Interlocked.Read(ref _enterCount);
    public long ExitCount => Interlocked.Read(ref _exitCount);
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    public TestRpcMethodTracer(RpcMethodDef method) : base(method) { }

    public override RpcMethodTrace? TryStartTrace(RpcInboundCall call)
        => new Trace(this);

    // Nested types

    private sealed class Trace : RpcMethodTrace
    {
        private readonly TestRpcMethodTracer _tracer;

        public Trace(TestRpcMethodTracer tracer)
        {
            _tracer = tracer;
            Interlocked.Increment(ref _tracer._enterCount);
        }

        public override void OnResultTaskReady(RpcInboundCall call)
            => _ = call.UntypedResultTask.ContinueWith(Complete,
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        private void Complete(Task resultTask)
        {
            Interlocked.Increment(ref _tracer._exitCount);
            if (!resultTask.IsCompletedSuccessfully())
                Interlocked.Increment(ref _tracer._errorCount);
        }
    }
}
