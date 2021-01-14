using System;

namespace Stl.CommandR
{
    public interface ICommand
    {
        Type ResultType { get; }
    }

    public interface ICommand<TResult> : ICommand
    {
        Type ICommand.ResultType => typeof(TResult);
    }
}
