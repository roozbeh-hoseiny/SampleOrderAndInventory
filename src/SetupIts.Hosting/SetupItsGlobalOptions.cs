namespace SetupIts.Hosting;
public sealed class SetupItsGlobalOptions
{
    public string Connectionstring { get; set; } = string.Empty;
    public string IdempotencyHeaderName { get; set; } = "x-idempotency-id";
}
