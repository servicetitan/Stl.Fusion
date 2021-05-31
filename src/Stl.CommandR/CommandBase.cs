using System;

namespace Stl.CommandR
{
    public abstract record CommandBase<TResult> : ICommand<TResult>
    {
        Type ICommand.ResultType => typeof(TResult);
    }
}
