using System.Reflection;
using Stl.Locking;
using Stl.Generators;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class AsyncLockSetTest : AsyncLockTestBase
{
    protected AsyncLockSet<string> CheckedFailSet { get; } =
        new AsyncLockSet<string>(ReentryMode.CheckedFail);
    protected AsyncLockSet<string> CheckedPassSet { get; } =
        new AsyncLockSet<string>(ReentryMode.CheckedPass);
    protected AsyncLockSet<string> UncheckedDeadlockSet { get; } =
        new AsyncLockSet<string>(ReentryMode.UncheckedDeadlock);

    protected class AsyncSetLock<TKey> : IAsyncLock
        where TKey : notnull
    {
        public IAsyncLockSet<TKey> LockSet { get; }
        public TKey Key { get; }

        public ReentryMode ReentryMode => LockSet.ReentryMode;
        public bool IsLocked => LockSet.IsLocked(Key);
        public bool? IsLockedLocally => LockSet.IsLockedLocally(Key);
        public ValueTask<IDisposable> Lock(CancellationToken cancellationToken = default)
            => LockSet.Lock(Key, cancellationToken);

        public AsyncSetLock(IAsyncLockSet<TKey> lockSet, TKey key)
        {
            LockSet = lockSet;
            Key = key;
        }
    }

    public AsyncLockSetTest(ITestOutputHelper @out) : base(@out) { }

    protected override IAsyncLock CreateAsyncLock(ReentryMode reentryMode)
    {
        var key = RandomStringGenerator.Default.Next();
        switch (reentryMode) {
        case ReentryMode.CheckedFail:
            return new AsyncSetLock<string>(CheckedFailSet, key);
        case ReentryMode.CheckedPass:
            return new AsyncSetLock<string>(CheckedPassSet, key);
        case ReentryMode.UncheckedDeadlock:
            return new AsyncSetLock<string>(UncheckedDeadlockSet, key);
        default:
            throw new ArgumentOutOfRangeException(nameof(reentryMode), reentryMode, null);
        }
    }

    protected override void AssertResourcesReleased()
    {
        void AssertIsEmpty(IAsyncLockSet<string> asyncLockSet)
        {
            var fEntries = asyncLockSet.GetType().GetField("_entries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var entries = fEntries!.GetValue(asyncLockSet);
            entries.Should().NotBeNull();

            var pCount = entries!.GetType().GetProperty("Count");
            var count = (int) pCount!.GetValue(entries)!;
            count.Should().Be(0);
        }

        AssertIsEmpty(CheckedFailSet);
        AssertIsEmpty(CheckedPassSet);
        AssertIsEmpty(UncheckedDeadlockSet);
    }
}
