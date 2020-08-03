using Stl.Text;

namespace Stl.DependencyInjection
{
    public static class ServiceScope
    {
        public static readonly Symbol Default = Symbol.Empty;
        public static readonly Symbol ManualRegistration = nameof(ManualRegistration);
    }
}
