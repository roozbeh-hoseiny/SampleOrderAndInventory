using SetupIts.Application.Orders.CreateOrder;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Presentation.Endpoints.Order.Contracts;

public sealed class CreateOrderApiRequest
{
    public int CustomerId { get; set; }
    public OrderItemCommand[] Items { get; set; } = [];

    internal static CreateOrderCommand Map(CreateOrderApiRequest src)
    {
        return new CreateOrderCommand()
        {
            CustomerId = src.CustomerId,
            Items = src.Items.Select(item => new OrderItemCommand(
                item.ProductId,
                item.Quantity,
                item.UnitPrice)).ToArray()
        };
    }
}
public sealed record OrderItemApiRequest
{
    public ProductId ProductId { get; set; } = null!;
    public Quantity Quantity { get; set; }
    public UnitPrice UnitPrice { get; set; }
}
