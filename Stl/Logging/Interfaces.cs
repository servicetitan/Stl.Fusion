using System;
using Serilog;

namespace Stl
{
    public interface IHasLog
    {
        ILogger Log { get; }
    }
}
