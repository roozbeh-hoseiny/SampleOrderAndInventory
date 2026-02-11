using Microsoft.Data.SqlClient;

namespace SetupIts.Infrastructure;

public interface ICurrentTransactionScope
{
    ValueTask<SqlTransaction> GetCurrentTransaction(CancellationToken cancellationToken, bool requireExisting = false);
    ValueTask<SqlConnection> GetCurrentConnection(CancellationToken cancellationToken, bool requireExisting = false);

}
