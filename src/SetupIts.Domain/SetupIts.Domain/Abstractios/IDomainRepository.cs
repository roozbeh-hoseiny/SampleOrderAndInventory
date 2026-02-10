using SetupIts.Shared.Primitives;

namespace SetupIts.Domain.Abstractios;

public interface IDomainRepository;
public interface IGenericDomainRepository<TAggregate, TId> : IDomainRepository where TAggregate : IAggregateRoot
{
    string TableName { get; }

    Task<PrimitiveResult<byte[]>> Add(TAggregate entity, CancellationToken cancellationToken);
    Task<PrimitiveResult<byte[]>> Update(TAggregate entity, CancellationToken cancellationToken);
    Task<PrimitiveResult<TAggregate>> GetOne(TId id, CancellationToken cancellationToken);
}
