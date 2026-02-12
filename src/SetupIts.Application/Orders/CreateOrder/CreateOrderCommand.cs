using SetupIts.Application.Abstractions;
using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.Orders.CreateOrder;
public sealed class CreateOrderCommand : IPrimitiveResultCommand<CreateOrderCommandResponse>
{
    public int CustomerId { get; set; }
    public OrderItemCommand[] Items { get; set; } = [];

    internal static CreateOrderRequest Map(CreateOrderCommand src)
    {
        return new CreateOrderRequest(
            src.CustomerId,
            src.Items.Select(item => new OrderItemCreateData(
                item.ProductId,
                item.Quantity,
                item.UnitPrice)).ToArray());
    }
}
public sealed record OrderItemCommand(
    ProductId ProductId,
    Quantity Quantity,
    UnitPrice UnitPrice
);

public sealed record CreateOrderCommandResponse(string OrderId);
public sealed class CreateOrderCommandHandler : IPrimitiveResultCommandHandler<CreateOrderCommand, CreateOrderCommandResponse>
{
    private readonly IOrderService _orderService;

    public CreateOrderCommandHandler(IOrderService orderService)
    {
        this._orderService = orderService;
    }

    public async Task<PrimitiveResult<CreateOrderCommandResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        return await this._orderService
            .CreateOrder(
                CreateOrderCommand.Map(request),
                cancellationToken)
            .Map(orderId => PrimitiveResult.Success(new CreateOrderCommandResponse(orderId.Value)));
    }
}