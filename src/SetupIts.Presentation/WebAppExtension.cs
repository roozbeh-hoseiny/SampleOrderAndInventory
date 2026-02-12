using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Antiforgery;
using SetupIts.Presentation.Endpoints;
using SetupIts.Presentation.Middlewares;

namespace SetupIts.Presentation;

public static class WebAppExtension
{
    public static void MapAllMiddlewares(this WebApplication app)
    {
        var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Serve AntiForgery ONLY for HTML files
                var fileName = ctx.File.Name;

                if (
                fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = antiforgery.GetAndStoreTokens(ctx.Context);

                    ctx.Context.Response.Cookies.Append(
                        "XSRF-TOKEN",
                        tokens.RequestToken!,
                        new CookieOptions
                        {
                            HttpOnly = false,               // Angular must read it
                            Secure = true,                 // Use HTTPS
                            SameSite = SameSiteMode.Strict,
                            Path = "/"
                        }
                    );
                }
            }
        });

        app.AddDevelopmentMiddlewares();
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseCors(policy =>
        {
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowAnyOrigin();
            policy.SetPreflightMaxAge(TimeSpan.FromMinutes(12));
            policy.WithExposedHeaders("Content-Disposition");
        });
        app.UseResponseCaching();
        app.AddApiVersioning();
        app.UseMiddleware<ClientIpMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.MapHealthChecks("/health");
    }

    static void AddApiVersioning(this WebApplication app)
    {
        ApiVersionSet apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1))
            .Build();

        var versionedGroup = app.MapGroup("api/v{apiVersion:apiVersion}")
            .WithApiVersionSet(apiVersionSet);

        AddAllEndpoints(app, versionedGroup);
    }

    private static void AddAllEndpoints(WebApplication app, RouteGroupBuilder versionedGroup)
    {
        var orderGroup = versionedGroup.MapGroup("/orders");
        var inventory = versionedGroup.MapGroup("/inventory");

        OrderEndpoints.AddAllEndpoitnts(app, orderGroup);
    }

    static void AddDevelopmentMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                o.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
        }
    }
}
