using SetupIts.Application.Abstractions;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.Orders.Cancel;
public sealed class CancelOrderCommand : IPrimitiveResultCommand<CancelOrderCommandResponse>
{
    public string Id { get; set; } = string.Empty;
    internal static CancelOrderRequest Map(CancelOrderCommand src)
    {
        return new CancelOrderRequest(OrderId.Create(src.Id));
    }
}
public sealed class CancelOrderCommandResponse;
public sealed class CancelOrderCommandHandler : IPrimitiveResultCommandHandler<CancelOrderCommand, CancelOrderCommandResponse>
{
    private readonly IOrderService _orderService;

    public CancelOrderCommandHandler(IOrderService orderService)
    {
        this._orderService = orderService;
    }

    public async Task<PrimitiveResult<CancelOrderCommandResponse>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await this._orderService
            .CancelOrder(CancelOrderCommand.Map(request), cancellationToken);

        if (result.IsFailure) return PrimitiveResult.Failure<CancelOrderCommandResponse>(result);

        return new CancelOrderCommandResponse();

    }
}