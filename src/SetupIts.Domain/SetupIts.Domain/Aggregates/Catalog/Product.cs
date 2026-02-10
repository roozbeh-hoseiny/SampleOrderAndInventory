using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Catalog;
public sealed class Product : AggregateRoot<ProductId>
{
    #region " Properties "
    public ProductName ProductName { get; private set; }
    public Sku Sku { get; private set; }
    public bool IsActive { get; private set; }
    #endregion

    #region " Constructors "
    private Product(ProductId id) : base(id) { }
    private Product() : base(ProductId.Create()) { }
    #endregion

    #region " Factory "
    public static PrimitiveResult<Product> Create(
    ProductName name,
    Sku sku,
    bool isActive)
    {
        return new Product()
        {
            ProductName = name,
            Sku = sku,
            IsActive = isActive
        };
    }
    #endregion
}
