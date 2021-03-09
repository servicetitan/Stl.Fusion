using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using static System.Console;

namespace Samples.HelloCart
{
    public static class AutoRunner
    {
        public static async Task RunAsync(AppBase app, CancellationToken cancellationToken = default)
        {
            var rnd = new Random(10);
            for (var i = 0;; i++) {
                var productId = app.ExistingProducts[rnd.Next(app.ExistingProducts.Length)].Id;
                var product = await app.ClientProductService.FindAsync(productId);
                var price = rnd.Next(10);
                var command = new EditCommand<Product>(product! with { Price = price });
                WriteLine(command);
                await app.ClientServices.Commander().CallAsync(command);
                await Task.Delay(2000);
            }
        }
    }
}
