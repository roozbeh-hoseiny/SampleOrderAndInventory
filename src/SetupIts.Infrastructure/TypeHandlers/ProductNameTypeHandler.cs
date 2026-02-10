using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class ProductNameTypeHandler : SqlMapper.TypeHandler<ProductName>
{
    public override ProductName Parse(object value) => ProductName.CreateUnsafe(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, ProductName value) => parameter.Value = value.Value;
}
