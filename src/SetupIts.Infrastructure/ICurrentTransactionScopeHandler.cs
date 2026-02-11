using Microsoft.Data.SqlClient;

namespace SetupIts.Infrastructure;

public interface ICurrentTransactionScopeHandler : ICurrentTransactionScope
{
    Task SetCurrentTransaction(SqlTransaction transaction);
    Task RollbackCurrentTransaction();
    Task CommitCurrentTransaction();

}
