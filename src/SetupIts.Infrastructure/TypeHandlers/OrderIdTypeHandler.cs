using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;
public sealed class OrderIdTypeHandler : SqlMapper.TypeHandler<OrderId>
{
    public override OrderId? Parse(object value) => OrderId.Create(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, OrderId? value) => parameter.Value = value?.Value ?? string.Empty;
}
