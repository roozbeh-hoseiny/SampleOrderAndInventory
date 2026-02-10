using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Inventory.Persistence;
public interface IInventoryRepository : IGenericDomainRepository<InventoryItem, InventoryItemId>
{
}