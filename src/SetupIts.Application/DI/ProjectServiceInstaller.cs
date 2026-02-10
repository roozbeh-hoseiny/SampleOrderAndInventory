using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SetupIts.Application.DomainServices;
using SetupIts.Domain.DomainServices;
using SetupIts.Hosting;
using SetupIts.Infrastructure;
using System.Reflection;

namespace SetupIts.Application.DI;
public sealed class ProjectServiceInstaller : IServiceInstaller
{
    public Assembly[]? DependantAssemblies => [InfrastructureAssemblyReference.Assembly];

    public IServiceCollection InstallService(IServiceCollection services, IConfiguration config)
    {
        return services
            .AddDomainServices(config)
            ;
    }
}

static class ServiceCollectionExtension
{
    internal static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration config)
    {
        services.TryAddScoped<IOrderService, OrderService>();
        return services;
    }
}