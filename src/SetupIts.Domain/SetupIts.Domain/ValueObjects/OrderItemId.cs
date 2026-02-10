using SetupIts.Domain.Abstractios;

namespace SetupIts.Domain.ValueObjects;

public sealed record OrderItemId : UlidIdBase
{
    private OrderItemId(string val) : base(val) { }
    private OrderItemId() : base() { }

    public static OrderItemId Create() => new();
    public static OrderItemId Create(string val) => new(val);
}
