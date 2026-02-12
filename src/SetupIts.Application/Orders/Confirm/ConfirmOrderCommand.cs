using SetupIts.Application.Abstractions;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.Orders.Confirm;
public sealed class ConfirmOrderCommand : IPrimitiveResultCommand<ConfirmOrderCommandResponse>
{
    public string Id { get; set; } = string.Empty;
    internal static ConfirmOrderRequest Map(ConfirmOrderCommand src)
    {
        return new ConfirmOrderRequest(OrderId.Create(src.Id));
    }
}
public sealed class ConfirmOrderCommandResponse;
public sealed class ConfirmOrderCommandHandler : IPrimitiveResultCommandHandler<ConfirmOrderCommand, ConfirmOrderCommandResponse>
{
    private readonly IOrderService _orderService;

    public ConfirmOrderCommandHandler(IOrderService orderService)
    {
        this._orderService = orderService;
    }

    public async Task<PrimitiveResult<ConfirmOrderCommandResponse>> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await this._orderService
            .ConfirmOrder(ConfirmOrderCommand.Map(request), cancellationToken);

        if (result.IsFailure) return PrimitiveResult.Failure<ConfirmOrderCommandResponse>(result);

        return new ConfirmOrderCommandResponse();

    }
}