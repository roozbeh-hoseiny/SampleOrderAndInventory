namespace SetupIts.Domain.Abstractios;
public interface IDomainEvent
{
    bool IsIntegrationEvent { get; }
    DateTimeOffset OccuredOn { get; }
}

