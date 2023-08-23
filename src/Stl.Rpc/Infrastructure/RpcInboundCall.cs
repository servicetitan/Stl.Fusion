using Stl.Interception;
using Stl.Rpc.Internal;

namespace Stl.Rpc.Infrastructure;

#pragma warning disable VSTHRD103

public abstract class RpcInboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>> FactoryCache = new();

    protected readonly CancellationTokenSource? CancellationTokenSource;

    public readonly RpcInboundContext Context;
    public readonly CancellationToken CancellationToken;
    public ArgumentList? Arguments;
    public abstract Task UntypedResultTask { get; }
    public List<RpcHeader>? ResultHeaders;

    public static RpcInboundCall New(byte callTypeId, RpcInboundContext context, RpcMethodDef? methodDef)
    {
        if (methodDef == null) {
            var notFoundMethodDef = context.Peer.Hub.SystemCallSender.NotFoundMethodDef;
            var message = context.Message;
            return new RpcInbound404Call<Unit>(context, notFoundMethodDef) {
                // This prevents argument deserialization
                Arguments = ArgumentList.New(message.Service, message.Method)
            };
        }

        return FactoryCache.GetOrAdd((callTypeId, methodDef.UnwrappedReturnType), static key => {
            var (callTypeId, tResult) = key;
            var type = RpcCallTypeRegistry.Resolve(callTypeId)
                .InboundCallType
                .MakeGenericType(tResult);
            return (Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>)type
                .GetConstructorDelegate(typeof(RpcInboundContext), typeof(RpcMethodDef))!;
        }).Invoke(context, methodDef);
    }

    protected RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(methodDef)
    {
        Context = context;
        Id = NoWait ? 0 : context.Message.CallId;
        var cancellationToken = Context.CancellationToken;
        if (NoWait)
            CancellationToken = cancellationToken;
        else {
            CancellationTokenSource = cancellationToken.CreateLinkedTokenSource();
            CancellationToken = CancellationTokenSource.Token;
        }
    }

    public override string ToString()
    {
        var message = Context.Message;
        var headers = message.Headers.OrEmpty();
        var arguments = Arguments != null ? Arguments.ToString() : $"ArgumentData: {message.ArgumentData}";
        return $"{GetType().GetName()} #{message.CallId}: {MethodDef.Name}{arguments}"
            + (headers.Count > 0 ? $", Headers: {headers.ToDelimitedString()}" : "");
    }

    public abstract ValueTask Run();

    public abstract ValueTask Complete(bool silentCancel = false);

    public abstract bool Restart();

    // Protected methods

    protected bool PrepareToStart()
    {
        var inboundCalls = Context.Peer.InboundCalls;
        while (true) {
            var existingCall = inboundCalls.GetOrRegister(this);
            if (existingCall == this)
                break;

            if (existingCall.Restart())
                return false;
        }
        return true;
    }

    protected bool PrepareToComplete(bool cancel = false)
    {
        if (!Context.Peer.InboundCalls.Unregister(this))
            return false; // Already completed or NoWait

        if (cancel)
            CancellationTokenSource.CancelAndDisposeSilently();
        else
            CancellationTokenSource?.Dispose();
        return true;
    }
}

public class RpcInboundCall<TResult>(RpcInboundContext context, RpcMethodDef methodDef)
    : RpcInboundCall(context, methodDef)
{
    public Task<TResult> ResultTask { get; private set; } = null!;
    public override Task UntypedResultTask => ResultTask;

    public override ValueTask Run()
    {
        if (NoWait) {
            try {
                Arguments ??= DeserializeArguments();
                if (Arguments == null)
                    return default; // No way to resolve argument list type -> the related call is already gone

                var peer = Context.Peer;
                peer.CallLog?.Log(peer.CallLogLevel, "'{PeerRef}': <- {Call}", peer.Ref, this);

                _ = InvokeTarget();
            }
            catch {
                // Intended
            }
            return default;
        }

        lock (Lock) {
            if (!PrepareToStart())
                return default;

            try {
                Arguments ??= DeserializeArguments();
                if (Arguments == null)
                    return default; // No way to resolve argument list type -> the related call is already gone

                var peer = Context.Peer;
                peer.CallLog?.Log(peer.CallLogLevel, "'{PeerRef}': <- {Call}", peer.Ref, this);

                Hub.InboundMiddlewares.BeforeCall(this);
                ResultTask = InvokeTarget();
            }
            catch (Exception error) {
                ResultTask = Task.FromException<TResult>(error);
            }
        }

        return ResultTask.IsCompleted
            ? Complete()
            : new ValueTask(CompleteEventually());
    }

    public override ValueTask Complete(bool silentCancel = false)
    {
        silentCancel |= CancellationToken.IsCancellationRequested;
        return PrepareToComplete(silentCancel) && !silentCancel
            ? SendResult()
            : default;
    }

    public override bool Restart()
    {
        if (!ResultTask.IsCompleted)
            return true; // Result isn't produced yet

        // Result is produced
        var inboundCalls = Context.Peer.InboundCalls;
        if (inboundCalls.Get(Id) == this)
            return true; // CompleteEventually haven't started yet -> let it do the job

        // Result might be sent, but likely isn't delivered - let's re-send it
        _ = SendResult();
        return true;
    }

    // Protected methods

    protected ArgumentList? DeserializeArguments()
    {
        var peer = Context.Peer;
        var message = Context.Message;
        var isSystemServiceCall = ServiceDef.IsSystem;

        if (!isSystemServiceCall && !peer.LocalServiceFilter.Invoke(ServiceDef))
            throw Errors.NoService(ServiceDef.Type);

        var arguments = MethodDef.ArgumentListFactory.Invoke();
        var allowPolymorphism = MethodDef.AllowArgumentPolymorphism;
        if (!MethodDef.HasObjectTypedArguments)
            peer.ArgumentSerializer.Deserialize(ref arguments, allowPolymorphism, message.ArgumentData);
        else {
            var dynamicCallHandler = (IRpcDynamicCallHandler)ServiceDef.Server;
            var expectedArguments = arguments;
            if (!dynamicCallHandler.IsValidCall(Context, ref expectedArguments, ref allowPolymorphism))
                return null;

            peer.ArgumentSerializer.Deserialize(ref expectedArguments, allowPolymorphism, message.ArgumentData);
            if (!ReferenceEquals(arguments, expectedArguments))
                arguments.SetFrom(expectedArguments);
        }

        // Set CancellationToken
        var ctIndex = MethodDef.CancellationTokenIndex;
        if (ctIndex >= 0)
            arguments.SetCancellationToken(ctIndex, CancellationToken);

        return arguments;
    }

    protected virtual Task<TResult> InvokeTarget()
    {
        var methodDef = MethodDef;
        var server = methodDef.Service.Server;
        return (Task<TResult>)methodDef.Invoker.Invoke(server, Arguments!);
    }

    protected ValueTask SendResult()
    {
        var resultTask = ResultTask;
        Result<TResult> result;
        if (!resultTask.IsCompleted)
            result = InvocationIsStillInProgressErrorResult();
        else if (resultTask.Exception is { } error)
            result = new Result<TResult>(default!, error.GetBaseException());
        else if (resultTask.IsCanceled)
            result = new Result<TResult>(default!, new TaskCanceledException());
        else
            result = resultTask.Result;

        var systemCallSender = Hub.SystemCallSender;
        return systemCallSender.Complete(Context.Peer, Id, result, MethodDef.AllowResultPolymorphism, ResultHeaders);
    }

    protected Task CompleteEventually()
        => ResultTask.ContinueWith(
            _ => Complete(),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // Private methods

    private static Result<TResult> InvocationIsStillInProgressErrorResult() =>
        new(default!, Stl.Internal.Errors.InternalError(
            "Something is off: remote method isn't completed yet, but the result is requested to be sent."));
}
