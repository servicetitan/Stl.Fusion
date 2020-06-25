using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.Client;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Client.Services
{
    public class ChatUI : ILiveUpdater<ChatUI.Model>
    {
        public class Model
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
        }

        protected IChatClient Client { get; }

        public ChatUI(IChatClient client) => Client = client;

        public virtual async Task<Model> UpdateAsync(
            IComputed<Model> prevComputed, CancellationToken cancellationToken)
        {
            var prevModel = prevComputed.UnsafeValue ?? new Model();
            var userCount = await Client.GetUserCountAsync(cancellationToken);
            var activeUserCount = await Client.GetActiveUserCountAsync(cancellationToken);
            var lastPage = await Client.GetChatTailAsync(30, cancellationToken);
            var model = new Model() {
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
