using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SetupIts.Domain.Aggregates.Inventory.Persistence;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Hosting;
using System.Data;

namespace SetupIts.Infrastructure;
public interface IUnitOfWork
{
    IOrderRepository OrderRepository { get; }
    IInventoryRepository InventoryRepository { get; }

    Task<TResult> ExecuteInTransactionAsync<TResult>(
       Func<Task<TResult>> action,
       IsolationLevel isolation = IsolationLevel.ReadCommitted,
       CancellationToken cancellationToken = default);
}
public sealed class UnitOfWork : IUnitOfWork
{
    #region " Fields "
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentTransactionScopeHandler _currentTransactionScopeHandler;
    private readonly string _connectionString;
    #endregion

    public IOrderRepository OrderRepository => this.GetRepository<IOrderRepository>();
    public IInventoryRepository InventoryRepository => this.GetRepository<IInventoryRepository>();

    public UnitOfWork(
        IServiceProvider serviceProvider,
        ICurrentTransactionScopeHandler currentTransactionScopeHandler,
        IOptionsMonitor<SetupItsGlobalOptions> opts)
    {
        this._connectionString = opts.CurrentValue.ConnectionString;
        this._serviceProvider = serviceProvider;
        this._currentTransactionScopeHandler = currentTransactionScopeHandler;
    }





    async ValueTask<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    async Task<TResult> WithConnectionAsync<TResult>(Func<SqlConnection, Task<TResult>> action, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        return await action(connection);
    }
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
       Func<Task<TResult>> action,
       IsolationLevel isolation = IsolationLevel.ReadCommitted,
       CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = connection.BeginTransaction(isolation);
        await this._currentTransactionScopeHandler.SetCurrentTransaction(transaction);

        try
        {
            var result = await action();
            await this._currentTransactionScopeHandler.CommitCurrentTransaction();
            return result;
        }
        catch (Exception ex)
        {
            await this._currentTransactionScopeHandler.RollbackCurrentTransaction();
            throw;
        }
    }

    TRepository GetRepository<TRepository>() where TRepository : class
    {
        var result = this._serviceProvider.GetRequiredService<TRepository>();
        return result;
    }
}
