using System.Collections.Concurrent;
using System.Text.Json;
using Cysharp.Text;
using Newtonsoft.Json;
using Stl.Fusion.Bridge;
using Stl.Fusion.Interception;
using Stl.Interception;
using Stl.Reflection;

namespace Stl.Fusion.Tests.Services;

public class InMemoryReplicaCache : ReplicaCache
{
    public record Options
    {
        public bool IsEnabled { get; init; }
        public ConcurrentDictionary<Symbol, string> Cache { get; init; } = new();
        public ITextSerializer KeySerializer { get; } =
            new SystemJsonSerializer(new JsonSerializerOptions() { WriteIndented = false });
            // new NewtonsoftJsonSerializer(new JsonSerializerSettings() { Formatting = Formatting.None }).ToTyped<Key>();
        public ITextSerializer ValueSerializer { get; } = SystemJsonSerializer.Default;
    }

    private Options Settings { get; }
    private bool IsEnabled => Settings.IsEnabled;
    private ConcurrentDictionary<Symbol, string> Cache => Settings.Cache;
    private ITextSerializer KeySerializer => Settings.KeySerializer;
    private ITextSerializer ValueSerializer => Settings.ValueSerializer;

    public InMemoryReplicaCache(Options settings, IServiceProvider services)
        : base(services)
        => Settings = settings;

    protected override ValueTask<Result<T>?> GetInternal<T>(ComputeMethodInput input, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
            return ValueTaskExt.FromResult((Result<T>?)null);

        var key = GetKey(input);
        if (!Cache.TryGetValue(key, out var value)) {
            Log.LogInformation("Get({Key}) -> miss", key);
            return ValueTaskExt.FromResult((Result<T>?)null);
        }

        var output = ValueSerializer.Read<Result<T>>(value);
        Log.LogInformation("Get({Key}) -> {Result}", key, output);
        return ValueTaskExt.FromResult((Result<T>?)output);
    }

    protected override ValueTask SetInternal<T>(ComputeMethodInput input, Result<T> output, CancellationToken cancellationToken)
    {
        if (!IsEnabled )
            return ValueTaskExt.CompletedTask;

        var key = GetKey(input);
        var value = ValueSerializer.Write(output);
        Cache[key] = value;
        return ValueTaskExt.CompletedTask;
    }

    // Private methods

    private Symbol GetKey(ComputeMethodInput input)
    {
        var arguments = input.Arguments;
        var ctIndex = input.MethodDef.CancellationTokenArgumentIndex;
        if (ctIndex >= 0)
            arguments = arguments.Remove(ctIndex);

        var service = input.Service.GetType().NonProxyType().GetName(true, true);
        var method = input.MethodDef.Method.Name;
        var argumentsJson = KeySerializer.Write(arguments, arguments.GetType());
        return $"{method} @ {service} <- {argumentsJson}";
    }
}
