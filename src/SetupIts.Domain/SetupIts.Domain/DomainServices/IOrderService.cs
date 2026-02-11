using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.DomainServices;
public interface IOrderService
{
    Task<PrimitiveResult<OrderId>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<PrimitiveResult> ConfirmOrder(ConfirmOrderRequest request, CancellationToken cancellationToken);
    Task<PrimitiveResult> CancelOrder(CancelOrderRequest request, CancellationToken cancellationToken);

    Task<PrimitiveResult<OrderReadModel>> GetOne(OrderId Id, CancellationToken cancellationToken);
}
public sealed record CreateOrderRequest(int CustomerId, OrderItemCreateData[] OrderItems);
public sealed record ConfirmOrderRequest(OrderId Id);
public sealed record CancelOrderRequest(OrderId Id);

