using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class QuantityTypeHandler : SqlMapper.TypeHandler<Quantity>
{
    public override Quantity Parse(object value)
    {
        if (int.TryParse(value?.ToString() ?? "0", out var v))
            return Quantity.CreateUnsafe(v);
        return Quantity.Zero;
    }
    public override void SetValue(IDbDataParameter parameter, Quantity value) => parameter.Value = value.Value;
}