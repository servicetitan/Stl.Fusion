using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Samples.HelloCart;
using Samples.HelloCart.V1;
using Samples.HelloCart.V2;
using Samples.HelloCart.V3;
using Samples.HelloCart.V4;
using Samples.HelloCart.V5;
using Stl.Async;
using Stl.CommandR;
using static System.Console;

// Create services
AppBase? app;
var isFirstTry = true;
while(true) {
    WriteLine("Select the implementation to use:");
    WriteLine("  1: ConcurrentDictionary-based");
    WriteLine("  2: EF Core + Operations Framework (OF)");
    WriteLine("  3: EF Core + OF + DbEntityResolvers (pipelined fetches)");
    WriteLine("  4: EF Core + OF + DbEntityResolvers + Client-Server");
    WriteLine("  5: EF Core + OF + DbEntityResolvers + Client-Server + Multi-Host");
    // WriteLine("  4: 3 + client-server mode");
    Write("Type 1..5: ");
    var input = isFirstTry
        ? args.SingleOrDefault() ?? ReadLine()
        : ReadLine();
    input = (input ?? "").Trim();
    app = input switch {
        "1" => new AppV1(),
        "2" => new AppV2(),
        "3" => new AppV3(),
        "4" => new AppV4(),
        "5" => new AppV5(),
        _ => null,
    };
    if (app != null)
        break;
    WriteLine("Invalid selection.");
    WriteLine();
    isFirstTry = false;
}
await using var appDisposable = app;
await app.InitializeAsync();

// Starting watch tasks
WriteLine("Initial state:");
using var cts = new CancellationTokenSource();
_ = app.Watch(cts.Token);
await Task.Delay(700); // Just to make sure watch tasks print whatever they want before our prompt appears
// await AutoRunner.Run(app);

WriteLine();
WriteLine("Change product price by typing [productId]=[price], e.g. \"apple=0\".");
WriteLine("See the total of every affected cart changes.");
while (true) {
    await Task.Delay(500);
    WriteLine();
    Write("[productId]=[price]: ");
    try {
        var input = (ReadLine() ?? "").Trim();
        if (input == "")
            break;
        var parts = input.Split("=");
        if (parts.Length != 2)
            throw new ApplicationException("Invalid price expression.");
        var productId = parts[0].Trim();
        var price = decimal.Parse(parts[1].Trim());
        var product = await app.ClientProductService.Get(productId);
        if (product == null)
            throw new KeyNotFoundException("Specified product doesn't exist.");
        var command = new EditCommand<Product>(product with { Price = price });
        await app.ClientProductService.Edit(command);
        // You can run absolutely identical action with:
        // await app.ClientServices.Commander().Call(command);
    }
    catch (Exception e) {
        WriteLine($"Error: {e.Message}");
    }
}
WriteLine("Terminating...");
cts.Cancel();
