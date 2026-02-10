using SetupIts.Domain.Abstractios;

namespace SetupIts.Infrastructure.Idempotency;
public enum IdempotencyStatus : byte
{
    InProgress = 0,
    Completed = 1,
    Failed = 2
}
public sealed class IdempotencyModel : EntityBase<Guid>
{
    public IdempotencyModel() : base(Guid.NewGuid()) { }
    public IdempotencyModel(Guid id) : base(id) { }

    public byte[] RequestHash { get; set; } = default!;
    public IdempotencyStatus Status { get; set; }
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }

    public static IdempotencyModel Create(
        Guid id,
        byte[] requestHash,
        DateTimeOffset expiredAt)
    {
        var now = DateTimeOffset.Now;

        return new IdempotencyModel(id)
        {
            RequestHash = requestHash,
            Status = IdempotencyStatus.InProgress,
            CreatedAt = now,
            UpdatedAt = now,
            ExpireAt = expiredAt
        };
    }
}