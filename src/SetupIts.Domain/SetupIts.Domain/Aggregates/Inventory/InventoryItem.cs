using SetupIts.Domain.Abstractios;
using SetupIts.Domain.Aggregates.Inventory.Events;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Inventory;

public sealed class InventoryItem : AggregateRoot<InventoryItemId>
{
    #region " Properties "
    public ProductId ProductId { get; private set; } = null!;
    public int WarehouseId { get; private set; }
    public Quantity OnHandQty { get; private set; }
    public Quantity ReservedQty { get; private set; }
    public Quantity AvailableQty => this.OnHandQty.TryDecrease(this.ReservedQty);
    #endregion

    #region " Constructors "
    private InventoryItem(InventoryItemId id) : base(id) { }
    private InventoryItem() : base(InventoryItemId.Create()) { }
    #endregion

    #region " Factory "
    public static PrimitiveResult<InventoryItem> Create(
       ProductId productId,
       int warehouseId,
       Quantity onHandQty)
    {
        var result = new InventoryItem()
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            OnHandQty = onHandQty,
            ReservedQty = Quantity.Zero
        };

        result.AddDomainEvent(new InventoryItemCreatedEvent(result.Id));


        return result;
    }
    #endregion

    #region " Methods "
    public PrimitiveResult Reserve(Quantity qty)
    {
        if (qty == Quantity.Zero)
            return PrimitiveResult.Success();

        if (qty > this.AvailableQty)
            return PrimitiveResult.Failure("Error", "Not enough available inventory");

        return this.ReservedQty.Increase(qty)
            .Map(newQuantity =>
            {
                this.ReservedQty = newQuantity;
                this.AddDomainEvent(new InventoryItemReserveEvent(this.Id, qty));
                return PrimitiveResult.Success();
            });
    }
    public PrimitiveResult Release(Quantity qty)
    {
        if (qty == Quantity.Zero)
            return PrimitiveResult.Success();

        if (qty > this.ReservedQty)
            return PrimitiveResult.Failure("Error", "Not enough available inventory");

        return this.ReservedQty.Decrease(qty)
            .Map(newQuantity =>
            {
                this.ReservedQty = newQuantity;
                this.AddDomainEvent(new InventoryItemReleasedEvent(this.Id, qty));
                return PrimitiveResult.Success();
            });
    }
    public PrimitiveResult Receive(Quantity qty)
    {
        if (qty == Quantity.Zero)
            return PrimitiveResult.Success();

        return this.OnHandQty.Increase(qty)
            .Map(newQuantity =>
            {
                this.OnHandQty = newQuantity;
                this.AddDomainEvent(new InventoryItemReceivedEvent(this.Id, qty));
                return PrimitiveResult.Success();
            });
    }
    public PrimitiveResult AdjustOnHand(Quantity newOnHand)
    {
        if (newOnHand < this.ReservedQty)
            return PrimitiveResult.Failure(
                "Inventory.AdjustBelowReserved",
                "On-hand quantity cannot be less than reserved quantity");

        this.OnHandQty = newOnHand;

        this.AddDomainEvent(new InventoryItemOnHandAdjustedEvent(this.Id, newOnHand));

        return PrimitiveResult.Success();
    }
    #endregion
}