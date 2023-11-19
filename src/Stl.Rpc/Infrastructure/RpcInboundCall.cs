using System.Diagnostics.CodeAnalysis;
using Stl.Interception;
using Stl.Rpc.Diagnostics;
using Stl.Rpc.Internal;
using Errors = Stl.Rpc.Internal.Errors;

namespace Stl.Rpc.Infrastructure;

#pragma warning disable MA0022
#pragma warning disable RCS1210
#pragma warning disable VSTHRD103

public abstract class RpcInboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>> FactoryCache = new();

    protected readonly CancellationTokenSource? CancellationTokenSource;
    protected ILogger Log => Context.Peer.Log;

    public readonly RpcInboundContext Context;
    public readonly CancellationToken CancellationToken;
    public ArgumentList? Arguments;
    public abstract Task UntypedResultTask { get; }
    public List<RpcHeader>? ResultHeaders;

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
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
        Id = NoWait ? 0 : context.Message.RelatedId;
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
        return $"{GetType().GetName()} #{message.RelatedId}: {MethodDef.Name}{arguments}"
            + (headers.Count > 0 ? $", Headers: {headers.ToDelimitedString()}" : "");
    }

    public abstract Task Run();

    public void Cancel()
        => CancellationTokenSource.CancelAndDisposeSilently();

    // Protected methods

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected abstract Task StartCompletion();

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected bool PrepareToStart()
    {
        var inboundCalls = Context.Peer.InboundCalls;
        var existingCall = inboundCalls.GetOrRegister(this);
        if (existingCall == this)
            return true;

        _ = existingCall.StartCompletion(); // Starts or restarts the completion
        return false;
    }

    protected virtual bool Unregister()
    {
        if (!Context.Peer.InboundCalls.Unregister(this))
            return false; // Already completed or NoWait

        CancellationTokenSource.DisposeSilently();
        return true;
    }
}

public class RpcInboundCall<TResult>(RpcInboundContext context, RpcMethodDef methodDef)
    : RpcInboundCall(context, methodDef)
{
    public Task<TResult> ResultTask { get; private set; } = null!;
    public override Task UntypedResultTask => ResultTask;

    [RequiresUnreferencedCode(UnreferencedCode.Rpc)]
#pragma warning disable IL2046
    public override Task Run()
#pragma warning restore IL2046
    {
        if (NoWait) {
            try {
                Arguments ??= DeserializeArguments();
                if (Arguments == null)
                    return Task.CompletedTask; // No way to resolve argument list type -> the related call is already gone

                var peer = Context.Peer;
                peer.CallLog?.Log(peer.CallLogLevel, "'{PeerRef}': <- {Call}", peer.Ref, this);
                return InvokeTarget();
            }
            catch (Exception error) {
                return Task.FromException<TResult>(error);
            }
        }

        lock (Lock) {
            if (!PrepareToStart())
                return Task.CompletedTask;

            RpcMethodTrace? trace = null;
            var inboundMiddlewares = Hub.InboundMiddlewares.NullIfEmpty();
            try {
                Arguments ??= DeserializeArguments();
                if (Arguments == null)
                    return Task.CompletedTask; // No way to resolve argument list type -> the related call is already gone

                // Before call
                var peer = Context.Peer;
                peer.CallLog?.Log(peer.CallLogLevel, "'{PeerRef}': <- {Call}", peer.Ref, this);
                if (MethodDef.Tracer is { } tracer && tracer.Sampler.Next.Invoke())
                    trace = tracer.TryStartTrace(this);

                // Call
                ResultTask = inboundMiddlewares != null
                    ? InvokeTarget(inboundMiddlewares)
                    : InvokeTarget();
            }
            catch (Exception error) {
                ResultTask = Task.FromException<TResult>(error);
            }
            finally {
                trace?.OnResultTaskReady(this);
            }
        }
        return StartCompletion();
    }

    // Protected methods

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
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

    protected async Task<TResult> InvokeTarget(RpcInboundMiddlewares middlewares)
    {
        await middlewares.BeforeCall(this).ConfigureAwait(false);
        Task<TResult> resultTask = null!;
        try {
            resultTask = InvokeTarget();
            return await resultTask.ConfigureAwait(false);
        }
        catch (Exception e) {
            resultTask ??= Task.FromException<TResult>(e);
            throw;
        }
        finally {
            await middlewares.AfterCall(this, resultTask).ConfigureAwait(false);
        }
    }

    protected virtual Task<TResult> InvokeTarget()
    {
        var methodDef = MethodDef;
        var server = methodDef.Service.Server;
        return (Task<TResult>)methodDef.Invoker.Invoke(server, Arguments!);
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected override Task StartCompletion()
    {
        if (!ResultTask.IsCompleted)
            return Complete();

        _ = CompleteSendResult();
        return Task.CompletedTask;
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected async Task Complete()
    {
        await ResultTask.SilentAwait(false);
        _ = CompleteSendResult();
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected virtual Task CompleteSendResult()
    {
        var mustSendResult = !CancellationToken.IsCancellationRequested;
        Unregister();
        return mustSendResult
            ? SendResult()
            : Task.CompletedTask;
    }

    [RequiresUnreferencedCode(Stl.Internal.UnreferencedCode.Serialization)]
    protected Task SendResult()
    {
        var resultTask = ResultTask;
        Result<TResult> result;
        if (!resultTask.IsCompleted)
            result = InvocationIsStillInProgressErrorResult();
        else if (resultTask.Exception is { } error) {
            Log.IfEnabled(LogLevel.Error)
                ?.LogError(error, "Remote call completed with an error: {Call}", this);
            result = new Result<TResult>(default!, error.GetBaseException());
        }
        else if (resultTask.IsCanceled) {
            Log.IfEnabled(LogLevel.Debug)
                ?.LogDebug("Remote call cancelled on the server side: {Call}", this);
            result = new Result<TResult>(default!, new TaskCanceledException());
        }
        else
            result = resultTask.Result;

        var systemCallSender = Hub.SystemCallSender;
        return systemCallSender.Complete(Context.Peer, Id, result, MethodDef.AllowResultPolymorphism, ResultHeaders);
    }

    // Private methods

    private static Result<TResult> InvocationIsStillInProgressErrorResult() =>
        new(default!, Stl.Internal.Errors.InternalError(
            "Something is off: remote method isn't completed yet, but the result is requested to be sent."));
}
