using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Aggregates.Ordering.Specifications;
public interface IValidCustomerSpecification
{
    Task<PrimitiveResult<bool>> IsSatisfied(int customerId, CancellationToken cancellationToken);
}
public interface IValidProductSpecification
{
    Task<PrimitiveResult<bool>> IsSatisfied(IEnumerable<ProductId> productIds, CancellationToken cancellationToken);
}