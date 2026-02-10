using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Ordering.Persistence;

public interface IOrderReadRepository
{
    Task<PrimitiveResult<IReadOnlyCollection<OrderReadModel>>> GetOne(OrderId id, CancellationToken cancellationToken);
}
