namespace Stl.Fusion.Tests.Services;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record SetValueCommand : ICommand<Unit>
{
    [DataMember, MemoryPackOrder(0)]
    public string Value { get; init; } = "";
}

public interface ISimplestProvider
{
    // These two properties are here solely for testing purposes
    int GetValueCallCount { get; }
    int GetCharCountCallCount { get; }

    void SetValue(string value);
    [ComputeMethod(MinCacheDuration = 10)]
    Task<string> GetValue();
    [ComputeMethod(MinCacheDuration = 0.5, TransientErrorInvalidationDelay = 0.5)]
    Task<int> GetCharCount();
    [ComputeMethod(TransientErrorInvalidationDelay = 0.5)]
    Task<int> Fail(Type exceptionType);

    [CommandHandler]
    Task SetValue(SetValueCommand command, CancellationToken cancellationToken = default);
}

public class SimplestProvider : ISimplestProvider, IHasId<Type>, IComputeService
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
        try {
            var value = await GetValue().ConfigureAwait(false);
            return value.Length;
        }
        catch (NullReferenceException e) {
            throw new TransientException(null, e);
        }
    }

    public virtual Task<int> Fail(Type exceptionType)
    {
        var e = new ExceptionInfo(exceptionType, "Fail!");
        throw e.ToException()!;
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

        using (Computed.Invalidate())
            _ = GetValue().AssertCompleted();

        // No need to invalidate GetCharCount,
        // since it will be invalidated automatically.
    }
}
