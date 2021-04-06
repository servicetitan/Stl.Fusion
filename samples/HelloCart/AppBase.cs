using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion;
using static System.Console;

namespace Samples.HelloCart
{
    public abstract class AppBase
    {
        public IServiceProvider HostServices { get; protected set; } = null!;
        public IProductService HostProductService => HostServices.GetRequiredService<IProductService>();
        public ICartService HostCartService => HostServices.GetRequiredService<ICartService>();

        public IServiceProvider ClientServices { get; protected set; } = null!;
        public IProductService ClientProductService => ClientServices.GetRequiredService<IProductService>();
        public ICartService ClientCartService => ClientServices.GetRequiredService<ICartService>();

        public Product[] ExistingProducts { get; set; } = Array.Empty<Product>();
        public Cart[] ExistingCarts { get; set; } = Array.Empty<Cart>();
        public virtual IServiceProvider WatchServices => ClientServices;

        public virtual async Task InitializeAsync()
        {
            var pApple = new Product { Id = "apple", Price = 2M };
            var pBanana = new Product { Id = "banana", Price = 0.5M };
            var pCarrot = new Product { Id = "carrot", Price = 1M };
            ExistingProducts = new [] { pApple, pBanana, pCarrot };
            foreach (var product in ExistingProducts)
                await HostProductService.Edit(new EditCommand<Product>(product));

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
                await HostCartService.Edit(new EditCommand<Cart>(cart));
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (ClientServices is IAsyncDisposable csd)
                await csd.DisposeAsync();
            if (HostServices is IAsyncDisposable sd)
                await sd.DisposeAsync();
        }

        public Task Watch(CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            foreach (var product in ExistingProducts)
                tasks.Add(WatchProduct(product.Id, cancellationToken));
            foreach (var cart in ExistingCarts)
                tasks.Add(WatchCartTotal(cart.Id, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public async Task WatchProduct(string productId, CancellationToken cancellationToken = default)
        {
            var productService = WatchServices.GetRequiredService<IProductService>();
            var computed = await Computed.Capture(ct => productService.TryGet(productId, ct), cancellationToken);
            for (;;) {
                WriteLine($"  {computed.Value}");
                await computed.WhenInvalidated(cancellationToken);
                computed = await computed.Update(cancellationToken);
            }
        }

        public async Task WatchCartTotal(string cartId, CancellationToken cancellationToken = default)
        {
            var cartService = WatchServices.GetRequiredService<ICartService>();
            var computed = await Computed.Capture(ct => cartService.GetTotal(cartId, ct), cancellationToken);
            for (;;) {
                WriteLine($"  {cartId}: total = {computed.Value}");
                await computed.WhenInvalidated(cancellationToken);
                computed = await computed.Update(cancellationToken);
            }
        }
    }
}
