using FluentAssertions;

namespace Stl.Testing;

public static class ChannelTestExt
{
    public static async Task AssertWrite<T>(
        this ChannelWriter<T> writer, T item, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(1);
        using var timeoutCts = new CancellationTokenSource();
        var timeoutToken = timeoutCts.Token;
        timeoutCts.CancelAfter(timeout.GetValueOrDefault());
        try {
            await writer.WriteAsync(item, timeoutToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            timeoutToken.IsCancellationRequested.Should().BeFalse(
                "the message wasn't read on time");
            throw;
        }
    }

    public static async Task<T> AssertRead<T>(
        this ChannelReader<T> channel, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(1);
        using var timeoutCts = new CancellationTokenSource();
        var timeoutToken = timeoutCts.Token;
        timeoutCts.CancelAfter(timeout.GetValueOrDefault());
        try {
            var hasMoreItems = await channel.WaitToReadAsync(timeoutToken).ConfigureAwait(false);
            hasMoreItems.Should().BeTrue();
            channel.TryRead(out var m).Should().BeTrue();
            return m!;
        }
        catch (OperationCanceledException) {
            timeoutToken.IsCancellationRequested.Should().BeFalse(
                "the message wasn't read on time");
            throw;
        }
    }

    public static async Task AssertCompleted<T>(
        this ChannelReader<T> channel, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(1);
        using var timeoutCts = new CancellationTokenSource();
        var timeoutToken = timeoutCts.Token;
        timeoutCts.CancelAfter(timeout.GetValueOrDefault());

        using var dTimeoutTask = timeoutToken.ToTask();
        await Task.WhenAny(channel.Completion, dTimeoutTask.Resource).ConfigureAwait(false);
        channel.Completion.IsCompleted.Should().BeTrue();
        await channel.Completion.ConfigureAwait(false);
    }

    public static async Task AssertCannotRead<T>(
        this ChannelReader<T> channel, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(0.1);
        using var timeoutCts = new CancellationTokenSource();
        var timeoutToken = timeoutCts.Token;
        timeoutCts.CancelAfter(timeout.GetValueOrDefault());

        Func<Task> tryRead = () => channel.WaitToReadAsync(timeoutToken).AsTask();
        await tryRead.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
    }
}
