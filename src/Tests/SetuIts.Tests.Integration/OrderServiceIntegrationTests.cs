using FluentAssertions;
using SetupIts.Domain.DomainServices;
using SetupIts.Domain.ValueObjects;

namespace SetuIts.Tests.Integration;
public sealed class OrderServiceIntegrationTests : IntegrationTestBase
{
    private readonly IOrderService _orderService;
    public OrderServiceIntegrationTests()
    {
        this._orderService = this.GetRepository<IOrderService>();
    }
    [Fact]
    public async Task CreateOrder_ShouldWork()
    {
        var orderService = this.GetRepository<IOrderService>();
        // Arrange
        var createOrderRequest = new CreateOrderRequest(
            1,
            [
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    ProductId.Create(),
                    Quantity.CreateUnsafe(1),
                    UnitPrice.CreateUnsafe(1000)),
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    ProductId.Create(),
                    Quantity.CreateUnsafe(3),
                    UnitPrice.CreateUnsafe(852))
            ]
            );
        // Act
        var result = await orderService.CreateOrder(createOrderRequest, CancellationToken.None);

        // Assert
        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmOrder_ShouldWork()
    {
        var orderService = this.GetRepository<IOrderService>();
        // Arrange
        var createOrderRequest = new CreateOrderRequest(
            1,
            [
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    ProductId.Create(),
                    Quantity.CreateUnsafe(1),
                    UnitPrice.CreateUnsafe(1000)),
                new SetupIts.Domain.Aggregates.Ordering.OrderItemCreateData(
                    ProductId.Create(),
                    Quantity.CreateUnsafe(3),
                    UnitPrice.CreateUnsafe(852))
            ]);
        var addResult = await orderService.CreateOrder(createOrderRequest, CancellationToken.None);
        addResult.IsSuccess.Should().BeTrue();

        // Act
        var result = await orderService.ConfirmOrder(new ConfirmOrderRequest(addResult.Value), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
