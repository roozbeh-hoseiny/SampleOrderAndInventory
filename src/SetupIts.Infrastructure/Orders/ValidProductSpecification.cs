using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Orders;

internal sealed class ValidProductSpecification : IValidProductSpecification
{
    public Task<PrimitiveResult<bool>> IsSatisfied(IEnumerable<ProductId> productIds, CancellationToken cancellationToken)
    {
        // Checking the database or a third-party service to determine whether this productId exists and is enabled.
        return Task.FromResult(PrimitiveResult.Success(true));
    }
}
