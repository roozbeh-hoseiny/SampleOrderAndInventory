using SetupIts.Domain.Abstractios;

namespace SetupIts.Domain.ValueObjects;
public sealed record ProductId : UlidIdBase
{
    private ProductId(string val) : base(val) { }
    private ProductId() : base() { }

    public static ProductId Create() => new ProductId();
    public static ProductId Create(string val) => new ProductId(val);
}