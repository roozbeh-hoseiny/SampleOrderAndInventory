using Microsoft.Extensions.Logging;
using SetupIts.Domain;
using SetupIts.Domain.Aggregates.Inventory.Persistence;
using SetupIts.Domain.Aggregates.Ordering;
using SetupIts.Domain.Aggregates.Ordering.Persistence;
using SetupIts.Domain.Aggregates.Ordering.Specifications;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Shared.Primitives;

namespace SetupIts.Application.DomainServices;
internal sealed class OrderService : IOrderService
{
    #region " Fields "
    private readonly IOrderRepository _orderWriteRepository;
    private readonly IOrderReadRepository _orderReadRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IValidCustomerSpecification _validCustomerSpecification;
    private readonly IValidProductSpecification _validProductSpecification;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    #endregion

    public OrderService(
        IOrderRepository orderWriteRepository,
        IOrderReadRepository orderReadRepository,
        IInventoryRepository inventoryRepository,
        IValidCustomerSpecification validCustomerSpecification,
        IValidProductSpecification validProductSpecification,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger)
    {
        this._orderWriteRepository = orderWriteRepository;
        this._orderReadRepository = orderReadRepository;
        this._inventoryRepository = inventoryRepository;
        this._validCustomerSpecification = validCustomerSpecification;
        this._validProductSpecification = validProductSpecification;
        this._unitOfWork = unitOfWork;
        this._logger = logger;
    }

    public async Task<PrimitiveResult<OrderId>> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.OrderItems.Length == 0)
            return PrimitiveResult.Failure<OrderId>("Error", "Can not create order without items");

        var newOrder = await Order.Create(
            request.CustomerId,
            request.OrderItems,
            this._validCustomerSpecification,
            this._validProductSpecification,
            cancellationToken)
            .ConfigureAwait(false);

        if (newOrder.IsFailure)
            return PrimitiveResult.Failure<OrderId>(newOrder.Errors);

        var repoResult = await this._unitOfWork
            .ExecuteInTransactionAsync(() => this._orderWriteRepository.Add(newOrder.Value, cancellationToken))
            .ConfigureAwait(false);

        this._logger.LogInformation("An order with id:{order_id} created", newOrder.Value.Id);

        if (repoResult.IsFailure)
            return PrimitiveResult.Failure<OrderId>(repoResult.Errors);

        return newOrder.Value.Id;
    }

    public async Task<PrimitiveResult> ConfirmOrder(ConfirmOrderRequest request, CancellationToken cancellationToken)
    {
        // 1. Fetch the order by Id
        var order = await this._orderWriteRepository.GetOne(request.Id, cancellationToken).ConfigureAwait(false);
        if (order.IsFailure) return PrimitiveResult.Failure(order.Errors);

        var orderConfirmResult = order.Value.Confirm();
        if (orderConfirmResult.IsFailure) return PrimitiveResult.Failure(orderConfirmResult.Errors);

        // 2. Collect all product Ids from order items
        HashSet<ProductId> productIds = order.Value.OrderItems.Select(i => i.ProductId).ToHashSet();

        // 3. Fetch inventory info for these products
        var orderProductsInventory = await this._inventoryRepository.GetByProductIds(
            productIds.ToArray(),
            cancellationToken);
        if (orderProductsInventory.IsFailure) return PrimitiveResult.Failure(orderProductsInventory.Errors);

        // 4. Check whether all order products have inventory records
        var inventoryProductIds = orderProductsInventory.Value.Select(i => i.ProductId).ToHashSet();
        if (productIds.Any(x => !inventoryProductIds.Contains(x)))
            return PrimitiveResult.Failure("", "Insufficient Inventory info of product");

        var inventoryDict = orderProductsInventory.Value.ToDictionary(i => i.ProductId);


        // 5. Reserve inventory for each order item
        foreach (var item in order.Value.OrderItems)
        {
            var inventoryItem = inventoryDict[item.ProductId];

            var reserveResult = inventoryItem.Reserve(item.Qty);
            if (reserveResult.IsFailure)
                return PrimitiveResult.Failure(reserveResult.Errors);
        }

        return await this._unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (_, inventory) in inventoryDict)
            {
                var inventoryUpdateResult = await this._unitOfWork.InventoryRepository
                    .UpdateReservedQty(inventory, cancellationToken)
                    .ConfigureAwait(false);
                if (inventoryUpdateResult.IsFailure) return PrimitiveResult.Failure("Error", "Can not update inventory status");
            }
            var orderStatusChangeResult = await this._orderWriteRepository
                .UpdateStatus(order.Value, cancellationToken)
                .ConfigureAwait(false);

            if (orderStatusChangeResult.IsSuccess)
            {
                this._logger.LogInformation("An order with id:{order_id} confirmed", request.Id.Value);
                return PrimitiveResult.Success();
            }
            this._logger.LogError("error in confirming order with id:{order_id}. {@errors}",
                request.Id.Value,
                orderStatusChangeResult.Errors);

            return PrimitiveResult.Failure(orderStatusChangeResult.Errors);
        });
    }


    public async Task<PrimitiveResult> CancelOrder(
    CancelOrderRequest request,
    CancellationToken cancellationToken)
    {
        // 1. Fetch the order
        var order = await this._orderWriteRepository
            .GetOne(request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (order.IsFailure)
            return PrimitiveResult.Failure(order.Errors);

        // 2. Cancel order (domain rule)
        var cancelResult = order.Value.Cancel();
        if (cancelResult.IsFailure)
            return PrimitiveResult.Failure(cancelResult.Errors);

        // 3. Collect product ids
        HashSet<ProductId> productIds =
            order.Value.OrderItems
                .Select(i => i.ProductId)
                .ToHashSet();

        // 4. Fetch inventory items
        var inventoriesResult = await this._inventoryRepository
            .GetByProductIds(productIds.ToArray(), cancellationToken)
            .ConfigureAwait(false);

        if (inventoriesResult.IsFailure)
            return PrimitiveResult.Failure(inventoriesResult.Errors);

        var inventoryDict =
            inventoriesResult.Value.ToDictionary(i => i.ProductId);

        // 5. Release reserved inventory
        foreach (var item in order.Value.OrderItems)
        {
            if (!inventoryDict.TryGetValue(item.ProductId, out var inventory))
                return PrimitiveResult.Failure("", "Inventory record not found");

            var releaseResult = inventory.Release(item.Qty);
            if (releaseResult.IsFailure)
                return PrimitiveResult.Failure(releaseResult.Errors);
        }

        // 6. Persist changes atomically
        return await this._unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (_, inventory) in inventoryDict)
            {
                var inventoryUpdateResult =
                    await this._unitOfWork.InventoryRepository
                        .UpdateReservedQty(inventory, cancellationToken)
                        .ConfigureAwait(false);

                if (inventoryUpdateResult.IsFailure)
                    return PrimitiveResult.Failure(
                        "Error",
                        "Can not update inventory status");
            }

            var orderStatusChangeResult =
                await this._orderWriteRepository
                    .UpdateStatus(order.Value, cancellationToken)
                    .ConfigureAwait(false);

            return orderStatusChangeResult.IsSuccess
                ? PrimitiveResult.Success()
                : PrimitiveResult.Failure(orderStatusChangeResult.Errors);
        });
    }


    public async Task<PrimitiveResult<OrderReadModel>> GetOne(OrderId Id, CancellationToken cancellationToken)
    {
        return await this._orderReadRepository.GetOne(Id, cancellationToken);
    }


    async Task<PrimitiveResult> ChangeOrderStatus(
        OrderId id,
        Func<Order, PrimitiveResult> func,
        CancellationToken cancellationToken)
    {
        var order = await this._orderWriteRepository.GetOne(id, cancellationToken)
               .ConfigureAwait(false);

        if (order.IsFailure) return PrimitiveResult.Failure(order.Errors);

        var changeStatusResult = func.Invoke(order.Value);

        if (changeStatusResult.IsFailure) return PrimitiveResult.Failure(changeStatusResult.Errors);

        var result = await this._unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var result = await this._orderWriteRepository
                .UpdateStatus(order.Value, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure) return PrimitiveResult.Failure(changeStatusResult.Errors);

            return PrimitiveResult.Success();
        });
        return result;

    }
}
