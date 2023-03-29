using System.Globalization;
using System.Text;
using Cysharp.Text;

namespace Stl.Fusion.Diagnostics;

public sealed class FusionMonitor : WorkerBase
{
    // Cached delegates
    private readonly Action<IComputed, bool> _onAccess;
    private readonly Action<IComputed> _onRegister;
    private readonly Action<IComputed> _onUnregister;
    private readonly object _lock = new();

    // Stats
    private Dictionary<string, (int, int)> _accesses = null!;
    private Dictionary<string, (int, int)> _registrations = null!;

    // Services
    private IServiceProvider Services { get; }
    private ILogger Log { get; }

    // Settings
    public RandomTimeSpan SleepPeriod { get; init; } = TimeSpan.Zero;
    public TimeSpan CollectPeriod { get; init; } = TimeSpan.FromMinutes(1);
    public Sampler AccessSampler { get; init; } = Sampler.EveryNth(8);
    public Func<IComputed, bool> AccessFilter { get; init; } = static _ => true;
    public Sampler RegistrationSampler { get; init; } = Sampler.EveryNth(8);
    public Sampler RegistrationLogSampler { get; init; } = Sampler.Never; // Applied after RegistrationSampler!
    public Action<Dictionary<string, (int, int)>>? AccessStatisticsPreprocessor { get; init; }
    public Action<Dictionary<string, (int, int)>>? RegistrationStatisticsPreprocessor { get; init; }

    public FusionMonitor(IServiceProvider services)
    {
        Services = services;
        Log = services.LogFor(GetType());
        _onAccess = OnAccess;
        _onRegister = input => OnRegistration(input, true);
        _onUnregister = input => OnRegistration(input, false);
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        try {
            Attach();
            Log.LogInformation("Running");

            // We want to format this log message as quickly as possible, so use StringBuilder here
            var sb = new StringBuilder();
            var fp = CultureInfo.InvariantCulture;
            while (!cancellationToken.IsCancellationRequested) {
                var sleepDelay = SleepPeriod.Next();
                if (sleepDelay > TimeSpan.Zero) {
                    Detach();
                    Log.LogInformation("Sleeping for {SleepPeriod}...", sleepDelay);
                    await Task.Delay(sleepDelay, cancellationToken).ConfigureAwait(false);
                    Attach();
                }

                Log.LogInformation("Collecting for {CollectPeriod}...", CollectPeriod);
                await Task.Delay(CollectPeriod, cancellationToken).ConfigureAwait(false);
                var (accesses, registrations) = GetAndResetStatistics();
                AccessStatisticsPreprocessor?.Invoke(accesses);
                RegistrationStatisticsPreprocessor?.Invoke(registrations);

                // Accesses
                if (accesses.Count != 0) {
                    var m = AccessSampler.InverseProbability;
                    sb.AppendFormat(fp, "Reads, sampled with {0}:", AccessSampler);
                    var hitSum = 0;
                    var missSum = 0;
                    foreach (var (key, (hits, misses)) in accesses.OrderByDescending(kv => kv.Value.Item1 + kv.Value.Item2)) {
                        hitSum += hits;
                        missSum += misses;
                        var reads = hits + misses;
                        sb.Append("\r\n- ");
                        sb.Append(key);
                        sb.AppendFormat(fp, ": {0:F1} reads -> {1:P2} hits", reads * m, (double)hits / reads);
                    }
                    var readSum = hitSum + missSum;
                    sb.AppendFormat(fp, "\r\nTotal: {0:F1} reads -> {1:P2} hits", readSum * m, (double)hitSum / readSum);
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    Log.LogInformation(sb.ToString());
                    sb.Clear();
                }

                // Registrations
                if (registrations.Count != 0) {
                    var m = RegistrationSampler.InverseProbability;
                    sb.AppendFormat(fp, "Updates (+) and invalidations (-), sampled with {0}:", RegistrationSampler);
                    var addSum = 0;
                    var subSum = 0;
                    foreach (var (key, (adds, subs)) in registrations.OrderByDescending(kv => kv.Value.Item1)) {
                        addSum += adds;
                        subSum += subs;
                        sb.Append("\r\n- ");
                        sb.Append(key);
                        sb.AppendFormat(fp, ": +{0:F1} -{1:F1}", adds * m, subs * m);
                    }
                    sb.AppendFormat(fp, "\r\nTotal: +{0:F1} -{1:F1}", addSum * m, subSum * m);
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    Log.LogInformation(sb.ToString());
                    sb.Clear();
                }
            }
        }
        finally {
            Detach();
            GetAndResetStatistics();
        }
    }

    // Private methods

    private void Attach()
    {
        GetAndResetStatistics();
        var registry = ComputedRegistry.Instance;
        registry.OnAccess += _onAccess;
        registry.OnRegister += _onRegister;
        registry.OnUnregister += _onUnregister;
    }

    private void Detach()
    {
        var registry = ComputedRegistry.Instance;
        registry.OnAccess -= _onAccess;
        registry.OnRegister -= _onRegister;
        registry.OnUnregister -= _onUnregister;
    }

    private (Dictionary<string, (int, int)> Accesses, Dictionary<string, (int, int)> Registrations) GetAndResetStatistics()
    {
        lock (_lock) {
            var gets = _accesses;
            var registrations = _registrations;
            _accesses = new(StringComparer.Ordinal);
            _registrations = new(StringComparer.Ordinal);
            return (gets, registrations);
        }
    }

    // Event handlers

    private void OnAccess(IComputed computed, bool isNew)
    {
        if (AccessSampler.Next())
            return;
        if (!AccessFilter.Invoke(computed))
            return;

        var input = computed.Input;
        var category = input.Category;
        var dHit = isNew ? 0 : 1;
        var dMiss = 1 - dHit;
        lock (_lock) {
            if (_accesses.TryGetValue(category, out var counts))
                _accesses[category] = (counts.Item1 + dHit, counts.Item2 + dMiss);
            else
                _accesses[category] = (dHit, dMiss);
        }
    }

    private void OnRegistration(IComputed computed, bool isRegistration)
    {
        if (RegistrationSampler.Next())
            return;

        var input = computed.Input;
        var category = input.Category;
        var dAdd = isRegistration ? 1 : 0;
        var dRemove = 1 - dAdd;
        lock (_lock) {
            if (_registrations.TryGetValue(category, out var counts))
                _registrations[category] = (counts.Item1 + dAdd, counts.Item2 + dRemove);
            else
                _registrations[category] = (dAdd, dRemove);
        }

        if (RegistrationLogSampler.Next())
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            Log.LogDebug(ZString.Concat(isRegistration ? "+ " : "- ", input));
    }
}
