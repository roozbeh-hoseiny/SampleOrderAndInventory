namespace SetupIts.Domain.Aggregates.Ordering.Persistence;

public sealed record OrderItemReadModel(
    string Id,
    string ProductId,
    string ProductName,
    int Qty,
    decimal UnitPrice,
    decimal TotalPrice);
