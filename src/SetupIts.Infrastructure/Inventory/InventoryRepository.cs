using Microsoft.Extensions.Options;
using SetupIts.Domain.Aggregates.Inventory;
using SetupIts.Domain.Aggregates.Inventory.Persistence;
using SetupIts.Domain.ValueObjects;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Abstractions;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Inventory;
public sealed class InventoryRepository : DapperGenericRepository, IInventoryRepository
{
    const string TABLE_NAME = "InventoryItem";
    const string AddInventoryItemCommand = $"""
        INSERT INTO {TABLE_NAME}(
            {nameof(InventoryItem.Id)},
            {nameof(InventoryItem.ProductId)},
            {nameof(InventoryItem.WarehouseId)},
            {nameof(InventoryItem.OnHandQty)},
            {nameof(InventoryItem.ReservedQty)})
        OUTPUT Inserted.RowVersion 
        VALUES
        (
            @{nameof(InventoryItem.Id)},
            @{nameof(InventoryItem.ProductId)},
            @{nameof(InventoryItem.WarehouseId)},
            @{nameof(InventoryItem.OnHandQty)},
            @{nameof(InventoryItem.ReservedQty)}
        );
        """;
    const string UpdateInventoryItemCommand = $"""
        Update {TABLE_NAME}
        SET
            {nameof(InventoryItem.ProductId)} = @{nameof(InventoryItem.ProductId)},
            {nameof(InventoryItem.WarehouseId)} = @{nameof(InventoryItem.WarehouseId)},
            {nameof(InventoryItem.OnHandQty)} = @{nameof(InventoryItem.OnHandQty)},
            {nameof(InventoryItem.ReservedQty)} = @{nameof(InventoryItem.ReservedQty)}
        WHERE 
            {nameof(InventoryItem.Id)} = @{nameof(InventoryItem.Id)}
            AND {nameof(InventoryItem.RowVersion)} = @{nameof(InventoryItem.RowVersion)};

        SELECT {nameof(InventoryItem.RowVersion)} FROM InventoryItem WHERE {nameof(InventoryItem.Id)} = @{nameof(InventoryItem.Id)};
        """;
    public string TableName => TABLE_NAME;

    public InventoryRepository(IOptionsMonitor<SetupItsGlobalOptions> opts) : base(opts)
    {

    }

    public async Task<PrimitiveResult<byte[]>> Add(
        InventoryItem entity,
        CancellationToken cancellationToken)
    {
        var result = await this.SaveAsync<InventoryItem, InventoryItemId, byte[]>(
            entity,
            CreateAddAndUpdateInventoryItemCommand(AddInventoryItemCommand, entity),
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
            entity.SetRowVersion(result.Value);

        return result;
    }

    public async Task<PrimitiveResult<byte[]>> Update(InventoryItem entity, CancellationToken cancellationToken)
    {
        var result = await this.SaveAsync<InventoryItem, InventoryItemId, byte[]>(
            entity,
            CreateAddAndUpdateInventoryItemCommand(UpdateInventoryItemCommand, entity),
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
            entity.SetRowVersion(result.Value);

        return result;
    }

    public Task<PrimitiveResult<InventoryItem>> GetOne(InventoryItemId id, CancellationToken cancellationToken)
    {
        return this.QueryFirstOrDefaultAsync<InventoryItem, InventoryItemId>(
            id,
      [
                nameof(InventoryItem.Id),
                nameof(InventoryItem.ProductId),
                nameof(InventoryItem.WarehouseId),
                nameof(InventoryItem.OnHandQty),
                nameof(InventoryItem.ReservedQty),
                nameof(InventoryItem.RowVersion)
            ],
            this.TableName,
            cancellationToken);
    }
    static Func<DapperCommandDefinitionBuilder> CreateAddAndUpdateInventoryItemCommand(string command, InventoryItem entity) => () =>
        DapperCommandDefinitionBuilder
            .Query(command)
            .SetParameter($"@{nameof(InventoryItem.Id)}", entity.Id.Value)
            .SetParameter($"@{nameof(InventoryItem.ProductId)}", entity.ProductId.Value)
            .SetParameter($"@{nameof(InventoryItem.WarehouseId)}", entity.WarehouseId)
            .SetParameter($"@{nameof(InventoryItem.OnHandQty)}", entity.OnHandQty.Value)
            .SetParameter($"@{nameof(InventoryItem.ReservedQty)}", entity.ReservedQty.Value)
            .SetParameter($"@{nameof(InventoryItem.RowVersion)}", entity.RowVersion);
}
