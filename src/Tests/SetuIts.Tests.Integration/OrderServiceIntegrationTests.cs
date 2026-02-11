using FluentAssertions;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;

namespace SetuIts.Tests.Integration;
public sealed class OrderServiceIntegrationTests : IntegrationTestBase
{
    ProductId _productId1 = ProductId.Create("01KH5WPMCQW2DNBF72KZXF0NZW");
    ProductId _productId2 = ProductId.Create("01KH5WQB3BKV45TW1QCMC9FWSN");

    public OrderServiceIntegrationTests()
    {
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

    [Fact]
    public async Task CreateOrder_ShouldWork()
    {
        var orderService = this.GetRepository<IOrderService>();

        var result = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmOrder_ShouldWork()
    {
        var orderService = this.GetRepository<IOrderService>();
        // Arrange
        var addResult = await orderService.CreateOrder(CreateNewOrderRequest(), CancellationToken.None);
        addResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await orderService.ConfirmOrder(new ConfirmOrderRequest(addResult.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CancelOrder_ShouldWork()
    {
        var orderService = this.GetRepository<IOrderService>();

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
        var orderService = this.GetRepository<IOrderService>();

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
        var orderService = this.GetRepository<IOrderService>();

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
        var orderService = this.GetRepository<IOrderService>();

        var req = CreateNewOrderRequest();
        var createResult = await orderService.CreateOrder(req, CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var result = await orderService.GetOne(createResult.Value, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderItems.Count.Should().Be(req.OrderItems.Length);
    }
}
