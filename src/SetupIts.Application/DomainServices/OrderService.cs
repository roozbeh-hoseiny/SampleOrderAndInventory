using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.DomainServices;
internal sealed class OrderService : IOrderService
{
    #region " Fields "
    private readonly IOrderRepository _orderWriteRepository;
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IValidCustomerSpecification _validCustomerSpecification;
    private readonly IValidProductSpecification _validProductSpecification;
    #endregion

    public OrderService(
        IOrderRepository orderWriteRepository,
        IOrderReadRepository orderReadRepository,
        IValidCustomerSpecification validCustomerSpecification,
        IValidProductSpecification validProductSpecification)
    {
        this._orderWriteRepository = orderWriteRepository;
        this._orderReadRepository = orderReadRepository;
        this._validCustomerSpecification = validCustomerSpecification;
        this._validProductSpecification = validProductSpecification;
    }

    public async Task<PrimitiveResult<OrderId>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        return await Order.Create(
            request.CustomerId,
            request.OrderItems,
            this._validCustomerSpecification,
            this._validProductSpecification,
            cancellationToken)
            .Map(newOrder => this._orderWriteRepository.Add(newOrder, cancellationToken)
                .Map(_ => PrimitiveResult.Success(newOrder.Id)))
            .ConfigureAwait(false);
    }

    public Task<PrimitiveResult> ConfirmOrder(ConfirmOrderRequest request, CancellationToken cancellationToken) =>
        this.ChangeOrderStatus(request.Id, order => order.Confirm(), cancellationToken);

    public Task<PrimitiveResult> CancelOrder(CancelOrderRequest request, CancellationToken cancellationToken) =>
        this.ChangeOrderStatus(request.Id, order => order.Cancel(), cancellationToken);

    public async Task<PrimitiveResult<OrderReadModel>> GetOne(OrderId Id, CancellationToken cancellationToken) => await this._orderReadRepository.GetOne(Id, cancellationToken);


    async Task<PrimitiveResult> ChangeOrderStatus(
        OrderId id,
        Func<Order, PrimitiveResult> func,
        CancellationToken cancellationToken)
    {
        var order = await this._orderWriteRepository.GetOne(id, cancellationToken)
            .ConfigureAwait(false);

        if (order.IsFailure) return PrimitiveResult.Failure(order.Errors);

        var changeStatusResult = func.Invoke(order.Value);

        if (changeStatusResult.IsFailure) return PrimitiveResult.Failure(changeStatusResult.Errors);

        var result = await this._orderWriteRepository
            .UpdateStatus(order.Value, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure) return PrimitiveResult.Failure(changeStatusResult.Errors);

        return PrimitiveResult.Success();
    }
}
