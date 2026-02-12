using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace SetupIts.Hosting;

public static class ServiceInfoHelper
{
    public static string GetEnvValue(
        IConfiguration? configuration,
        string envName,
        string defaultValue = "")
    {
        var result = configuration?[envName] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result))
        {
            var osEnvValue = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(osEnvValue)) return defaultValue;
            return osEnvValue;
        }
        return result;
    }
    public static string GetEnvValue(
        IConfiguration? configuration,
        string envName,
        Func<string> defaultValueFactory)
    {
        var result = configuration?[envName] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result))
        {
            var osEnvValue = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(osEnvValue)) return defaultValueFactory.Invoke();
            return osEnvValue;
        }
        return result;
    }
    public static string GetEnvValue(
        IConfiguration? configuration,
        string[] envNames,
        string defaultValue = "")
    {
        foreach (var name in envNames.Distinct())
        {
            var r = GetEnvValue(configuration, name, "???");
            if (!r.Equals("???")) return r;
        }
        return defaultValue;
    }

    public static string GetServiceEnvName(IConfiguration configuration,
        string envName = "env",
        string defaultEnvNameKeyName = "ASPNETCORE_ENVIRONMENT")
    {
        return GetEnvValue(configuration,
            envName,
            () => Environment.GetEnvironmentVariable(defaultEnvNameKeyName) ?? "Development");
    }

    public static string GetServiceEnvName(IConfiguration configuration,
        Func<string> defaultValueFactory,
        string envName = "env")
    {
        return GetEnvValue(configuration,
            envName,
            () => defaultValueFactory.Invoke() ?? "Development");
    }

    public static string GetEntryProjectName(IConfiguration config,
        string configName = "OTLP:ServiceName")
    {
        var result = config.GetSection(configName).Get<string>();

        if (!string.IsNullOrWhiteSpace(result)) return result;

        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly is not null)
        {
            return entryAssembly!.GetName().Name!;
        }
        return string.Empty;
    }

    public static string GetFullServiceName(IConfiguration config,
        string serviceNameConfigName = "OTLP:ServiceName",
        string envName = "env",
        string defaultEnvNameKeyName = "ASPNETCORE_ENVIRONMENT")
    {
        var serviceName = GetEntryProjectName(config, serviceNameConfigName);
        var env = GetServiceEnvName(config, envName, defaultEnvNameKeyName);
        if (string.IsNullOrWhiteSpace(env))
        {
            env = "Development";
        }

        return $"{serviceName}({env})";
    }
    public static string GetFullServiceName(IConfiguration config)
    {
        return GetFullServiceName(config, "OTLP:ServiceName", "env", "ASPNETCORE_ENVIRONMENT");
    }
}

