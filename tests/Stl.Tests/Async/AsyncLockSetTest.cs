using System.Reflection;
using Stl.Locking;
using Stl.Generators;
using Stl.Locking.Internal;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class AsyncLockSetTest : AsyncLockTestBase
{
    protected AsyncLockSet<string> CheckedFailSet { get; } = new(LockReentryMode.CheckedFail);
    protected AsyncLockSet<string> CheckedPassSet { get; } = new(LockReentryMode.CheckedPass);
    protected AsyncLockSet<string> UncheckedSet { get; } = new(LockReentryMode.Unchecked);

    protected class AsyncSetLock<TKey> : IAsyncLock
        where TKey : notnull
    {
        public AsyncLockSet<TKey> LockSet { get; }
        public TKey Key { get; }

        public AsyncSetLock(AsyncLockSet<TKey> lockSet, TKey key)
        {
            LockSet = lockSet;
            Key = key;
        }

        public ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
        {
            var task = LockSet.Lock(Key, cancellationToken);
            return AsyncLockReleaser.NewWhenCompleted(task, this);
        }

        public void Release()
            => LockSet.Release(Key);
    }

    public AsyncLockSetTest(ITestOutputHelper @out) : base(@out) { }

    protected override IAsyncLock CreateAsyncLock(LockReentryMode reentryMode)
    {
        var key = RandomStringGenerator.Default.Next();
        switch (reentryMode) {
        case LockReentryMode.CheckedFail:
            return new AsyncSetLock<string>(CheckedFailSet, key);
        case LockReentryMode.CheckedPass:
            return new AsyncSetLock<string>(CheckedPassSet, key);
        case LockReentryMode.Unchecked:
            return new AsyncSetLock<string>(UncheckedSet, key);
        default:
            throw new ArgumentOutOfRangeException(nameof(reentryMode), reentryMode, null);
        }
    }

    protected override void AssertResourcesReleased()
    {
        void AssertIsEmpty(AsyncLockSet<string> asyncLockSet)
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
        AssertIsEmpty(UncheckedSet);
    }
}
