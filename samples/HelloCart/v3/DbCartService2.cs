using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Samples.HelloCart.V2;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.EntityFramework;

namespace Samples.HelloCart.V3
{
    public class DbCartService2 : DbServiceBase<AppDbContext>, ICartService
    {
        private readonly IProductService _products;
        private readonly DbEntityResolver<AppDbContext, string, DbCart> _cartResolver;

        public DbCartService2(
            IServiceProvider services,
            IProductService products,
            DbEntityResolver<AppDbContext, string, DbCart> cartResolver)
            : base(services)
        {
            _products = products;
            _cartResolver = cartResolver;
        }

        public virtual async Task EditAsync(EditCommand<Cart> command, CancellationToken cancellationToken = default)
        {
            var (cartId, cart) = command;
            if (string.IsNullOrEmpty(cartId))
                throw new ArgumentOutOfRangeException(nameof(command));
            if (Computed.IsInvalidating()) {
                FindAsync(cartId, default).Ignore();
                return;
            }

            await using var dbContext = await CreateCommandDbContextAsync(cancellationToken);
            var dbCart = await dbContext.Carts.FindAsync(ComposeKey(cartId), cancellationToken);
            if (cart == null) {
                if (dbCart != null)
                    dbContext.Remove(dbCart);
            }
            else {
                if (dbCart != null) {
                    await dbContext.Entry(dbCart).Collection(c => c.Items).LoadAsync(cancellationToken);
                    // Removing what doesn't exist in cart.Items
                    dbCart.Items.RemoveAll(i => !cart.Items.ContainsKey(i.DbProductId));
                    // Updating the ones that exist in both collections
                    foreach (var dbCartItem in dbCart.Items)
                        dbCartItem.Quantity = cart.Items[dbCartItem.DbProductId];
                    // Adding the new ones
                    var existingProductIds = dbCart.Items.Select(i => i.DbProductId).ToHashSet();
                    foreach (var item in cart.Items.Where(i => !existingProductIds.Contains(i.Key))) {
                        var dbCartItem = new DbCartItem() {
                            DbCartId = cartId,
                            DbProductId = item.Key,
                            Quantity = item.Value,
                        };
                        dbCart.Items.Add(dbCartItem);
                    }
                }
                else
                    dbContext.Add(new DbCart() {
                        Id = cartId,
                        Items = cart.Items.Select(i => new DbCartItem() {
                            DbCartId = cartId,
                            DbProductId = i.Key,
                            Quantity = i.Value,
                        }).ToList(),
                    });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<Cart?> FindAsync(string id, CancellationToken cancellationToken = default)
        {
            var dbCart = await _cartResolver.TryGetAsync(id, cancellationToken);
            if (dbCart == null)
                return null;
            return new Cart() {
                Id = dbCart.Id,
                Items = dbCart.Items.ToImmutableDictionary(i => i.DbProductId, i => i.Quantity),
            };
        }

        public virtual async Task<decimal> GetTotalAsync(string id, CancellationToken cancellationToken = default)
        {
            var cart = await FindAsync(id, cancellationToken);
            if (cart == null)
                return 0;
            var itemTotals = await Task.WhenAll(cart.Items.Select(async item => {
                var product = await _products.FindAsync(item.Key, cancellationToken);
                return item.Value * (product?.Price ?? 0M);
            }));
            return itemTotals.Sum();
        }
    }
}
