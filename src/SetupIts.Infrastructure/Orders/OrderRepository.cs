using Dapper;
using Microsoft.Extensions.Options;
using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.ValueObjects;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Abstractions;
using SetupIts.Shared.Primitives;
using static Dapper.SqlMapper;

namespace SetupIts.Infrastructure.Orders;

public sealed class OrderRepository : DapperGenericRepository, IOrderRepository
{
    const string AddOrderCommand = $"""
        INSERT INTO {TableNames.Order_TableName}
        (
            {nameof(Order.Id)},    
            {nameof(Order.CustomerId)},
            [{nameof(Order.Status)}],
            {nameof(Order.CreatedAt)}
        )
        OUTPUT Inserted.RowVersion 
        VALUES
        (
            @{nameof(Order.Id)},
            @{nameof(Order.CustomerId)},
            @{nameof(Order.Status)},
            @{nameof(Order.CreatedAt)}
        );
        """;
    const string AddOrderItemCommand = $"""
        INSERT INTO {TableNames.OrderItem_TableName}
        (
            {nameof(OrderItem.Id)},
            {nameof(OrderItem.OrderId)},
            {nameof(OrderItem.ProductId)},
            {nameof(OrderItem.Qty)},
            {nameof(OrderItem.UnitPrice)}
        )
        VALUES #ORDER_ITEM_VALUES#
        """;
    const string GetOneQuery = $"""
        SELECT 
            {TableNames.Order_TableName}.{nameof(Order.Id)},
            {TableNames.Order_TableName}.{nameof(Order.CustomerId)},
            {TableNames.Order_TableName}.{nameof(Order.Status)},
            {TableNames.Order_TableName}.{nameof(Order.CreatedAt)},
            {TableNames.Order_TableName}.{nameof(Order.RowVersion)}
        FROM {TableNames.Order_TableName}
        WHERE {TableNames.Order_TableName}.{nameof(Order.Id)}  = @{nameof(Order.Id)} 
        """;
    readonly static string GetOrderItemsQuery = $"""
        SELECT 
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Id)} ,
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.ProductId)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Qty)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.UnitPrice)}
        FROM {TableNames.OrderItem_TableName}
        WHERE {TableNames.OrderItem_TableName}.{nameof(OrderItem.OrderId)}  = @{nameof(Order.Id)} 
        """;
    const string ChangeStatusCommand = $"""
        UPDATE {TableNames.Order_TableName}
        SET [Status] = @Status
        OUTPUT inserted.{nameof(Order.RowVersion)}
        WHERE 
            {nameof(Order.Id)} = @{nameof(Order.Id)}
            AND {nameof(Order.RowVersion)} = @{nameof(Order.RowVersion)}
        """;
    const string GetOneWithOrdersQuery = $"""
        SELECT 
            {TableNames.Order_TableName}.{nameof(Order.Id)},
            {TableNames.Order_TableName}.{nameof(Order.CustomerId)},
            {TableNames.Order_TableName}.{nameof(Order.Status)},
            {TableNames.Order_TableName}.{nameof(Order.CreatedAt)},
            
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Id)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.ProductId)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Qty)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.UnitPrice)}
        FROM {TableNames.Order_TableName}
        LEFT OUTER JOIN {TableNames.OrderItem_TableName} 
        ON {TableNames.Order_TableName}.{nameof(Order.Id)} = {TableNames.OrderItem_TableName}.{nameof(OrderItem.OrderId)}
        WHERE {TableNames.Order_TableName}.{nameof(Order.Id)}  = @{nameof(Order.Id)} 
        """;
    public string TableName => TableNames.Order_TableName;

    public OrderRepository(
        ICurrentTransactionScope currentTransactionScope,
        IOptionsMonitor<SetupItsGlobalOptions> opts) : base(currentTransactionScope, opts) { }

    public async Task<PrimitiveResult<byte[]>> Add(Order entity, CancellationToken cancellationToken)
    {
        var currentTransaction = await this._currentTransactionScope.GetCurrentTransaction(cancellationToken);

        var addOrderCommand = DapperCommandDefinitionBuilder
            .Query($"{AddOrderCommand}")
            .SetParameter(nameof(Order.Id), entity.Id.Value)
            .SetParameter(nameof(Order.CustomerId), entity.CustomerId)
            .SetParameter(nameof(Order.Status), entity.Status)
            .SetParameter(nameof(Order.CreatedAt), entity.CreatedAt);

        addOrderCommand.SetTransaction(currentTransaction);

        var result = await currentTransaction.Connection
            .ExecuteScalarAsync<byte[]>(addOrderCommand.Build(cancellationToken))
            .ConfigureAwait(false);

        var addOrderItemsResult = await currentTransaction.Connection.ExecuteAsync(
            CreateOrderInsertCommand(entity)
                    .SetTransaction(currentTransaction)
                    .Build(cancellationToken))
            .ConfigureAwait(false);

        await this.AddEntityEvents<Order, OrderId>(entity, currentTransaction).ConfigureAwait(false);

        if (result is null) PrimitiveResult.Failure<byte[]>("Error", "result is null");

        entity.SetRowVersion(result!);

        return result!;

    }
    public async Task<PrimitiveResult<byte[]>> UpdateStatus(Order entity, CancellationToken cancellationToken)
    {
        var result = await this.SaveAsync<Order, OrderId, byte[]>(
           entity,
           DapperCommandDefinitionBuilder
            .Query(ChangeStatusCommand)
            .SetParameter(nameof(Order.Id), entity.Id.Value)
            .SetParameter(nameof(Order.RowVersion), entity.RowVersion)
            .SetParameter(nameof(Order.Status), entity.Status),
           cancellationToken)
           .ConfigureAwait(false);

        if (result.IsSuccess)
            entity.SetRowVersion(result.Value);

        return result;
    }
    public Task<PrimitiveResult<byte[]>> Update(Order entity, CancellationToken cancellationToken) => throw new NotImplementedException();
    public async Task<PrimitiveResult<Order>> GetOne(OrderId id, CancellationToken cancellationToken)
    {
        var result = await this.RunDbCommand(async connection =>
        {
            Order? result = null;

            var command = DapperCommandDefinitionBuilder
                    .Query($"{GetOneQuery}{Environment.NewLine}{GetOrderItemsQuery}")
                    .SetParameter(nameof(Order.Id), id.Value, System.Data.DbType.String);

            var dbResult = await this.QueryMultipleAsync(
                command,
                async (reader) => await reader.ReadFirstOrDefaultAsync<Order>(),
                    async (reader) => (await reader.ReadAsync<OrderItem>())?.ToList(),
                    cancellationToken)
            .ConfigureAwait(false);

            if (dbResult.IsFailure || dbResult.Value.Item1 is null) return result;

            result = dbResult.Value.Item1;
            result.AddOrderItems(dbResult.Value.Item2 ?? []);

            return result;

        }, cancellationToken).ConfigureAwait(false);

        if (result is not null) return result;

        return PrimitiveResult.Failure<Order>("NotFound.Error", $"An order wth Id:{id.Value} not found");
    }
    public async Task<PrimitiveResult<Order>> GetOneWithItems(OrderId id, CancellationToken cancellationToken)
    {
        var result = await this.RunDbCommand(async connection =>
        {
            var orderLookup = new Dictionary<OrderId, Order>();

            await connection.QueryAsync<Order, OrderItem, Order>(
                DapperCommandDefinitionBuilder
                    .Query(GetOneWithOrdersQuery)
                    .SetParameter($"{nameof(Order.Id)}", id.Value, System.Data.DbType.String)
                    .Build(cancellationToken),
                (order, item) =>
                {
                    if (!orderLookup.TryGetValue(order.Id, out var existing))
                    {
                        existing = order;
                        orderLookup.Add(existing.Id, existing);
                    }

                    if (item is not null)
                    {
                        existing.AddOrderItem(item);
                    }

                    return existing;
                },
                splitOn: nameof(OrderItem.Id));

            return orderLookup.Values.FirstOrDefault();
        }, cancellationToken).ConfigureAwait(false);

        if (result is not null) return result;
        return PrimitiveResult.Failure<Order>("NotFound.Error", $"An order wth Id:{id.Value} not found");

    }

    static DapperCommandDefinitionBuilder CreateOrderInsertCommand(Order entity)
    {
        var addOrderItemValuesCommand = string.Empty;

        var currentOrderItems = entity.OrderItems.ToArray();

        if (currentOrderItems.Length > 0)
        {
            var itmesCommandList = new List<string>(entity.OrderItems.Count);
            for (int i = 0; i < currentOrderItems.Length; i++)
            {
                itmesCommandList.Add($"(@{nameof(OrderItem.Id)}_{i},@{nameof(OrderItem.OrderId)}_{i},@{nameof(OrderItem.ProductId)}_{i},@{nameof(OrderItem.Qty)}_{i},@{nameof(OrderItem.UnitPrice)}_{i})");
            }
            addOrderItemValuesCommand = string.Join(',', itmesCommandList);
        }

        var result = DapperCommandDefinitionBuilder
            .Query($"{AddOrderItemCommand.Replace("#ORDER_ITEM_VALUES#", addOrderItemValuesCommand)}")
            .SetParameter(nameof(Order.Id), entity.Id.Value)
            .SetParameter(nameof(Order.CustomerId), entity.CustomerId)
            .SetParameter(nameof(Order.Status), entity.Status)
            .SetParameter(nameof(Order.CreatedAt), entity.CreatedAt);

        if (entity.OrderItems.Any())
        {
            for (int i = 0; i < entity.OrderItems.Count; i++)
            {
                result = result
                    .SetParameter($"{nameof(OrderItem.Id)}_{i}", currentOrderItems[i].Id.Value)
                    .SetParameter($"{nameof(OrderItem.OrderId)}_{i}", entity.Id.Value)
                    .SetParameter($"{nameof(OrderItem.ProductId)}_{i}", currentOrderItems[i].ProductId.Value)
                    .SetParameter($"{nameof(OrderItem.Qty)}_{i}", currentOrderItems[i].Qty.Value)
                    .SetParameter($"{nameof(OrderItem.UnitPrice)}_{i}", currentOrderItems[i].UnitPrice.Value);
            }
        }
        return result;
    }
}
