using Microsoft.EntityFrameworkCore;
using Samples.HelloCart.V2;
using static System.Console;

namespace Samples.HelloCart;

public abstract class AppBase
{
    public IServiceProvider ServerServices { get; protected set; } = null!;
    public IServiceProvider ClientServices { get; protected set; } = null!;
    public virtual IServiceProvider WatchedServices => ClientServices;

    public Product[] ExistingProducts { get; set; } = Array.Empty<Product>();
    public Cart[] ExistingCarts { get; set; } = Array.Empty<Cart>();

    public virtual async Task InitializeAsync(IServiceProvider services)
    {
        var dbContextFactory = services.GetService<IDbContextFactory<AppDbContext>>();
        if (dbContextFactory != null) {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var commander = services.Commander();

        var pApple = new Product { Id = "apple", Price = 2M };
        var pBanana = new Product { Id = "banana", Price = 0.5M };
        var pCarrot = new Product { Id = "carrot", Price = 1M };
        ExistingProducts = new [] { pApple, pBanana, pCarrot };
        foreach (var product in ExistingProducts)
            await commander.Call(new EditCommand<Product>(product));

        var cart1 = new Cart() { Id = "cart:apple=1,banana=2",
            Items = ImmutableDictionary<string, decimal>.Empty
                .Add(pApple.Id, 1)
                .Add(pBanana.Id, 2)
        };
        var cart2 = new Cart() { Id = "cart:banana=1,carrot=1",
            Items = ImmutableDictionary<string, decimal>.Empty
                .Add(pBanana.Id, 1)
                .Add(pCarrot.Id, 1)
        };
        ExistingCarts = new [] { cart1, cart2 };
        foreach (var cart in ExistingCarts)
            await commander.Call(new EditCommand<Cart>(cart));
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (ClientServices is IAsyncDisposable csd)
            await csd.DisposeAsync();
        if (ServerServices is IAsyncDisposable sd)
            await sd.DisposeAsync();
    }

    public Task Watch(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        foreach (var product in ExistingProducts)
            tasks.Add(WatchProduct(services, product.Id, cancellationToken));
        foreach (var cart in ExistingCarts)
            tasks.Add(WatchCartTotal(services, cart.Id, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public async Task WatchProduct(
        IServiceProvider services, string productId, CancellationToken cancellationToken = default)
    {
        var productService = services.GetRequiredService<IProductService>();
        var computed = await Computed.Capture(() => productService.Get(productId, cancellationToken));
        while (true) {
            WriteLine($"  {computed.Value}");
            await computed.WhenInvalidated(cancellationToken);
            computed = await computed.Update(cancellationToken);
        }
    }

    public async Task WatchCartTotal(
        IServiceProvider services, string cartId, CancellationToken cancellationToken = default)
    {
        var cartService = services.GetRequiredService<ICartService>();
        var computed = await Computed.Capture(() => cartService.GetTotal(cartId, cancellationToken));
        while (true) {
            WriteLine($"  {cartId}: total = {computed.Value}");
            await computed.WhenInvalidated(cancellationToken);
            computed = await computed.Update(cancellationToken);
        }
    }
}
