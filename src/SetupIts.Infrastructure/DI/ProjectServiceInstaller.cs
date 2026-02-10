using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SetupIts.Domain.Abstractios;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Idempotency;
using SetupIts.Infrastructure.Inventory;
using SetupIts.Infrastructure.Orders;
using System.Reflection;

namespace SetupIts.Infrastructure.DI;
public sealed class ProjectServiceInstaller : IServiceInstaller
{
    public Assembly[]? DependantAssemblies => null;

    public IServiceCollection InstallService(IServiceCollection services, IConfiguration config)
    {
        AddAllDapperTypeHandlers();

        return services
            .AddGlobalOptions(config)
            .AddRepositories()
            .AddSpecifications()
            ;
    }

    static void AddAllDapperTypeHandlers()
    {
        var typeHandlers = InfrastructureAssemblyReference.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface &&
                            t.BaseType != null &&
                            t.BaseType.IsGenericType &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(SqlMapper.TypeHandler<>))
                .ToList();

        foreach (var handlerType in typeHandlers)
        {
            if (handlerType.BaseType is null) continue;
            var handledType = handlerType.BaseType.GetGenericArguments().First();
            var handlerInstance = Activator.CreateInstance(handlerType);

            var instance = (SqlMapper.ITypeHandler)handlerInstance;

            if (instance is null) continue;

            SqlMapper.AddTypeHandler(handledType, instance);
        }
    }
}

static class ServiceCollectionExtension
{
    internal static IServiceCollection AddGlobalOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SetupItsGlobalOptions>(config.GetSection("GlobalOptions"));
        return services;
    }
    internal static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.Scan(scan =>
            scan.FromAssembliesOf(typeof(InventoryRepository))
            .AddClasses(c => c.AssignableTo(typeof(IDomainRepository)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.TryAddScoped<IIdempotencyStore, IdempotencyStore>();
        services.TryAddScoped<IOrderReadRepository, OrderReadRepository>();

        return services;
    }
    internal static IServiceCollection AddSpecifications(this IServiceCollection services)
    {
        services.TryAddScoped<IValidCustomerSpecification, ValidCustomerSpecification>();
        services.TryAddScoped<IValidProductSpecification, ValidProductSpecification>();

        return services;
    }
}