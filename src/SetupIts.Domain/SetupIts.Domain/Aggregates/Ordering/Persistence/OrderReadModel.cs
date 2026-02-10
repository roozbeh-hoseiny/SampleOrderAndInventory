namespace SetupIts.Domain.Aggregates.Ordering.Persistence;

public sealed record OrderReadModel(
    string Id,
    int CustomerId,
    byte Status,
    byte StatusTitle,
    DateTimeOffset CreatedAt,
    decimal TotalAmount,
    IReadOnlyCollection<OrderItemReadModel> OrderItems);
