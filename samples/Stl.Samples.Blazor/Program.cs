using System.Threading.Tasks;
using Stl.OS;

namespace Stl.Samples.Blazor
{
    public class Program
    {
        public static Task Main(string[] args) 
            => OSInfo.Kind == OSKind.Wasm 
                ? new ClientProgram().RunAsync(args) 
                : new ServerProgram().RunAsync(args);

    }
}
