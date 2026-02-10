using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Reflection;

namespace SetupIts.Hosting;

public static class ServiceInstallerHelper
{
    private static Hashtable _installedAssemblies = new();

    public static IServiceCollection InstallServicesRecursively(IServiceCollection services, IConfiguration config, bool ignoreInstalled, params Assembly[] assemblies)
    {
        var serviceInstallers = assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(a => IsAssignableToType<IServiceInstaller>(a))
            .Select(Activator.CreateInstance)
            .Cast<IServiceInstaller>();

        foreach (var installer in serviceInstallers)
        {
            Install(services, config, installer, ignoreInstalled);
        }

        return services;
    }

    public static IServiceCollection Install(IServiceCollection services, IConfiguration config, IServiceInstaller installer, bool ignoreInstalled)
    {
        if (installer.DependantAssemblies?.Any() ?? false)
        {
            foreach (var dependantAssembly in installer.DependantAssemblies)
            {
                if (_installedAssemblies.Contains(dependantAssembly.FullName!) && ignoreInstalled) continue;
                InstallServicesRecursively(services, config, ignoreInstalled, new Assembly[1] { dependantAssembly });
                if (ignoreInstalled)
                    _installedAssemblies.Add(dependantAssembly.FullName!, dependantAssembly.FullName!);
            }
        }

        return installer.InstallService(services, config);
    }


    public static IServiceCollection Install<TInstaller>(IServiceCollection services, IConfiguration config, bool ignoreInstalled)
        where TInstaller : IServiceInstaller, new() => Install(services, config, new TInstaller(), ignoreInstalled);

    public static IServiceCollection InstallServices(IServiceCollection services, IConfiguration config, params Assembly[] assemblies)
    {
        var serviceInstallers = assemblies
            .SelectMany(a => a.DefinedTypes)
            .Where(a => IsAssignableToType<IServiceInstaller>(a))
            .Select(Activator.CreateInstance)
            .Cast<IServiceInstaller>();

        foreach (var installer in serviceInstallers)
        {
            installer.InstallService(services, config);
        }

        return services;
    }

    private static bool IsAssignableToType<T>(TypeInfo typeInfo)
        => typeof(T).IsAssignableFrom(typeInfo)
            && !typeInfo.IsInterface
            && !typeInfo.IsAbstract;
}
