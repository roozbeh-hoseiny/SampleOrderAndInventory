using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SetupIts.Application.Behaviors;
using SetupIts.Application.DomainServices;
using SetupIts.Domain.DomainServices;
using SetupIts.Hosting;
using System.Reflection;

namespace SetupIts.Application.DI;
public sealed class ProjectServiceInstaller : IServiceInstaller
{
    public Assembly[]? DependantAssemblies => null;

    public IServiceCollection InstallService(IServiceCollection services, IConfiguration config)
    {
        return services
            .AddDomainServices(config)
            .InstallMediatRServices()
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
    public static IServiceCollection InstallMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly(), ApplicationAssemblyReference.Assembly);
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        });
        return services;
    }
}