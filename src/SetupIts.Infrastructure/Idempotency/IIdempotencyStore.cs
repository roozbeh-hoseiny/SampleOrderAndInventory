using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Idempotency;

public interface IIdempotencyStore
{
    Task<PrimitiveResult<IdempotencyModel>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PrimitiveResult<IdempotencyModel>> TryBeginAsync(Guid key, byte[] requestHash, TimeSpan timeout, CancellationToken cancellationToken);
    Task CompleteAsync(Guid key, int statusCode, string responseBody);
    Task FailedAsync(Guid key, string reason);
}
