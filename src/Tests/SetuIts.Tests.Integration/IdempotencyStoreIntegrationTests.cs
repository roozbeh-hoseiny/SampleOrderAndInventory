using Dapper;
using FluentAssertions;
using SetupIts.Infrastructure.Idempotency;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SetuIts.Tests.Integration;
public sealed class IdempotencyStoreIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IIdempotencyStore _idempotencyStore;

    public IdempotencyStoreIntegrationTests()
    {
        this._idempotencyStore = this.GetRepository<IIdempotencyStore>();
    }

    [Fact]
    public async Task TryBeginAsync_ShouldInsertItem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new { orderId = 1 };
        var requestHash = ComputeSha256(request);
        var timeOut = TimeSpan.FromMinutes(30);

        await this.ClearIdempotencyTableAsync();

        // Act
        var result = await this._idempotencyStore.TryBeginAsync(id, requestHash, timeOut, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

    }

    [Fact]
    public async Task TryBeginAsync_ForExpired_ShouldInsertItem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new { orderId = 1 };
        var requestHash = ComputeSha256(request);
        var timeOut = TimeSpan.FromMilliseconds(1);

        await this.ClearIdempotencyTableAsync();

        // Act
        var first = await this._idempotencyStore.TryBeginAsync(id, requestHash, timeOut, CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(3));
        var result = await this._idempotencyStore.TryBeginAsync(id, requestHash, TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

    }

    [Fact]
    public async Task TryBeginAsync_ForInProgress_ShouldNotInsertItem()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new { orderId = 1 };
        var requestHash = ComputeSha256(request);
        var timeOut = TimeSpan.FromSeconds(10);

        await this.ClearIdempotencyTableAsync();

        // Act
        var first = await this._idempotencyStore.TryBeginAsync(id, requestHash, timeOut, CancellationToken.None);
        var result = await this._idempotencyStore.TryBeginAsync(id, requestHash, timeOut, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

    }

    [Fact]
    public async Task TryBeginAsync_ForMultipleIdempotencyRequest_ShouldInsertItem()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = new { orderId = 1 };
        var requestHash = ComputeSha256(request);
        var timeOut = TimeSpan.FromSeconds(10);

        await this.ClearIdempotencyTableAsync();

        // Act
        var first = await this._idempotencyStore.TryBeginAsync(id1, requestHash, timeOut, CancellationToken.None);
        var second = await this._idempotencyStore.TryBeginAsync(id2, requestHash, timeOut, CancellationToken.None);

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
    }

    async Task ClearIdempotencyTableAsync()
    {
        await this.RunDbCommand(connection => connection.ExecuteAsync("DELETE FROM Idempotency")).ConfigureAwait(false);
    }

    static byte[] ComputeSha256(object obj)
    {
        var json = JsonSerializer.Serialize(obj, Options);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(bytes);
    }
}
