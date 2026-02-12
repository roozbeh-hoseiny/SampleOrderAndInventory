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
        this._opts = opts;
    }

    public async Task SetCurrentTransaction(SqlTransaction transaction)
    {
        if (this._currentTransaction is not null)
        {
            await this.RollbackCurrentTransaction();
        }

        this._currentTransaction = transaction;
        this._connection = transaction.Connection;
    }


    public async Task CommitCurrentTransaction()
    {
        if (this._currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await this._currentTransaction.CommitAsync();
        await this.DisposeTransactionAndConnection();
    }

    public async Task RollbackCurrentTransaction()
    {
        if (this._currentTransaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        await this._currentTransaction.RollbackAsync();
        await this.DisposeTransactionAndConnection();
    }

    public async ValueTask<SqlTransaction> GetCurrentTransaction(CancellationToken cancellationToken, bool requireExisting = false)
    {
        if (this._currentTransaction is not null)
            return this._currentTransaction;

        if (requireExisting)
            throw new InvalidOperationException("No active transaction scope.");

        await this.GetCurrentConnection(cancellationToken, requireExisting).ConfigureAwait(false);

        if (this._connection is null) throw new InvalidOperationException("No active connection.");

        this._currentTransaction = this._connection.BeginTransaction();
        return this._currentTransaction;
    }
    public async ValueTask<SqlConnection> GetCurrentConnection(CancellationToken cancellationToken, bool requireExisting = false)
    {
        if (this._connection is not null)
            return this._connection;

        if (requireExisting)
            throw new InvalidOperationException("No active transaction scope.");

        var connectionString = this._opts.CurrentValue.ConnectionString;

        this._connection = new SqlConnection(connectionString);
        await this._connection.OpenAsync(cancellationToken);

        return this._connection;
    }

    private async Task DisposeTransactionAndConnection()
    {
        try
        {
            if (this._currentTransaction is not null)
            {
                await this._currentTransaction.DisposeAsync();
            }
        }
        catch { }

        try
        {
            if (this._connection is not null)
            {
                await this._connection.DisposeAsync();
            }
        }
        catch { }

        this._currentTransaction = null;
        this._connection = null;
    }

    #region " IDisposable / IAsyncDisposable "

    public void Dispose()
    {
        if (this._disposed) return;

        try
        {
            if (this._currentTransaction is not null)
            {
                this._currentTransaction.Rollback();
                this._currentTransaction.Dispose();
            }

            this._connection?.Dispose();
        }
        catch { }

        this._currentTransaction = null;
        this._connection = null;
        this._disposed = true;

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (this._disposed) return;

        try
        {
            if (this._currentTransaction is not null)
            {
                await this._currentTransaction.RollbackAsync();
                await this._currentTransaction.DisposeAsync();
            }

            if (this._connection is not null)
                await this._connection.DisposeAsync();
        }
        catch { }

        this._currentTransaction = null;
        this._connection = null;
        this._disposed = true;

        GC.SuppressFinalize(this);
    }


    #endregion
}
