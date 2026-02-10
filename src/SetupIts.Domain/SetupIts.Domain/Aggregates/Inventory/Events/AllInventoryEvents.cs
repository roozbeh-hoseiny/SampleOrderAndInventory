using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Inventory.Events;
public sealed record InventoryItemCreatedEvent(InventoryItemId Id) : DomainEventBase(DateTimeOffset.Now);
public sealed record InventoryItemReserveEvent(InventoryItemId Id, Quantity Quantity) : DomainEventBase(DateTimeOffset.Now);
public sealed record InventoryItemReleasedEvent(InventoryItemId Id, Quantity Quantity) : DomainEventBase(DateTimeOffset.Now);
public sealed record InventoryItemReceivedEvent(InventoryItemId Id, Quantity Quantity) : DomainEventBase(DateTimeOffset.Now);
public sealed record InventoryItemOnHandAdjustedEvent(InventoryItemId Id, Quantity Quantity) : DomainEventBase(DateTimeOffset.Now);
