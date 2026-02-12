using SetupIts.Domain.Aggregates.Inventory.Persistence;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using System.Data;

namespace SetupIts.Domain;

public interface IUnitOfWork
{
    IOrderRepository OrderRepository { get; }
    IInventoryRepository InventoryRepository { get; }

    Task<TResult> ExecuteInTransactionAsync<TResult>(
       Func<Task<TResult>> action,
       IsolationLevel isolation = IsolationLevel.ReadCommitted,
       CancellationToken cancellationToken = default);
}
