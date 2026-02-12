using Dapper;
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
    #region " Queries "
    const string AddInventoryItemCommand = $"""
        INSERT INTO {TableNames.InventoryItem_TableName}(
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
        Update {TableNames.InventoryItem_TableName}
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
    const string UpdateReservedQtyCommand = $"""
        Update {TableNames.InventoryItem_TableName}
        SET
            {nameof(InventoryItem.ReservedQty)} = @{nameof(InventoryItem.ReservedQty)}
        WHERE 
            {nameof(InventoryItem.Id)} = @{nameof(InventoryItem.Id)}
            AND {nameof(InventoryItem.RowVersion)} = @{nameof(InventoryItem.RowVersion)};

        SELECT {nameof(InventoryItem.RowVersion)} FROM InventoryItem WHERE {nameof(InventoryItem.Id)} = @{nameof(InventoryItem.Id)};
        """;
    const string GetByProductIdsQuery = $"""
        SELECT {TableNames.InventoryItem_TableName}.* 
        FROM {TableNames.InventoryItem_TableName}
        WHERE {nameof(InventoryItem.ProductId)} IN 
        (
        	SELECT [Value] FROM OPENJSON(@Ids)
        )
        """;
    #endregion

    #region " Properties "
    public string TableName => TableNames.InventoryItem_TableName;
    #endregion

    #region " Constructor "
    public InventoryRepository(
        ICurrentTransactionScope currentTransactionScope,
        IOptionsMonitor<SetupItsGlobalOptions> opts) : base(currentTransactionScope, opts) { }

    #endregion

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
    public async Task<PrimitiveResult> UpdateReservedQty(InventoryItem entity, CancellationToken cancellationToken)
    {

        var result = await this.SaveAsync<InventoryItem, InventoryItemId, byte[]>(
            entity,
             DapperCommandDefinitionBuilder
            .Query(UpdateReservedQtyCommand)
            .SetParameter($"@{nameof(InventoryItem.Id)}", entity.Id.Value)
            .SetParameter($"@{nameof(InventoryItem.ReservedQty)}", entity.ReservedQty.Value)
            .SetParameter($"@{nameof(InventoryItem.RowVersion)}", entity.RowVersion),
            cancellationToken)
            .ConfigureAwait(false);


        return result.IsSuccess ? PrimitiveResult.Success() : PrimitiveResult.Failure(result.Errors);
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
    static DapperCommandDefinitionBuilder CreateAddAndUpdateInventoryItemCommand(string command, InventoryItem entity) =>
        DapperCommandDefinitionBuilder
            .Query(command)
            .SetParameter($"@{nameof(InventoryItem.Id)}", entity.Id.Value)
            .SetParameter($"@{nameof(InventoryItem.ProductId)}", entity.ProductId.Value)
            .SetParameter($"@{nameof(InventoryItem.WarehouseId)}", entity.WarehouseId)
            .SetParameter($"@{nameof(InventoryItem.OnHandQty)}", entity.OnHandQty.Value)
            .SetParameter($"@{nameof(InventoryItem.ReservedQty)}", entity.ReservedQty.Value)
            .SetParameter($"@{nameof(InventoryItem.RowVersion)}", entity.RowVersion);
    public async Task<PrimitiveResult<InventoryItem[]>> GetByProductIds(ProductId[] ids, CancellationToken cancellationToken)
    {
        var command = DapperCommandDefinitionBuilder
            .Query(GetByProductIdsQuery)
            .SetParameter("ids", System.Text.Json.JsonSerializer.Serialize(ids.Select(i => i.Value)), System.Data.DbType.String)
            .Build(cancellationToken);
        var result = await this.RunDbCommand(
            async connection =>
            {
                var dbResult = await connection.QueryAsync<InventoryItem>(command).ConfigureAwait(false);
                return dbResult?.ToArray() ?? Array.Empty<InventoryItem>();
            },
            cancellationToken)
            .ConfigureAwait(false);

        return PrimitiveResult.Success(result ?? []);
    }

}
