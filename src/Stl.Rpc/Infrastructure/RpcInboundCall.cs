using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

#pragma warning disable VSTHRD103

public abstract class RpcInboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>> FactoryCache = new();

    protected CancellationTokenSource? CancellationTokenSource { get; private set; }

    public RpcInboundContext Context { get; }
    public CancellationToken CancellationToken { get; private set; }
    public ArgumentList? Arguments { get; protected set; } = null;
    public List<RpcHeader>? ResultHeaders { get; set; }

    public static RpcInboundCall New(byte callTypeId, RpcInboundContext context, RpcMethodDef? methodDef)
    {
        if (methodDef == null) {
            var notFoundMethodDef = context.Peer.Hub.SystemCallSender.NotFoundMethodDef;
            return new RpcInbound404Call<Unit>(context, notFoundMethodDef);
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
        Id =  methodDef.NoWait ? 0 : context.Message.CallId;
        var cancellationToken = Context.CancellationToken;
        if (NoWait)
            CancellationToken = cancellationToken;
        else {
            CancellationTokenSource = cancellationToken.CreateLinkedTokenSource();
            CancellationToken = CancellationTokenSource.Token;
        }
    }

    public abstract ValueTask Run();

    public abstract ValueTask Complete(bool silentCancel = false);

    public abstract bool Restart();

    // Protected methods

    protected bool PrepareToStart()
    {
        var inboundCalls = Context.Peer.InboundCalls;
        while (true) {
            var existingCall = inboundCalls.Register(this);
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

public class RpcInboundCall<TResult> : RpcInboundCall
{
    private static readonly Result<TResult> CancelledResult = new(default!, new TaskCanceledException());

    public Task<TResult> ResultTask { get; private set; } = null!;

    public RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override ValueTask Run()
    {
        ArgumentList? arguments;
        if (NoWait) {
            try {
                arguments = DeserializeArguments();
                if (arguments == null)
                    return default; // No way to resolve argument list type -> the related call is already gone

                Arguments = arguments;
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
                arguments = DeserializeArguments();
                if (arguments == null)
                    return default; // No way to resolve argument list type -> the related call is already gone

                Arguments = arguments;
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

        var arguments = ArgumentList.Empty;
        var argumentListType = MethodDef.RemoteArgumentListType;
        if (MethodDef.HasObjectTypedArguments) {
            var argumentListTypeResolver = (IRpcArgumentListTypeResolver)ServiceDef.Server;
            argumentListType = argumentListTypeResolver.GetArgumentListType(Context);
            if (argumentListType == null)
                return null;
        }

        if (argumentListType.IsGenericType) { // == Has 1+ arguments
            var headers = Context.Message.Headers.OrEmpty();
            if (headers.Any(static h => h.Name.StartsWith(RpcSystemHeaders.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
                var argumentTypes = argumentListType.GetGenericArguments();
                foreach (var h in headers) {
                    if (!h.Name.StartsWith(RpcSystemHeaders.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))
                        continue;
#if NET7_0_OR_GREATER
                    if (!int.TryParse(
                        h.Name.AsSpan(RpcSystemHeaders.ArgumentTypeHeaderPrefix.Length),
                        CultureInfo.InvariantCulture,
                        out var argumentIndex))
#else
#pragma warning disable MA0011
                    if (!int.TryParse(
                        h.Name.Substring(RpcSystemHeaders.ArgumentTypeHeaderPrefix.Length),
                        out var argumentIndex))
#pragma warning restore MA0011
#endif
                        continue;

                    var argumentType = new TypeRef(h.Value).Resolve();
                    if (!argumentTypes[argumentIndex].IsAssignableFrom(argumentType))
                        throw Errors.IncompatibleArgumentType(MethodDef, argumentIndex, argumentType);

                    argumentTypes[argumentIndex] = argumentType;
                }
                argumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(argumentTypes);
            }

            var deserializedArguments = peer.ArgumentSerializer.Deserialize(message.ArgumentData, argumentListType);
            if (argumentListType == MethodDef.ArgumentListType)
                arguments = deserializedArguments;
            else {
                arguments = (ArgumentList)MethodDef.ArgumentListType.CreateInstance();
                var ctIndex = MethodDef.CancellationTokenIndex;
                if (ctIndex >= 0)
                    deserializedArguments = deserializedArguments.InsertCancellationToken(ctIndex, CancellationToken);
                arguments.SetFrom(deserializedArguments);
            }
        }
        else if (argumentListType != MethodDef.ArgumentListType) {
            var ctIndex = MethodDef.CancellationTokenIndex;
            if (ctIndex >= 0)
                arguments = arguments.InsertCancellationToken(ctIndex, CancellationToken);
        }

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
        return systemCallSender.Complete(Context.Peer, Id, result, ResultHeaders);
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
