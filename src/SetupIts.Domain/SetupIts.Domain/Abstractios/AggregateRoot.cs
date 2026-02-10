namespace SetupIts.Domain.Abstractios;

public abstract class AggregateRoot<TId> :
    EntityBase<TId>,
    IAggregateRoot
    where TId : IEquatable<TId>
{
    protected AggregateRoot(TId id) : base(id) { }
}
