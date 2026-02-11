using Dapper;
using FluentAssertions;
using SetupIts.Domain.Aggregates.Inventory;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;
using SetupIts.Infrastructure;

namespace SetuIts.Tests.Integration;
public sealed class OrderServiceIntegrationTests : IntegrationTestBase
{

    private readonly IUnitOfWork _unitOfWork;
    public OrderServiceIntegrationTests()
    {
        this._unitOfWork = this.GetService<IUnitOfWork>();
    }


    [Fact]
    public async Task CreateOrder_ShouldWork()
    {
        await this.ClearOrderTables();
        var orderService = this.GetService<IOrderService>();

        var result = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmOrder_ShouldWork()
    {
        await this.ClearOrderTables();
        var orderService = this.GetService<IOrderService>();

        var onHandQty = 50;
        await this.ClearInventoryTableAsync();

        var inventoryItem1 = InventoryItem.Create(
            this._productId1,
            1,
            Quantity.Create(onHandQty).Value).Value;
        var inventoryItem2 = InventoryItem.Create(
            this._productId2,
            1,
            Quantity.Create(onHandQty).Value).Value;

        // Act
        var inventoryItemAddResult = await this._unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await this._unitOfWork.InventoryRepository.Add(inventoryItem1, CancellationToken.None);
            return await this._unitOfWork.InventoryRepository.Add(inventoryItem2, CancellationToken.None);
        });


        var addResult = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        addResult.IsSuccess.Should().BeTrue();

        var result = await orderService.ConfirmOrder(new ConfirmOrderRequest(addResult.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelOrder_ShouldWork()
    {
        await this.ClearOrderTables();

        var orderService = this.GetService<IOrderService>();

        var createResult = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var confirmResult = await orderService.ConfirmOrder(new ConfirmOrderRequest(createResult.Value), CancellationToken.None);
        confirmResult.IsSuccess.Should().BeTrue();

        var result = await orderService.CancelOrder(new CancelOrderRequest(createResult.Value), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Confirming_CancelledOrder_ShouldNotWork()
    {
        await this.ClearOrderTables();

        var orderService = this.GetService<IOrderService>();

        var createResult = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var cancelResult = await orderService.CancelOrder(new CancelOrderRequest(createResult.Value), CancellationToken.None);
        cancelResult.IsSuccess.Should().BeTrue();

        var confimrResult = await orderService.ConfirmOrder(new ConfirmOrderRequest(createResult.Value), CancellationToken.None);
        confimrResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Cancelling_CancelledOrder_ShouldNotWork()
    {
        await this.ClearOrderTables();

        var orderService = this.GetService<IOrderService>();

        var createResult = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var cancelResult = await orderService.CancelOrder(new CancelOrderRequest(createResult.Value), CancellationToken.None);
        cancelResult.IsSuccess.Should().BeTrue();

        var seconCancelResult = await orderService.CancelOrder(new CancelOrderRequest(createResult.Value), CancellationToken.None);
        seconCancelResult.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetOneOrderWithItems_ShouldWork()
    {
        await this.ClearOrderTables();

        var orderService = this.GetService<IOrderService>();

        var req = CreateNewOrderRequest();
        var createResult = await orderService.CreateOrder(req, CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var result = await orderService.GetOne(createResult.Value, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderItems.Count.Should().Be(req.OrderItems.Length);
    }

    CreateOrderRequest CreateNewOrderRequest() => new CreateOrderRequest(
           1,
           [
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    _productId1,
                    Quantity.CreateUnsafe(1),
                    UnitPrice.CreateUnsafe(1000)),
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    _productId2,
                    Quantity.CreateUnsafe(3),
                    UnitPrice.CreateUnsafe(852))
           ]);

    async Task ClearOrderTables()
    {
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM [OrderItem]")).ConfigureAwait(false);
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM [Order]")).ConfigureAwait(false);
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM [Outboxmessage]")).ConfigureAwait(false);
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM [InventoryItem]")).ConfigureAwait(false);
    }
}
