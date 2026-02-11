using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Inventory.Persistence;
public interface IInventoryRepository : IGenericDomainRepository<InventoryItem, InventoryItemId>
{
    Task<PrimitiveResult> UpdateReservedQty(InventoryItem entity, CancellationToken cancellationToken);
    Task<PrimitiveResult<InventoryItem[]>> GetByProductIds(ProductId[] ids, CancellationToken cancellationToken);
}