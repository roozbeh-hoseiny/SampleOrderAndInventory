using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;

public sealed class TotalAmountTypeHandler : SqlMapper.TypeHandler<TotalAmount>
{
    public override TotalAmount Parse(object value)
    {
        if (decimal.TryParse(value?.ToString() ?? "0", out var v))
            return TotalAmount.CreateUnsafe(v);
        return TotalAmount.Zero;
    }
    public override void SetValue(IDbDataParameter parameter, TotalAmount value) => parameter.Value = value.Value;
}
