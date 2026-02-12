using Microsoft.Extensions.Options;
using SetupIts.Domain.Aggregates.Catalog;
using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.ValueObjects;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Abstractions;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Orders;

internal sealed class OrderReadRepository : DapperGenericRepository, IOrderReadRepository
{
    readonly static string GetOneQuery = $"""
        SELECT 
            {TableNames.Order_TableName}.{nameof(Order.Id)} as {nameof(OrderReadModel.Id)},
            {TableNames.Order_TableName}.{nameof(Order.CustomerId)} as {nameof(OrderReadModel.CustomerId)},
            {TableNames.Order_TableName}.{nameof(Order.Status)} as {nameof(OrderReadModel.Status)},
            CASE {TableNames.Order_TableName}.{nameof(Order.Status)}
                WHEN {(int)OrderStatuses.Draft} THEN 'Draft'
                WHEN {(int)OrderStatuses.Confirmed} THEN 'Confirmed'
                WHEN {(int)OrderStatuses.Cancelled} THEN 'Cancelled'
            END AS {nameof(OrderReadModel.StatusTitle)},
            {TableNames.Order_TableName}.{nameof(Order.CreatedAt)} as {nameof(OrderReadModel.CreatedAt)}
        FROM {TableNames.Order_TableName}
        WHERE {TableNames.Order_TableName}.{nameof(Order.Id)}  = @{nameof(Order.Id)} 
        """;
    readonly static string GetOrderItemsQuery = $"""
        SELECT 
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Id)} as {nameof(OrderItemReadModel.Id)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.ProductId)} as {nameof(OrderItemReadModel.ProductId)},
            {TableNames.Product_TableName}.{nameof(Product.ProductName)} as {nameof(OrderItemReadModel.ProductName)},
            {TableNames.Product_TableName}.{nameof(Product.Sku)} as {nameof(OrderItemReadModel.Sku)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.Qty)} as {nameof(OrderItemReadModel.Qty)},
            {TableNames.OrderItem_TableName}.{nameof(OrderItem.UnitPrice)} as {nameof(OrderItemReadModel.UnitPrice)}
        FROM {TableNames.OrderItem_TableName}
        INNER JOIN {TableNames.Product_TableName} ON {TableNames.Product_TableName}.{nameof(Product.Id)} = {TableNames.OrderItem_TableName}.{nameof(OrderItem.ProductId)}
        WHERE {TableNames.OrderItem_TableName}.{nameof(OrderItem.OrderId)}  = @{nameof(Order.Id)} 
        """;


    public OrderReadRepository(
        ICurrentTransactionScope currentTransactionScope,
        IOptionsMonitor<SetupItsGlobalOptions> opts) : base(currentTransactionScope, opts, true) { }

    public async Task<PrimitiveResult<OrderReadModel>> GetOne(OrderId id, CancellationToken cancellationToken)
    {
        var result = await this.RunDbCommand(async connection =>
        {
            OrderReadModel? result = null;

            var command = DapperCommandDefinitionBuilder
                    .Query($"{GetOneQuery}{Environment.NewLine}{GetOrderItemsQuery}")
                    .SetParameter(nameof(Order.Id), id.Value, System.Data.DbType.String);

            var dbResult = await this.QueryMultipleAsync(
                command,
                async (reader) => await reader.ReadFirstOrDefaultAsync<OrderReadModel>(),
                    async (reader) => (await reader.ReadAsync<OrderItemReadModel>())?.ToList(),
                    cancellationToken)
            .ConfigureAwait(false);

            if (dbResult.IsFailure || dbResult.Value.Item1 is null) return result;

            result = dbResult.Value.Item1 with
            {
                OrderItems = (dbResult.Value.Item2 ?? []).AsReadOnly()
            };

            return result;

        }, cancellationToken).ConfigureAwait(false);

        if (result is not null) return result;
        return PrimitiveResult.Failure<OrderReadModel>("NotFound.Error", $"An order wth Id:{id.Value} not found");
    }
}
