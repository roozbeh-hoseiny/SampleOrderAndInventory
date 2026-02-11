using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SetupIts.Hosting;

namespace SetupIts.Infrastructure;

public sealed class CurrentTransactionScopeHandler : ICurrentTransactionScope, ICurrentTransactionScopeHandler
{
    private SqlConnection? _connection;
    private SqlTransaction? _currentTransaction;
    private readonly IOptionsMonitor<SetupItsGlobalOptions> _opts;
    private bool _disposed;

    public CurrentTransactionScopeHandler(IOptionsMonitor<SetupItsGlobalOptions> opts)
    {
        _opts = opts;
    }

    public async Task SetCurrentTransaction(SqlTransaction transaction)
    {
        if (_currentTransaction is not null)
        {
            await RollbackCurrentTransaction();
        }

        _currentTransaction = transaction;
        _connection = transaction.Connection;
    }


    public async Task CommitCurrentTransaction()
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _currentTransaction.CommitAsync();
        await DisposeTransactionAndConnection();
    }

    public async Task RollbackCurrentTransaction()
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        await _currentTransaction.RollbackAsync();
        await DisposeTransactionAndConnection();
    }

    public async ValueTask<SqlTransaction> GetCurrentTransaction(CancellationToken cancellationToken, bool requireExisting = false)
    {
        if (_currentTransaction is not null)
            return _currentTransaction;

        if (requireExisting)
            throw new InvalidOperationException("No active transaction scope.");

        var connectionString = _opts.CurrentValue.ConnectionString;

        _connection = new SqlConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        _currentTransaction = _connection.BeginTransaction();
        return _currentTransaction;
    }
    public async ValueTask<SqlConnection> GetCurrentConnection(CancellationToken cancellationToken, bool requireExisting = false)
    {
        if (_connection is not null)
            return _connection;

        if (requireExisting)
            throw new InvalidOperationException("No active transaction scope.");

        var connectionString = _opts.CurrentValue.ConnectionString;

        _connection = new SqlConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        return _connection;
    }

    private async Task DisposeTransactionAndConnection()
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
            }
        }
        catch { }

        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch { }

        _currentTransaction = null;
        _connection = null;
    }

    #region " IDisposable / IAsyncDisposable "

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            if (_currentTransaction is not null)
            {
                _currentTransaction.Rollback();
                _currentTransaction.Dispose();
            }

            _connection?.Dispose();
        }
        catch { }

        _currentTransaction = null;
        _connection = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
            }

            if (_connection is not null)
                await _connection.DisposeAsync();
        }
        catch { }

        _currentTransaction = null;
        _connection = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }


    #endregion
}
