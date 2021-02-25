using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using static System.Console;

namespace Samples.HelloCart
{
    public static class MiniTest
    {
        public static async Task RunAsync(AppBase app, CancellationToken cancellationToken = default)
        {
            var rnd = new Random(10);
            for (var i = 0;; i++) {
                var productId = i <= 0 ? "carrot" : "banana";
                var product = await app.ClientProductService.FindAsync(productId);
                var price = 0;
                var command = new EditCommand<Product>(product! with { Price = price });
                WriteLine(command);
                await app.ClientServices.Commander().CallAsync(command);
                await Task.Delay(2000);
            }
        }
    }
}
