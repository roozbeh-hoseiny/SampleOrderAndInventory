namespace SetupIts.Presentation.Middlewares;

using Microsoft.AspNetCore.Http;
using SetupIts.Application.ClientIpContext;

public sealed class ClientIpMiddleware
{
    private readonly RequestDelegate _next;

    public ClientIpMiddleware(RequestDelegate next)
    {
        this._next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var ip =
                context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? context.Connection.RemoteIpAddress?.ToString();

            ClientIpContext.Set(ip);

            await this._next(context);
        }
        finally
        {
            ClientIpContext.Clear();
        }
    }
}
