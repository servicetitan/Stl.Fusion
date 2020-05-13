using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Stl.Channels
{
    public static class ChannelEx
    {
        public static async Task<bool> CopyAsync<T>(
            this ChannelReader<T> reader, ChannelWriter<T> writer, bool tryComplete,  
            CancellationToken cancellationToken = default)
        {
            Exception? error = null;
            var result = false;
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (reader.TryRead(out var value))
                        await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) {
                error = e;
                throw;
            }
            finally {
                if (tryComplete)
                    result = writer.TryComplete(error);
            }
            return result;
        }

        public static async Task<bool> TransformAsync<TRead, TWrite>(
            this ChannelReader<TRead> reader, ChannelWriter<TWrite> writer,
            bool tryComplete, Func<TRead, TWrite> adapter,  
            CancellationToken cancellationToken = default)
        {
            Exception? error = null;
            var result = false;
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (reader.TryRead(out var wValue)) {
                        var vWrite = adapter.Invoke(wValue);
                        await writer.WriteAsync(vWrite, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) {
                error = e;
                throw;
            }
            finally {
                if (tryComplete)
                    result = writer.TryComplete(error);
            }
            return result;
        }

        public static Task ConnectAsync<T>(
            this Channel<T> channel1, Channel<T> channel2,
            bool tryComplete, 
            CancellationToken cancellationToken = default) 
            => Task.WhenAll(
                Task.Run(() => channel1.Reader.CopyAsync(channel2, tryComplete, cancellationToken), CancellationToken.None),
                Task.Run(() => channel2.Reader.CopyAsync(channel1, tryComplete, cancellationToken), CancellationToken.None)
            );

        public static Task ConnectAsync<T1, T2>(
            this Channel<T1> channel1, Channel<T2> channel2,
            bool tryComplete,
            Func<T1, T2> adapter12, Func<T2, T1> adapter21,
            CancellationToken cancellationToken = default) 
            => Task.WhenAll(
                Task.Run(() => channel1.Reader.TransformAsync(channel2, tryComplete, adapter12, cancellationToken), CancellationToken.None),
                Task.Run(() => channel2.Reader.TransformAsync(channel1, tryComplete, adapter21, cancellationToken), CancellationToken.None)
            );

        public static async Task ConsumeAsync<T>(this ChannelReader<T> reader, CancellationToken cancellationToken = default)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                reader.TryRead(out var v);
        }

        public static async Task ConsumeSilentAsync<T>(this ChannelReader<T> reader, CancellationToken cancellationToken = default)
        {
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    reader.TryRead(out var v);
            }
            catch {
                // Silent means silent :)
            }
        }
    }
}
