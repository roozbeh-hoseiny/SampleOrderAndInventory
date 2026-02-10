using FluentAssertions;
using SetupIts.Domain.Aggregates.Inventory;
using SetupIts.Domain.Aggregates.Inventory.Persistence;
using SetupIts.Domain.ValueObjects;

namespace SetuIts.Tests.Integration;

public sealed class InventoryRepositoryIntegrationTests : IntegrationTestBase
{
    private readonly IInventoryRepository _inventoryItemRepository;

    public InventoryRepositoryIntegrationTests()
    {
        this._inventoryItemRepository = this.GetRepository<IInventoryRepository>();
    }

    [Fact]
    public async Task AddInventoryItem_ShouldInsertItem()
    {
        // Arrange
        var onHandQty = 50;
        await this.ClearInventoryTableAsync();

        var inventoryItem = InventoryItem.Create(
            ProductId.Create(),
            1,
            Quantity.Create(onHandQty).Value).Value;

        // Act
        var result = await this._inventoryItemRepository.Add(inventoryItem, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();


        var dbItem = await this._inventoryItemRepository.GetOne(inventoryItem.Id, CancellationToken.None);
        dbItem.Should().NotBeNull();
        dbItem.IsSuccess.Should().BeTrue();
        dbItem.Value.OnHandQty.Value.Should().Be(onHandQty);
    }

    [Fact]
    public async Task ReceiveInventoryItem_ShouldUpdateItem()
    {
        // Arrange
        var onHandQty = 50;
        var receiveQty = 50;
        await this.ClearInventoryTableAsync();

        var inventoryItem = InventoryItem.Create(
            ProductId.Create(),
            1,
            Quantity.Create(onHandQty).Value).Value;

        // Act
        var addResult = await this._inventoryItemRepository.Add(inventoryItem, CancellationToken.None);
        inventoryItem.Receive(Quantity.CreateUnsafe(receiveQty));
        var result = await this._inventoryItemRepository.Update(inventoryItem, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify directly in DB
        var dbItem = await this._inventoryItemRepository.GetOne(inventoryItem.Id, CancellationToken.None);
        dbItem.Should().NotBeNull();
        dbItem.IsSuccess.Should().BeTrue();
        dbItem.Value.OnHandQty.Value.Should().Be(onHandQty + receiveQty);
        dbItem.Value.RowVersion.SequenceEqual(result.Value).Should().BeTrue();
    }

    [Fact]
    public async Task ReserveInventoryItem_ShouldUpdateItem()
    {
        // Arrange
        var onHandQty = 50;
        var reserveQty = 13;
        await this.ClearInventoryTableAsync();

        var inventoryItem = InventoryItem.Create(
            ProductId.Create(),
            1,
            Quantity.Create(onHandQty).Value).Value;

        // Act
        var addResult = await this._inventoryItemRepository.Add(inventoryItem, CancellationToken.None);
        inventoryItem.Reserve(Quantity.CreateUnsafe(reserveQty));
        var result = await this._inventoryItemRepository.Update(inventoryItem, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify directly in DB
        var dbItem = await this._inventoryItemRepository.GetOne(inventoryItem.Id, CancellationToken.None);
        dbItem.Should().NotBeNull();
        dbItem.IsSuccess.Should().BeTrue();
        dbItem.Value.ReservedQty.Value.Should().Be(reserveQty);
    }

    [Fact]
    public async Task ReleaseInventoryItem_ShouldUpdateItem()
    {
        // Arrange
        var onHandQty = 50;
        var reserveQty = 13;
        var releaseQty = 8;
        await this.ClearInventoryTableAsync();

        var inventoryItem = InventoryItem.Create(
            ProductId.Create(),
            1,
            Quantity.Create(onHandQty).Value).Value;

        // Act
        var addResult = await this._inventoryItemRepository.Add(inventoryItem, CancellationToken.None);
        inventoryItem.Reserve(Quantity.CreateUnsafe(reserveQty));
        inventoryItem.Release(Quantity.CreateUnsafe(releaseQty));
        var result = await this._inventoryItemRepository.Update(inventoryItem, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify directly in DB
        var dbItem = await this._inventoryItemRepository.GetOne(inventoryItem.Id, CancellationToken.None);
        dbItem.Should().NotBeNull();
        dbItem.IsSuccess.Should().BeTrue();
        dbItem.Value.ReservedQty.Value.Should().Be(reserveQty - releaseQty);
    }
}
