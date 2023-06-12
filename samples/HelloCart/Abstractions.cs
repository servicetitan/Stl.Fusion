using System.Runtime.Serialization;

namespace Samples.HelloCart;

[DataContract]
public record Product : IHasId<string>
{
    [DataMember] public string Id { get; init; } = "";
    [DataMember] public decimal Price { get; init; } = 0;
}

[DataContract]
public record Cart : IHasId<string>
{
    [DataMember] public string Id { get; init; } = "";
    [DataMember] public ImmutableDictionary<string, decimal> Items { get; init; } = ImmutableDictionary<string, decimal>.Empty;
}

[DataContract]
public record EditCommand<TValue>(
    [property: DataMember] string Id,
    [property: DataMember] TValue? Value = null
    ) : ICommand<Unit>
    where TValue : class, IHasId<string>
{
    public EditCommand(TValue value) : this(value.Id, value) { }
    // Newtonsoft.Json needs this constructor to deserialize this record
    public EditCommand() : this("") { }
}

public interface IProductService: IComputeService
{
    [CommandHandler]
    Task Edit(EditCommand<Product> command, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<Product?> Get(string id, CancellationToken cancellationToken = default);
}

public interface ICartService: IComputeService
{
    [CommandHandler]
    Task Edit(EditCommand<Cart> command, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<Cart?> Get(string id, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<decimal> GetTotal(string id, CancellationToken cancellationToken = default);
}
