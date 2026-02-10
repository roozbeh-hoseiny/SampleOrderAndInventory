using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class OrderItemIdTypeHandler : SqlMapper.TypeHandler<OrderItemId>
{
    public override OrderItemId? Parse(object value) => OrderItemId.Create(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, OrderItemId? value) => parameter.Value = value?.Value ?? string.Empty;
}
