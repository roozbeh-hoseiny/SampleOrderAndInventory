using SetupIts.Domain.Abstractios;

namespace SetupIts.Domain.ValueObjects;

public sealed record InventoryItemId : UlidIdBase
{
    private InventoryItemId(string val) : base(val) { }
    private InventoryItemId() : base() { }

    public static InventoryItemId Create() => new();
    public static InventoryItemId Create(string val) => new(val);
}
