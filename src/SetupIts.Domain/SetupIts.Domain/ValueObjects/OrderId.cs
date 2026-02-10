using SetupIts.Domain.Abstractios;

namespace SetupIts.Domain.ValueObjects;

public sealed record OrderId : UlidIdBase
{
    private OrderId(string val) : base(val) { }
    private OrderId() : base() { }

    public static OrderId Create() => new();
    public static OrderId Create(string val) => new(val);
}
