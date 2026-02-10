using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Ordering;

public sealed record OrderItemCreateData(
    ProductId ProductId,
    Quantity Quantity,
    UnitPrice UnitPrice
);