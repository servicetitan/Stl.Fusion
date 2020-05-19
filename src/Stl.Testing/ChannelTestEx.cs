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
        public static async Task AssertWriteAsync<T>(
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

        public static async Task<T> AssertReadAsync<T>(
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
                return m;
            }
            catch (OperationCanceledException) {
                timeoutToken.IsCancellationRequested.Should().BeFalse(
                    "the message wasn't read on time");
                throw;
            }
        }

        public static async Task AssertCompletedAsync<T>(
            this ChannelReader<T> reader, TimeSpan? timeout = default)
        {
            timeout ??= TimeSpan.FromSeconds(1);
            using var timeoutCts = new CancellationTokenSource();
            var timeoutToken = timeoutCts.Token;
            timeoutCts.CancelAfter(timeout.GetValueOrDefault());

            var timeoutTask = timeoutToken.ToTaskSource(false).Task;
            await Task.WhenAny(reader.Completion, timeoutTask).ConfigureAwait(false);
            reader.Completion.IsCompleted.Should().BeTrue();
        }

        public static async Task AssertCannotReadAsync<T>(
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
