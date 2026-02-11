namespace SetupIts.Domain.Aggregates.Ordering.Persistence;

public sealed record class OrderReadModel
{
    public string Id { get; init; } = default!;
    public int CustomerId { get; init; }
    public byte Status { get; init; }
    public string StatusTitle { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
    public decimal TotalAmount => this.OrderItems.Sum(x => x.TotalPrice);
    public IReadOnlyCollection<OrderItemReadModel> OrderItems { get; init; } = Array.Empty<OrderItemReadModel>();
}
