using SetupIts.Domain.Abstractios;
using SetupIts.Domain.ValueObjects;

namespace SetupIts.Domain.Aggregates.Catalog.Persistence;
public interface ICatalogRepository : IGenericDomainRepository<Product, ProductId>
{
}