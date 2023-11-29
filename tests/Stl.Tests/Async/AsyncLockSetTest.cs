using System.Reflection;
using Stl.Locking;
using Stl.Generators;
using Stl.Testing.Collections;

namespace Stl.Tests.Async;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class AsyncLockSetTest(ITestOutputHelper @out) : AsyncLockTestBase(@out)
{
    protected AsyncLockSet<string> CheckedFailSet { get; } = new(LockReentryMode.CheckedFail);
    protected AsyncLockSet<string> CheckedPassSet { get; } = new(LockReentryMode.CheckedPass);

    protected sealed class AsyncSetLock<TKey>(AsyncLockSet<TKey> lockSet, TKey key)
        : IAsyncLock<AsyncSetLock<TKey>.Releaser>
        where TKey : notnull
    {
        public AsyncLockSet<TKey> LockSet { get; } = lockSet;
        public TKey Key { get; } = key;

        public LockReentryMode ReentryMode => LockSet.ReentryMode;

        async ValueTask<IAsyncLockReleaser> IAsyncLock.Lock(CancellationToken cancellationToken)
        {
            var releaser = await LockSet.Lock(Key, cancellationToken).ConfigureAwait(false);
            return new Releaser(releaser);
        }

        public async ValueTask<Releaser> Lock(CancellationToken cancellationToken = default)
        {
            var releaser = await LockSet.Lock(Key, cancellationToken).ConfigureAwait(false);
            return new Releaser(releaser);
        }

        // Nested types

        public class Releaser(AsyncLockSet<TKey>.Releaser releaser) : IAsyncLockReleaser
        {
            public void MarkLockedLocally()
                => releaser.MarkLockedLocally();

            public void Dispose()
                => releaser.Dispose();
        }
    }

    protected override IAsyncLock CreateAsyncLock(LockReentryMode reentryMode)
    {
        var key = RandomStringGenerator.Default.Next();
        switch (reentryMode) {
        case LockReentryMode.CheckedFail:
            return new AsyncSetLock<string>(CheckedFailSet, key);
        case LockReentryMode.CheckedPass:
            return new AsyncSetLock<string>(CheckedPassSet, key);
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
    }
}
