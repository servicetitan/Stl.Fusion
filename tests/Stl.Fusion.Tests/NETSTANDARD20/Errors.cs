#if !NETCOREAPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stl.Fusion.Tests
{
    static class Errors
    {
        public static Exception SupportedOnlyInNetCore()
        {
            return new NotSupportedException("SUPPORTED ONLY FOR NET CORE");
        }
    }
}

#endif
