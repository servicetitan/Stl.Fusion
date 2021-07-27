using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using Stl.Serialization;

namespace Stl.Fusion.Bridge.Messages
{
    public abstract class PublicationStateReply : PublicationReply
    {
        public LTag Version { get; set; }
        public bool IsConsistent { get; set; }
        public abstract Type GetResultType();
    }

    public class PublicationStateReply<T> : PublicationStateReply
    {
        private static readonly ConcurrentDictionary<Type, Func<Result<T>, PublicationStateReply<T>>> NewInstanceCache =
            new();
        private static readonly MethodInfo NewInternalMethod =
            typeof(PublicationStateReply<T>).GetMethod(nameof(NewInternal), BindingFlags.Static | BindingFlags.NonPublic)!;

        public override Type GetResultType() => typeof(T);
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public virtual Result<T>? Output => new Result<T>(default!, null);

        public static PublicationStateReply<T> New(Result<T> output)
            => NewInstanceCache.GetOrAdd(
                output.ValueOrDefault?.GetType() ?? typeof(T),
                tActual => {
                    var mNewInternal = NewInternalMethod.MakeGenericMethod(tActual);
                    var pOutput = Expression.Parameter(typeof(Result<T>));
                    var fnNewInternal = Expression.Lambda<Func<Result<T>, PublicationStateReply<T>>>(
                        Expression.Call(mNewInternal, pOutput),
                        pOutput
                    ).Compile();
                    return fnNewInternal;
                }).Invoke(output);

        private static PublicationStateReply<T> NewInternal<TActual>(Result<T> output)
            where TActual : T
        => new PublicationStateReply<T,TActual>() {
            Value = (TActual?) output.ValueOrDefault,
            Error = output.Error!,
        };
    }

    [Serializable]
    public class PublicationStateReply<T, TActual> : PublicationStateReply<T>
        where TActual : T
    {
        public TActual? Value { get; set; }
        public ExceptionParcel Error { get; set; }
        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public override Result<T>? Output => new Result<T>(Value!, Error.ToException());
    }
}
