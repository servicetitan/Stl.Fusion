namespace Stl.Fusion.Tests.Services;

[DataContract]
public record SetValueCommand : ICommand<Unit>
{
    [DataMember]
    public string Value { get; init; } = "";
}

public interface ISimplestProvider
{
    // These two properties are here solely for testing purposes
    int GetValueCallCount { get; }
    int GetCharCountCallCount { get; }

    void SetValue(string value);
    [ComputeMethod(KeepAliveTime = 10)]
    Task<string> GetValue();
    [ComputeMethod(KeepAliveTime = 0.5, ErrorAutoInvalidateTime = 0.5)]
    Task<int> GetCharCount();
    [ComputeMethod(ErrorAutoInvalidateTime = 0.5)]
    Task<int> Fail(Type exceptionType, bool wrapToResultException);

    [CommandHandler]
    Task SetValue(SetValueCommand command, CancellationToken cancellationToken = default);
}

[RegisterComputeService(typeof(ISimplestProvider), Lifetime = ServiceLifetime.Scoped, Scope = ServiceScope.Services)]
public class SimplestProvider : ISimplestProvider, IHasId<Type>
{
    private static volatile string _value = "";
    private readonly bool _isCaching;

    public Type Id => GetType();
    public int GetValueCallCount { get; private set; }
    public int GetCharCountCallCount { get; private set; }

    public SimplestProvider()
        => _isCaching = GetType().Name.EndsWith("Proxy");

    public void SetValue(string value)
    {
        Interlocked.Exchange(ref _value, value);
        Invalidate();
    }

    public virtual Task<string> GetValue()
    {
        GetValueCallCount++;
        return Task.FromResult(_value);
    }

    public virtual async Task<int> GetCharCount()
    {
        GetCharCountCallCount++;
        var value = await GetValue().ConfigureAwait(false);
        return value.Length;
    }

    public virtual Task<int> Fail(Type exceptionType, bool wrapToResultException)
    {
        var e = ExceptionInfo.ToExceptionConverter.Invoke(
            new ExceptionInfo(exceptionType, "Fail!"))!;
        throw e.MaybeToResultException(wrapToResultException);
    }

    public virtual Task SetValue(SetValueCommand command, CancellationToken cancellationToken = default)
    {
        SetValue(command.Value);
        return Task.CompletedTask;
    }

    protected virtual void Invalidate()
    {
        if (!_isCaching)
            return;

        using (Computed.Invalidate()) {
            GetValue().AssertCompleted();
        }
        // No need to invalidate GetCharCount,
        // since it will be invalidated automatically.
    }
}
