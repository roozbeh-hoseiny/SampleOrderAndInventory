using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class ProductIdTypeHandler : SqlMapper.TypeHandler<ProductId>
{
    public override ProductId? Parse(object value) => ProductId.Create(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, ProductId? value) => parameter.Value = value?.Value ?? string.Empty;
}
