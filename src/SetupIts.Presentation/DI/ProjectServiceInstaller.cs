using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SetupIts.Application;
using SetupIts.Hosting;
using SetupIts.Infrastructure;
using SetupIts.Presentation.AppCore;
using System.Reflection;

namespace SetupIts.Presentation.DI;

sealed class ProjectServiceInstaller : IServiceInstaller
{
    public Assembly[]? DependantAssemblies => [
        InfrastructureAssemblyReference.Assembly,
        ApplicationAssemblyReference.Assembly
        ];

    public IServiceCollection InstallService(IServiceCollection services, IConfiguration config)
    {
        services.Configure<RouteHandlerOptions>(o =>
        {
            o.ThrowOnBadRequest = true;
        });

        services.TryAddSingleton<IResultHandler, ResultHandler>();

        services.AddHttpContextAccessor();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
        });

        services.AddResponseCaching()
            .AddCors()
            .InstallApiVersioning()
            .AddCors()
            .InstallGlobalExceptionHandler();

        return services;
    }

}
static class ProjectServiceInstallerExtension
{
    internal static IServiceCollection InstallGlobalExceptionHandler(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    internal static IServiceCollection InstallApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Version"),
                new MediaTypeApiVersionReader("X-Version"));

        }).AddApiExplorer(options =>
        {
            // Format: v1, v2, ...
            options.GroupNameFormat = "'v'VVV";

            // Substitute version into URL
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
