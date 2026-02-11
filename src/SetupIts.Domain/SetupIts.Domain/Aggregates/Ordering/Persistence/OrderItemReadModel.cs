namespace SetupIts.Domain.Aggregates.Ordering.Persistence;

public sealed record class OrderItemReadModel
{
    public string Id { get; init; } = default!;
    public string ProductId { get; init; } = default!;
    public string ProductName { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public int Qty { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice => this.Qty * this.UnitPrice;
}
