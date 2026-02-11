using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SetupIts.Application;
using SetupIts.Domain.ValueObjects;
using SetupIts.Hosting;

namespace SetuIts.Tests.Integration;
public abstract class IntegrationTestBase
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }

    protected ProductId _productId1 = ProductId.Create("01KH5WPMCQW2DNBF72KZXF0NZW");
    protected ProductId _productId2 = ProductId.Create("01KH5WQB3BKV45TW1QCMC9FWSN");

    protected IntegrationTestBase()
    {
        this.Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();        // or Debug / XUnit
            builder.SetMinimumLevel(LogLevel.Information);
        });

        ServiceInstallerHelper.InstallServicesRecursively(
            services,
            this.Configuration,
            false,
            ApplicationAssemblyReference.Assembly);

        this.ServiceProvider = services.BuildServiceProvider();
    }
    protected async Task ClearInventoryTableAsync()
    {
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM InventoryItem")).ConfigureAwait(false);
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM OutboxMessage")).ConfigureAwait(false);
    }

    protected async Task<TOut> RunDbCommand<TOut>(Func<SqlConnection, Task<TOut>> func)
    {
        var connectionString = this.ServiceProvider.GetRequiredService<IOptions<SetupItsGlobalOptions>>().Value.ConnectionString;
        await using var connection = new SqlConnection(connectionString);
        var result = await func.Invoke(connection).ConfigureAwait(false);
        return result;
    }
    protected TRepository GetService<TRepository>() where TRepository : class
    {
        var result = this.ServiceProvider.GetRequiredService<TRepository>();
        return result;
    }
}
