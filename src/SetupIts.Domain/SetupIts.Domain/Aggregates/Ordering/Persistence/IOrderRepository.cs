using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Ordering.Persistence;
public interface IOrderRepository : IGenericDomainRepository<Order, OrderId>
{
    Task<PrimitiveResult<byte[]>> UpdateStatus(Order entity, CancellationToken cancellationToken);
    Task<PrimitiveResult<Order>> GetOneWithItems(OrderId id, CancellationToken cancellationToken);
}
