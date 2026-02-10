using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Ordering;

public sealed class OrderItem : EntityBase<OrderItemId>
{
    public OrderId OrderId { get; private set; } = null!;
    public ProductId ProductId { get; private set; } = null!;
    public Quantity Qty { get; private set; }
    public UnitPrice UnitPrice { get; private set; }
    public UnitPrice TotalPrice => this.UnitPrice.TryScale(this.Qty.Value);

    #region " Constructors "
    private OrderItem(OrderItemId id) : base(id) { }
    private OrderItem() : base(OrderItemId.Create()) { }

    #endregion

    internal static OrderItem Create(
        OrderId orderId,
        ProductId productId,
        Quantity qty,
        UnitPrice unitPrice)
    {
        return new OrderItem()
        {
            OrderId = orderId,
            ProductId = productId,
            Qty = qty,
            UnitPrice = unitPrice
        };
    }
}