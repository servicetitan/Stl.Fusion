using System.Runtime.CompilerServices;

namespace Stl.Fusion.Interception
{
    public class ByRefArgumentHandler : ArgumentHandler
    {
        public static ByRefArgumentHandler Instance { get; } = new();

        private ByRefArgumentHandler()
        {
            GetHashCodeFunc = obj => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
            EqualsFunc = ReferenceEquals;
        }
    }
}
