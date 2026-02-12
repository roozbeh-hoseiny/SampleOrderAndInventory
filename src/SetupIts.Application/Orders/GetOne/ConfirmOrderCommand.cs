using SetupIts.Application.Abstractions;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.Orders.GetOne;
public sealed class GetOneQuery : IPrimitiveResultQuery<OrderReadModel>
{
    public string Id { get; set; } = string.Empty;
}
public sealed class GetOneCommandHandler : IPrimitiveResultQueryHandler<GetOneQuery, OrderReadModel>
{
    private readonly IOrderService _orderService;

    public GetOneCommandHandler(IOrderService orderService)
    {
        this._orderService = orderService;
    }

    public async Task<PrimitiveResult<OrderReadModel>> Handle(GetOneQuery request, CancellationToken cancellationToken)
    {
        var result = await this._orderService
            .GetOne(OrderId.Create(request.Id), cancellationToken);

        if (result.IsFailure) return PrimitiveResult.Failure<OrderReadModel>(result);

        return result;

    }
}