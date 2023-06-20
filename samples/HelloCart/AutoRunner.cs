using static System.Console;

namespace Samples.HelloCart;

public static class AutoRunner
{
    public static async Task Run(AppBase app, CancellationToken cancellationToken = default)
    {
        var rnd = new Random(10);
        for (var i = 0;; i++) {
            var productId = app.ExistingProducts[rnd.Next(app.ExistingProducts.Length)].Id;
            var product = await app.ClientProductService.Get(productId, cancellationToken);
            var price = rnd.Next(10);
            var command = new EditCommand<Product>(product! with { Price = price });
            WriteLine(command);
            await app.ClientCommander.Call(command, cancellationToken);
            await Task.Delay(2000, cancellationToken);
        }
    }
}
