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
    const string TABLE_NAME = "[Order]";
    const string ORDERITEM_TABLE_NAME = "OrderItem";
    const string AddOrderCommand = $"""
        INSERT INTO {TABLE_NAME}
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
        INSERT INTO {ORDERITEM_TABLE_NAME}
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
            {TABLE_NAME}.{nameof(Order.Id)},
            {TABLE_NAME}.{nameof(Order.CustomerId)},
            {TABLE_NAME}.{nameof(Order.Status)},
            {TABLE_NAME}.{nameof(Order.CreatedAt)},
            {TABLE_NAME}.{nameof(Order.RowVersion)}
        FROM {TABLE_NAME}
        WHERE {TABLE_NAME}.{nameof(Order.Id)}  = @{nameof(Order.Id)} 
        """;
    const string ChangeStatusCommand = $"""
        UPDATE {TABLE_NAME}
        SET [Status] = @Status
        OUTPUT inserted.{nameof(Order.RowVersion)}
        WHERE 
            {nameof(Order.Id)} = @{nameof(Order.Id)}
            AND {nameof(Order.RowVersion)} = @{nameof(Order.RowVersion)}
        
        """;
    const string GetOneWithOrdersQuery = $"""
        SELECT 
            {TABLE_NAME}.{nameof(Order.Id)},
            {TABLE_NAME}.{nameof(Order.CustomerId)},
            {TABLE_NAME}.{nameof(Order.Status)},
            {TABLE_NAME}.{nameof(Order.CreatedAt)},
            
            {ORDERITEM_TABLE_NAME}.{nameof(OrderItem.Id)},
            {ORDERITEM_TABLE_NAME}.{nameof(OrderItem.ProductId)},
            {ORDERITEM_TABLE_NAME}.{nameof(OrderItem.Qty)},
            {ORDERITEM_TABLE_NAME}.{nameof(OrderItem.UnitPrice)}
        FROM {TABLE_NAME}
        LEFT OUTER JOIN {ORDERITEM_TABLE_NAME} 
        ON {TABLE_NAME}.{nameof(Order.Id)} = {ORDERITEM_TABLE_NAME}.{nameof(OrderItem.OrderId)}
        WHERE {TABLE_NAME}.{nameof(Order.Id)}  = @{nameof(Order.Id)} 
        """;
    public string TableName => TABLE_NAME;

    public OrderRepository(IOptionsMonitor<SetupItsGlobalOptions> opts) : base(opts)
    {

    }


    public async Task<PrimitiveResult<byte[]>> Add(Order entity, CancellationToken cancellationToken)
    {
        var result = await this.SaveAsync<Order, OrderId, byte[]>(
            entity,
            () => CreateOrderInsertCommand(entity),
            cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
            entity.SetRowVersion(result.Value);

        return result;

    }
    public async Task<PrimitiveResult<byte[]>> UpdateStatus(Order entity, CancellationToken cancellationToken)
    {
        var result = await this.SaveAsync<Order, OrderId, byte[]>(
           entity,
           () => DapperCommandDefinitionBuilder
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
        var result = await this.RunDbCommand<Order>(connection =>
            connection.QueryFirstOrDefaultAsync<Order>(
                DapperCommandDefinitionBuilder
                    .Query(GetOneQuery)
                    .SetParameter($"{nameof(Order.Id)}", id.Value, System.Data.DbType.String)
                    .Build(cancellationToken)
                )).ConfigureAwait(false);

        if (result is not null) return result;
        return PrimitiveResult.Failure<Order>("NotFound.Error", $"An order wth Id:{id.Value} not found");

    }
    public async Task<PrimitiveResult<Order>> GetOne__(OrderId id, CancellationToken cancellationToken)
    {
        var result = await this.RunDbCommand<Order>(async connection =>
        {
            var orderDict = new Dictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);

            var command = DapperCommandDefinitionBuilder
                .Query(GetOneWithOrdersQuery)
                .SetParameter($"{nameof(Order.Id)}", id.Value, System.Data.DbType.String)
                .Build(cancellationToken);

            var result = (await connection.QueryAsync<Order, OrderItem, Order>(
                command,
                (order, orderItem) =>
                {
                    if (!orderDict.TryGetValue(order.Id.Value, out var currentOrder))
                    {
                        currentOrder = order;
                        orderDict.Add(order.Id.Value, currentOrder);
                    }
                    if (orderItem is not null)
                    {
                        currentOrder.AddOrderItem(orderItem);
                    }
                    return currentOrder;
                },
                "OrderItemId")
                .ConfigureAwait(false)).ToList();
            return result?.FirstOrDefault();
        });

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
            .Query($"{AddOrderCommand}{Environment.NewLine}{AddOrderItemCommand.Replace("#ORDER_ITEM_VALUES#", addOrderItemValuesCommand)}")
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

internal sealed class OrderReadRepository : IOrderReadRepository
{
    public Task<PrimitiveResult<IReadOnlyCollection<OrderReadModel>>> GetOne(OrderId id, CancellationToken cancellationToken) => throw new NotImplementedException();
}