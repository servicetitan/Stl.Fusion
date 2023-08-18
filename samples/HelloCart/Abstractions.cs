using System.Runtime.Serialization;
using MemoryPack;
using Newtonsoft.Json;

namespace Samples.HelloCart;

[DataContract, MemoryPackable]
public partial record Product : IHasId<string>
{
    [DataMember] public string Id { get; init; } = "";
    [DataMember] public decimal Price { get; init; } = 0;
}

[DataContract, MemoryPackable]
public partial record Cart : IHasId<string>
{
    [DataMember] public string Id { get; init; } = "";
    [DataMember] public ImmutableDictionary<string, decimal> Items { get; init; } = ImmutableDictionary<string, decimal>.Empty;
}

[DataContract, MemoryPackable]
public partial record EditCommand<TItem> : ICommand<Unit>
    where TItem : class, IHasId<string>
{
    [DataMember] public string Id { get; init; }
    [DataMember] public TItem? Item { get; init; }

    public EditCommand(TItem value) : this(value.Id, value) { }

    [JsonConstructor, MemoryPackConstructor]
    public EditCommand(string id, TItem item)
    {
        Id = id;
        Item = item;
    }

    public void Deconstruct(out string id, out TItem? item)
    {
        id = Id;
        item = Item;
    }
}

public interface IProductService: IComputeService
{
    [ComputeMethod]
    Task<Product?> Get(string id, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task Edit(EditCommand<Product> command, CancellationToken cancellationToken = default);
}

public interface ICartService: IComputeService
{
    [ComputeMethod]
    Task<Cart?> Get(string id, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<decimal> GetTotal(string id, CancellationToken cancellationToken = default);

    [CommandHandler]
    Task Edit(EditCommand<Cart> command, CancellationToken cancellationToken = default);
}
