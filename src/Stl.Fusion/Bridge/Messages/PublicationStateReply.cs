using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Stl.Serialization;

namespace Stl.Fusion.Bridge.Messages
{
    [DataContract]
    public abstract class PublicationStateReply : PublicationReply
    {
        [DataMember(Order = 3)]
        public LTag Version { get; set; }
        [DataMember(Order = 4)]
        public bool IsConsistent { get; set; }

        public abstract Type GetResultType();
    }

    [DataContract]
    public class PublicationStateReply<T> : PublicationStateReply
    {
        private static readonly ConcurrentDictionary<Type, Func<Result<T>, PublicationStateReply<T>>> NewInstanceCache =
            new();
        private static readonly MethodInfo NewInternalMethod =
            typeof(PublicationStateReply<T>).GetMethod(nameof(NewInternal), BindingFlags.Static | BindingFlags.NonPublic)!;

        [IgnoreDataMember, JsonIgnore]
        public virtual Result<T>? Output => new Result<T>(default!, null);

        public override Type GetResultType() => typeof(T);

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

    [DataContract]
    public class PublicationStateReply<T, TActual> : PublicationStateReply<T>
        where TActual : T
    {
        [DataMember(Order = 5)]
        public TActual? Value { get; set; }
        [DataMember(Order = 6)]
        public ExceptionInfo Error { get; set; }
        [IgnoreDataMember, JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public override Result<T>? Output => new Result<T>(Value!, Error.ToException());
    }
}
