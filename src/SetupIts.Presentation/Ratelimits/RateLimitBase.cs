using Microsoft.AspNetCore.RateLimiting;
using SetupIts.Application.ClientIpContext;
using System.Globalization;
using System.Threading.RateLimiting;

namespace SetupIts.Presentation.Ratelimits;

internal abstract class RateLimitBase<TPartitionKey> : IRateLimiterPolicy<TPartitionKey>
{
    private readonly IClientIpContext _clientIpContext;

    public virtual Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            return ValueTask.CompletedTask;
        };

    protected RateLimitBase(IClientIpContext clientIpContext)
    {
        this._clientIpContext = clientIpContext;
    }


    public abstract RateLimitPartition<TPartitionKey> GetPartition(HttpContext httpContext);

    protected string GeneratePartitionKey(HttpContext httpContext)
    {
        return (this._clientIpContext.ClientIp ?? "GUEST").Replace(".", string.Empty);
    }
}

internal sealed class CreateOrderRateLimitPolicy : RateLimitBase<string>
{
    public CreateOrderRateLimitPolicy(IClientIpContext clientIpContext) : base(clientIpContext)
    {
    }

    public override RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
                this.GeneratePartitionKey(httpContext),
                partitionKey => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 15,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
    }
}
