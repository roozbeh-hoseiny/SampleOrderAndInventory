namespace SetupIts.Domain.Abstractios;

public abstract record DomainEventBase(DateTimeOffset OccuredOn) : IDomainEvent
{
    public bool IsIntegrationEvent => false;
}

