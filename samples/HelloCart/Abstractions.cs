using System.Collections.Immutable;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion;

namespace Samples.HelloCart
{
    public record Product : IHasId<string>
    {
        public string Id { get; init; } = "";
        public decimal Price { get; init; } = 0;
    }

    public record Cart : IHasId<string>
    {
        public string Id { get; init; } = "";
        public ImmutableDictionary<string, decimal> Items { get; init; } = ImmutableDictionary<string, decimal>.Empty;
    }

    public record EditCommand<TValue>(string Id, TValue? Value = null) : ICommand<Unit>
        where TValue : class, IHasId<string>
    {
        public EditCommand(TValue value) : this(value.Id, value) { }
        // Needed just to make JSON deserialization work for this type:
        public EditCommand() : this("", null) { }
    }

    public interface IProductService
    {
        [CommandHandler]
        Task EditAsync(EditCommand<Product> command, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<Product?> FindAsync(string id, CancellationToken cancellationToken = default);
    }

    public interface ICartService
    {
        [CommandHandler]
        Task EditAsync(EditCommand<Cart> command, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<Cart?> FindAsync(string id, CancellationToken cancellationToken = default);
        [ComputeMethod]
        Task<decimal> GetTotalAsync(string id, CancellationToken cancellationToken = default);
    }
}
