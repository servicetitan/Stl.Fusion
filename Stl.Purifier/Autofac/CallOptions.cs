using System;
using System.Collections.Generic;

namespace Stl.Purifier.Autofac
{
    public class CallOptions
    {
        private static readonly Dictionary<CallAction, CallOptions> _cache;
        public static readonly CallOptions Default;

        public CallAction Action { get; }
        public object? InvalidatedBy { get; }

        public static CallOptions New(CallAction action, object? invalidatedBy = null) 
            => invalidatedBy == null ? _cache[action] : new CallOptions(action, invalidatedBy);

        static CallOptions()
        {
            var allActions = CallAction.CaptureComputed | CallAction.TryGetCached |  CallAction.Invalidate;
            var cache = new Dictionary<CallAction, CallOptions>();
            for (var i = 0; i < (int) allActions; i++) {
                var action = (CallAction) i;
                cache[action] = new CallOptions(action, null);
            }
            _cache = cache;
            Default = New(default);
        }

        private CallOptions(CallAction action, object? invalidatedBy)
        {
            Action = action;
            InvalidatedBy = invalidatedBy;
        }

        public static implicit operator CallOptions(CallAction action) => _cache[action];

        public override string ToString() => $"{GetType().Name}({Action}, {InvalidatedBy ?? "null"})";
    }
}
