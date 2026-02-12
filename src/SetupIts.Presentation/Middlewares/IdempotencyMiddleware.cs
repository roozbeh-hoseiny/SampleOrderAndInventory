namespace SetupIts.Presentation.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Idempotency;
using System.Security.Cryptography;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptions<SetupItsGlobalOptions> _opts;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly TimeSpan _timeout;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<SetupItsGlobalOptions> opts,
        ILogger<IdempotencyMiddleware> logger)
    {
        this._next = next;
        this._serviceScopeFactory = serviceScopeFactory;
        this._opts = opts;
        this._logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var scope = this._serviceScopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        Guid key = Guid.Empty;
        if (!context.Request.Headers.TryGetValue(this._opts.Value.IdempotencyHeaderName, out var keyHeader)
            || !Guid.TryParse(keyHeader, out key)
            || key.Equals(Guid.Empty))
        {
            await this._next(context);
            return;
        }

        var requestHash = await ComputeRequestHashAsync(context);

        var record = await store.TryBeginAsync(key, requestHash, this._timeout, context.RequestAborted);
        if (record.IsFailure)
        {
            throw new Exception("Idempotency exception");
        }
        if (record.Value.Status == IdempotencyStatus.Completed)
        {
            context.Response.StatusCode = record.Value.ResponseCode!.Value;
            await context.Response.WriteAsync(record.Value.ResponseBody!);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        try
        {
            await this._next(context);
            var responseBodyContent = await ReadResponseBodyAsync(context.Response);
            await store.CompleteAsync(key, context.Response.StatusCode, responseBodyContent);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, ex.Message);
            await store.FailedAsync(key, ex.Message);
            throw;
        }
        finally
        {
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
    static async Task<byte[]> ComputeRequestHashAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        context.Request.Body.Position = 0;

        using var sha = SHA256.Create();
        return sha.ComputeHash(ms.ToArray());
    }
    static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return text;
    }
}
