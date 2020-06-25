using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.Serialization;

namespace Stl.Channels
{
    public static class ChannelEx
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this Channel<(T Item, ExceptionDispatchInfo? Error)> channel,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var reader = channel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken)) {
                if (!reader.TryRead(out var pair))
                    continue;
                var (item, error) = pair;
                if (error == null)
                    yield return item;
                else
                    error.Throw();
            }
        }

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

        public static async Task ConsumeAsync<T>(
            this ChannelReader<T> reader, 
            CancellationToken cancellationToken = default)
        {
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                reader.TryRead(out var v);
        }

        public static async Task ConsumeSilentAsync<T>(
            this ChannelReader<T> reader, 
            CancellationToken cancellationToken = default)
        {
            try {
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    reader.TryRead(out var v);
            }
            catch {
                // Silent means silent :)
            }
        }

        public static Channel<T> WithSerializers<T, TSerialized>(
            this Channel<TSerialized> downstreamChannel,
            ChannelSerializerPair<T, TSerialized> serializers, 
            BoundedChannelOptions? channelOptions = null,
            CancellationToken cancellationToken = default)
            => downstreamChannel.WithSerializers(
                serializers.Serializer, serializers.Deserializer, 
                channelOptions, cancellationToken);

        public static Channel<T> WithSerializers<T, TSerialized>(
            this Channel<TSerialized> downstreamChannel,
            ITypedSerializer<T, TSerialized> serializer, 
            ITypedSerializer<T, TSerialized> deserializer, 
            BoundedChannelOptions? channelOptions = null,
            CancellationToken cancellationToken = default)
        {
            channelOptions ??= new BoundedChannelOptions(16) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
            };
            var pair = ChannelPair.CreateTwisted(
                Channel.CreateBounded<T>(channelOptions),
                Channel.CreateBounded<T>(channelOptions));

            downstreamChannel.ConnectAsync(
                pair.Channel1, true,
                deserializer.Deserialize, serializer.Serialize,
                cancellationToken);
            return pair.Channel2;
        }

        public static Channel<T> WithLogger<T>(
            this Channel<T> channel,
            string channelName, 
            ILogger logger, LogLevel logLevel, int? maxLength = null,  
            BoundedChannelOptions? channelOptions = null,
            CancellationToken cancellationToken = default)
        {
            channelOptions ??= new BoundedChannelOptions(16) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = true,
            };
            var pair = ChannelPair.CreateTwisted(
                Channel.CreateBounded<T>(channelOptions),
                Channel.CreateBounded<T>(channelOptions));

            T LogMessage(T message)
            {
                var text = message?.ToString() ?? "<null>";
                if (maxLength.HasValue && text.Length > maxLength.GetValueOrDefault())
                    text = text.Substring(0, maxLength.GetValueOrDefault()) + "...";
                logger.Log(logLevel, $"{channelName} <- {text}");
                return message;
            }

            channel.ConnectAsync(
                pair.Channel1, true,
                LogMessage, LogMessage,
                cancellationToken);
            return pair.Channel2;
        }

        public static CustomChannelWithId<TId, T> WithId<TId, T>(
            this Channel<T> channel, TId id)
            => new CustomChannelWithId<TId, T>(id, channel.Reader, channel.Writer);
    }
}
