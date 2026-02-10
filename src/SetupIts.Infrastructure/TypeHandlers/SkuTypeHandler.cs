using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class SkuTypeHandler : SqlMapper.TypeHandler<Sku>
{
    public override Sku Parse(object value) => Sku.CreateUnsafe(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, Sku value) => parameter.Value = value.Value;
}
