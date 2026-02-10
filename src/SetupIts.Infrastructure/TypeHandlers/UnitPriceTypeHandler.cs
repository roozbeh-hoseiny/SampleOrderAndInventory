using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class UnitPriceTypeHandler : SqlMapper.TypeHandler<UnitPrice>
{
    public override UnitPrice Parse(object value)
    {
        if (decimal.TryParse(value?.ToString() ?? "0", out var v))
            return UnitPrice.CreateUnsafe(v);
        return UnitPrice.Zero;
    }
    public override void SetValue(IDbDataParameter parameter, UnitPrice value) => parameter.Value = value.Value;
}