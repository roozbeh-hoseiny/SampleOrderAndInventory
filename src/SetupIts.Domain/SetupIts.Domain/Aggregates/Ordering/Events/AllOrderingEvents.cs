using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Inventory.Events;
public sealed record OrderCreatedEvent(OrderId OrderId) : DomainEventBase(DateTimeOffset.Now);
public sealed record OrderConfirmedEvent(OrderId OrderId) : DomainEventBase(DateTimeOffset.Now);
public sealed record OrderCancelledEvent(OrderId OrderId) : DomainEventBase(DateTimeOffset.Now);
public sealed record OrderItemAddedToOrderCreatedEvent(OrderId OrderId, OrderItemId OrderItemId) : DomainEventBase(DateTimeOffset.Now);
