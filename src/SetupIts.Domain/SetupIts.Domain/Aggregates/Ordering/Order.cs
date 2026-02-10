using SetupIts.Domain.Abstractios;
using SetupIts.Domain.Aggregates.Inventory.Events;
using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Ordering;
public sealed class Order : AggregateRoot<OrderId>
{
    #region " Fields "
    private List<OrderItem> _orderItems = new List<OrderItem>();
    #endregion

    #region " Properties "
    public int CustomerId { get; private set; }
    public OrderStatuses Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public TotalAmount TotalAmount => TotalAmount.CreateUnsafe(this.OrderItems.Sum(i => i.TotalPrice.Value));
    public IReadOnlyCollection<OrderItem> OrderItems => this._orderItems.AsReadOnly();
    #endregion

    #region " Constructors "
    private Order(OrderId id) : base(id) { }
    private Order() : base(OrderId.Create()) { }
    #endregion

    public static async Task<PrimitiveResult<Order>> Create(
        int customerId,
        OrderItemCreateData[] orderItems,
        IValidCustomerSpecification validCustomerSpecification,
        IValidProductSpecification validPeoductSpecification,
        CancellationToken cancellationToken)
    {
        var customerIsValid = await validCustomerSpecification
            .IsSatisfied(customerId, cancellationToken)
            .ConfigureAwait(false);

        if (customerIsValid.IsFailure)
            return PrimitiveResult.Failure<Order>(customerIsValid.Errors);

        var productIsValid = await validPeoductSpecification
            .IsSatisfied(orderItems.Select(o => o.ProductId), cancellationToken)
            .ConfigureAwait(false);

        if (productIsValid.IsFailure)
            return PrimitiveResult.Failure<Order>(productIsValid.Errors);

        var result = new Order()
        {
            CustomerId = customerId,
            Status = OrderStatuses.Draft,
            CreatedAt = DateTimeOffset.Now
        };

        if (orderItems.Length > 0)
        {
            foreach (var item in orderItems)
            {
                result.AddOrderItem(
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    false);
            }
        }

        result.AddDomainEvent(new OrderCreatedEvent(result.Id));

        return result;
    }

    public void AddOrderItems(IEnumerable<OrderItem> items)
    {
        this._orderItems.AddRange(items);
    }

    // for repository
    internal void AddOrderItem(OrderItem item)
    {
        this._orderItems.Add(item);
    }

    public void AddOrderItem(
       ProductId productId,
       Quantity qty,
       UnitPrice unitPrice) => this.AddOrderItem(productId, qty, unitPrice, true);

    public PrimitiveResult Confirm()
    {
        if (!this.Status.Equals(OrderStatuses.Draft))
            return PrimitiveResult.Failure("Error", "This order can not be confirm");

        if (this.Status.Equals(OrderStatuses.Confirmed)) return PrimitiveResult.Success();

        this.Status = OrderStatuses.Confirmed;

        this.AddDomainEvent(new OrderConfirmedEvent(this.Id));

        return PrimitiveResult.Success();
    }
    public PrimitiveResult Cancel()
    {
        if (this.Status.Equals(OrderStatuses.Cancelled)) return PrimitiveResult.Failure("Error", "Cancelled order can not be cancelled again!");

        this.Status = OrderStatuses.Cancelled;

        this.AddDomainEvent(new OrderCancelledEvent(this.Id));

        return PrimitiveResult.Success();
    }

    #region " Private Methods "
    void AddOrderItem(
      ProductId productId,
      Quantity qty,
      UnitPrice unitPrice,
      bool addEvent)
    {
        var newOrderItem = OrderItem.Create(
            this.Id,
            productId,
            qty,
            unitPrice);

        this.AddOrderItem(newOrderItem);
        if (addEvent)
            this.AddDomainEvent(new OrderItemAddedToOrderCreatedEvent(this.Id, newOrderItem.Id));
    }
    #endregion

}
