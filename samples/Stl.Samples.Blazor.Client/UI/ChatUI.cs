using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.UI;
using Stl.Samples.Blazor.Client.Services;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.UI
{
    public class ChatUI
    {
        private static ChatUser? _me;

        public long UserCount { get; set; } = 0;
        public long ActiveUserCount { get; set; } = 0;
        public ChatPage LastPage { get; set; } = new ChatPage();

        // Client-side only properties
        public ChatUser? Me { get => _me; set => _me = value; }
        public string MyName { get; set; } = "";
        public string MyMessage { get; set; } = "";
        public Exception? ActionError { get; set; }

        // Updater

        public class Updater : ILiveUpdater<ChatUI>
        {
            protected IChatClient Client { get; }

            public Updater(IChatClient client) => Client = client;

            public virtual async Task<ChatUI> UpdateAsync(
                IComputed<ChatUI> prevComputed, CancellationToken cancellationToken)
            {
                var prevModel = prevComputed.UnsafeValue ?? new ChatUI();
                var userCount = await Client.GetUserCountAsync(cancellationToken);
                var activeUserCount = await Client.GetActiveUserCountAsync(cancellationToken);
                var lastPage = await Client.GetChatTailAsync(30, cancellationToken);
                var model = new ChatUI() {
                    UserCount = userCount,
                    ActiveUserCount = activeUserCount, 
                    LastPage = lastPage,
                    ActionError = prevModel.ActionError,
                    MyMessage = prevModel.MyMessage,
                    MyName = prevModel.MyName,
                };
                var me = model.Me;
                if (model.MyName == "" && me != null)
                    model.MyName = me.Name;
                return model;
            }
        }
    }

}
