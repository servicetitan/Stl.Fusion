using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.Services
{
    public class ClientSideComposerService : IComposerService, IComputedService
    {
        private readonly ILogger _log;
        private readonly ITimeClient _time;
        private readonly IChatClient _chat;

        public ClientSideComposerService(
            ITimeClient time,
            IChatClient chat,
            ILogger<ClientSideComposerService>? log = null)
        {
            _log = log ??= NullLogger<ClientSideComposerService>.Instance;
            _time = time;
            _chat = chat;
        }

        [ComputedServiceMethod]
        public virtual async Task<ComposedValue> GetComposedValueAsync(string parameter, CancellationToken cancellationToken)
        {
            var chatTail = await _chat.GetChatTailAsync(1, cancellationToken).ConfigureAwait(false);
            var time = await _time.GetTimeAsync(cancellationToken).ConfigureAwait(false);
            var lastChatMessage = chatTail.Messages.SingleOrDefault()?.Text ?? "(no messages)";
            var activeUserCount = await _chat.GetActiveUserCountAsync(cancellationToken).ConfigureAwait(false);
            return new ComposedValue($"{parameter} - local", time, lastChatMessage, activeUserCount);
        }
    }
}
