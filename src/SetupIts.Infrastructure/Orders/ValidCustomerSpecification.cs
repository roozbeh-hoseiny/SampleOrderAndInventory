using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Orders;
internal sealed class ValidCustomerSpecification : IValidCustomerSpecification
{
    public Task<PrimitiveResult<bool>> IsSatisfied(int customerId, CancellationToken cancellationToken)
    {
        // Checking the database or a third-party service to determine whether this customerId exists and is enabled.
        return Task.FromResult(PrimitiveResult.Success(true));
    }
}
