namespace SetupIts.Domain.Abstractios;
public abstract class EntityBase<TId> :
    IEventEntity
    where TId : IEquatable<TId>
{
    private List<IDomainEvent> _domainEvents = new();

    public TId Id { get; private set; }
    public IReadOnlyCollection<IDomainEvent> Events => this._domainEvents.AsReadOnly();
    internal byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected EntityBase(TId id) => this.Id = id;

    public void AddDomainEvent(IDomainEvent domainEvent) => this._domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => this._domainEvents.Clear();

    public static IEqualityComparer<TEntity> GetIdEqualityComparer<TEntity>()
        where TEntity : EntityBase<TId>
        => new IdEqualityComparer<TEntity>();

    private class IdEqualityComparer<TEntity> : IEqualityComparer<TEntity>
        where TEntity : EntityBase<TId>
    {
        public bool Equals(TEntity? x, TEntity? y)
        {
            if (x is null || y is null)
                return false;
            if (ReferenceEquals(x, y))
                return true;
            return x.GetType() == y.GetType() && x.Id.Equals(y.Id);
        }

        public int GetHashCode(TEntity obj)
        {
            return obj.Id.GetHashCode();
        }
    }

    internal void SetRowVersion(byte[] rowVersion)
    {
        this.RowVersion = rowVersion ?? throw new ArgumentNullException(nameof(rowVersion));
    }
}
