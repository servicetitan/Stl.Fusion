using System;

namespace Stl.CommandR
{
    public interface ICommand
    {
        Type ResultType { get; }
    }

    public interface ICommand<TResult> : ICommand
    {
        #if !NETSTANDARD2_0
        Type ICommand.ResultType => typeof(TResult);
        #endif
    }
}
