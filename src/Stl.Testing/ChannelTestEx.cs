using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.Async;

namespace Stl.Testing
{
    public static class ChannelTestEx
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
            this ChannelReader<T> reader, TimeSpan? timeout = default)
        {
            timeout ??= TimeSpan.FromSeconds(1);
            using var timeoutCts = new CancellationTokenSource();
            var timeoutToken = timeoutCts.Token;
            timeoutCts.CancelAfter(timeout.GetValueOrDefault());
            try {
                var hasMoreItems = await reader.WaitToReadAsync(timeoutToken).ConfigureAwait(false);
                hasMoreItems.Should().BeTrue();
                reader.TryRead(out var m).Should().BeTrue();
                return m!;
            }
            catch (OperationCanceledException) {
                timeoutToken.IsCancellationRequested.Should().BeFalse(
                    "the message wasn't read on time");
                throw;
            }
        }

        public static async Task AssertCompleted<T>(
            this ChannelReader<T> reader, TimeSpan? timeout = default)
        {
            timeout ??= TimeSpan.FromSeconds(1);
            using var timeoutCts = new CancellationTokenSource();
            var timeoutToken = timeoutCts.Token;
            timeoutCts.CancelAfter(timeout.GetValueOrDefault());

            using var dTimeoutTask = timeoutToken.ToTask();
            await Task.WhenAny(reader.Completion, dTimeoutTask.Resource).ConfigureAwait(false);
            reader.Completion.IsCompleted.Should().BeTrue();
        }

        public static async Task AssertCannotRead<T>(
            this ChannelReader<T> reader, TimeSpan? timeout = default)
        {
            timeout ??= TimeSpan.FromSeconds(0.1);
            using var timeoutCts = new CancellationTokenSource();
            var timeoutToken = timeoutCts.Token;
            timeoutCts.CancelAfter(timeout.GetValueOrDefault());

            Func<Task> tryRead = () => reader.WaitToReadAsync(timeoutToken).AsTask();
            await tryRead.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        }
    }
}
