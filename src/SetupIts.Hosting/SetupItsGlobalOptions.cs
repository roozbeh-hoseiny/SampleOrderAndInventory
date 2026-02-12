namespace SetupIts.Hosting;
public sealed class SetupItsGlobalOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string IdempotencyHeaderName { get; set; } = "x-idempotency-id";
    public TimeSpan IdempotencyTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
