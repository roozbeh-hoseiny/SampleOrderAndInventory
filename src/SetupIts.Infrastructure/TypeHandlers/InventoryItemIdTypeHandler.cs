using Dapper;
using SetupIts.Domain.ValueObjects;
using System.Data;

namespace SetupIts.Infrastructure.TypeHandlers;
public sealed class InventoryItemIdTypeHandler : SqlMapper.TypeHandler<InventoryItemId>
{
    public override InventoryItemId? Parse(object value) => InventoryItemId.Create(value?.ToString() ?? string.Empty);
    public override void SetValue(IDbDataParameter parameter, InventoryItemId? value) => parameter.Value = value?.Value ?? string.Empty;
}