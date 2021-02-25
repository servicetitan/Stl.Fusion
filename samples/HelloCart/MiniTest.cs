using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.CommandR;

namespace Samples.HelloCart
{
    public static class MiniTest
    {
        public static async Task RunAsync(AppBase app, CancellationToken cancellationToken = default)
        {
            var rnd = new Random(10);
            for (var i = 0;; i++) {
                for (var j = 0; j<200; j++) {
                    var command = new EditCommand<Product>(app.ExistingProducts[1] with { Price = 0 });
                    app.ClientServices.Commander().CallAsync(command).Ignore();
                }
                await Task.Delay(500);
            }
        }
    }
}
