namespace SetupIts.Domain.Abstractios;

public interface IEventEntity
{
    IReadOnlyCollection<IDomainEvent> Events { get; }

    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
