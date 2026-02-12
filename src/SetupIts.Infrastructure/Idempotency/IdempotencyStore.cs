using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SetupIts.Domain.Aggregates.Inventory;
using SetupIts.Hosting;
using SetupIts.Infrastructure.Abstractions;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Idempotency;

public sealed class IdempotencyStore : DapperGenericRepository, IIdempotencyStore
{
    const string TABLE_NAME = "Idempotency";
    const string AddIdempotencyQuery = $"""
        INSERT INTO {TABLE_NAME}
        (
            {nameof(IdempotencyModel.Id)},
            {nameof(IdempotencyModel.RequestHash)},
            {nameof(IdempotencyModel.Status)},
            {nameof(IdempotencyModel.CreatedAt)},
            {nameof(IdempotencyModel.UpdatedAt)},
            {nameof(IdempotencyModel.ExpireAt)}
        )
        VALUES
        (
            @{nameof(IdempotencyModel.Id)},
            @{nameof(IdempotencyModel.RequestHash)},
            @{nameof(IdempotencyModel.Status)},
            @{nameof(IdempotencyModel.CreatedAt)},
            @{nameof(IdempotencyModel.UpdatedAt)},
            @{nameof(IdempotencyModel.ExpireAt)}
        )
        """;
    const string UpdateIdempotencyQuery = $"""
        Update {TABLE_NAME}
        SET
            {nameof(IdempotencyModel.RequestHash)} = @{nameof(IdempotencyModel.RequestHash)},
            {nameof(IdempotencyModel.Status)} = @{nameof(IdempotencyModel.Status)},
            {nameof(IdempotencyModel.UpdatedAt)} = @{nameof(IdempotencyModel.UpdatedAt)}
        OUTPUT inserted.{nameof(InventoryItem.RowVersion)}
        WHERE 
            {nameof(IdempotencyModel.Id)} = @{nameof(InventoryItem.Id)}
            AND {nameof(IdempotencyModel.RowVersion)} = @{nameof(InventoryItem.RowVersion)};
        """;
    private readonly ILogger<IdempotencyStore> _logger;

    public IdempotencyStore(
        ICurrentTransactionScope currentTransactionScope,
        IOptionsMonitor<SetupItsGlobalOptions> opts,
        ILogger<IdempotencyStore> logger) : base(currentTransactionScope, opts)
    {
        this._logger = logger;
    }


    public async Task<PrimitiveResult<IdempotencyModel>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await this.QueryFirstOrDefaultAsync<IdempotencyModel, Guid>(
            id,
        [
                nameof(IdempotencyModel.Id),
                nameof(IdempotencyModel.RequestHash),
                nameof(IdempotencyModel.Status),
                nameof(IdempotencyModel.CreatedAt),
                nameof(IdempotencyModel.UpdatedAt)
            ],
        TABLE_NAME,
        cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<PrimitiveResult<IdempotencyModel>> TryBeginAsync(Guid id, byte[] requestHash, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var expiryTime = now.Add(timeout);
        try
        {
            var newRecord = IdempotencyModel.Create(id, requestHash, expiryTime);

            var insertResult = await this.WithConnectionAsync(connection => connection.ExecuteAsync(
                    DapperCommandDefinitionBuilder
                    .Query(AddIdempotencyQuery)
                    .SetParameter($"@{nameof(IdempotencyModel.Id)}", newRecord.Id)
                    .SetParameter($"@{nameof(IdempotencyModel.RequestHash)}", newRecord.RequestHash)
                    .SetParameter($"@{nameof(IdempotencyModel.Status)}", newRecord.Status)
                    .SetParameter($"@{nameof(IdempotencyModel.CreatedAt)}", newRecord.CreatedAt)
                    .SetParameter($"@{nameof(IdempotencyModel.UpdatedAt)}", newRecord.UpdatedAt)
                    .SetParameter($"@{nameof(IdempotencyModel.ExpireAt)}", newRecord.ExpireAt)
                    .Build(cancellationToken)), cancellationToken)
                .ConfigureAwait(false);

            if (insertResult > 0)
            {
                return newRecord;
            }
        }
        catch { }

        var existingResult = await this.QueryFirstOrDefaultAsync<IdempotencyModel, Guid>(
            id,
            [],
            TABLE_NAME,
            cancellationToken)
            .ConfigureAwait(false);

        if (existingResult.IsFailure || existingResult.Value is null)
        {
            this._logger.LogError("Idempotency record missing. {id}", id);
            return PrimitiveResult.InternalFailure<IdempotencyModel>("Missing.Error", "Idempotency record missing!");
        }

        var existing = existingResult.Value;

        if (existing.Status == IdempotencyStatus.Completed)
            return existing;

        if (existing.Status == IdempotencyStatus.InProgress && now < existing.ExpireAt)
        {
            this._logger.LogWarning("Request is already in progress. {id}", id);
            return PrimitiveResult.Failure<IdempotencyModel>("InProgress.Error", "Request is already in progress");
        }

        existing.Status = IdempotencyStatus.InProgress;
        existing.RequestHash = requestHash;
        existing.UpdatedAt = now;
        existing.ExpireAt = expiryTime;

        try
        {
            var updateResult = await this.RunDbCommand(async connection =>
            {
                var dbResult = await connection.ExecuteAsync(
                    DapperCommandDefinitionBuilder
                        .Query(UpdateIdempotencyQuery)
                        .SetParameter($"@{nameof(IdempotencyModel.Id)}", existing.Id)
                        .SetParameter($"@{nameof(IdempotencyModel.RequestHash)}", existing.RequestHash)
                        .SetParameter($"@{nameof(IdempotencyModel.Status)}", existing.Status)
                        .SetParameter($"@{nameof(IdempotencyModel.UpdatedAt)}", existing.UpdatedAt)
                        .SetParameter($"@{nameof(IdempotencyModel.RowVersion)}", existing.RowVersion)
                        .SetParameter($"@{nameof(IdempotencyModel.ExpireAt)}", existing.ExpireAt)
                        .Build(cancellationToken));
                return dbResult > 0 ? PrimitiveResult.Success() : PrimitiveResult.InternalFailure("Error", "Can not update idempotency model");
            },
            cancellationToken).ConfigureAwait(false);

            if (updateResult.IsSuccess)
            {
                return existing;
            }
            this._logger.LogError("Error in updating existing idempotency record with id : {id}. {@error}", id, updateResult.Errors);
            return PrimitiveResult.InternalFailure<IdempotencyModel>("Error", updateResult.Error.Message);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Exception in updating existing idempotency record with id : {id}.", id);
            return PrimitiveResult.InternalFailure<IdempotencyModel>("Exception", ex.Message);
        }
    }

    public async Task CompleteAsync(Guid key, int statusCode, string responseBody)
    {
        var updateResult = await this.RunDbCommand(async connection =>
        {
            var dbResult = await connection.ExecuteAsync(
                DapperCommandDefinitionBuilder
                    .Query($"UPDATE  {TABLE_NAME} SET {nameof(IdempotencyModel.Status)} = @{nameof(IdempotencyModel.Status)},{nameof(IdempotencyModel.ResponseCode)} = @{nameof(IdempotencyModel.ResponseCode)}, {nameof(IdempotencyModel.ResponseBody)} = @{nameof(IdempotencyModel.ResponseBody)} WHERE {nameof(IdempotencyModel.Id)} = @{nameof(IdempotencyModel.Id)}")
                    .SetParameter($"@{nameof(IdempotencyModel.Id)}", key)
                    .SetParameter($"@{nameof(IdempotencyModel.Status)}", IdempotencyStatus.Completed)
                    .SetParameter($"@{nameof(IdempotencyModel.ResponseCode)}", statusCode)
                    .SetParameter($"@{nameof(IdempotencyModel.ResponseBody)}", responseBody)
                    .Build());
            return dbResult > 0 ? PrimitiveResult.Success() : PrimitiveResult.InternalFailure("Error", "Can not update idempotency model");
        }, CancellationToken.None).ConfigureAwait(false);
    }
    public async Task FailedAsync(Guid key, string reason)
    {
        var updateResult = await this.RunDbCommand(async connection =>
        {
            var dbResult = await connection.ExecuteAsync(
                DapperCommandDefinitionBuilder
                    .Query($"UPDATE  {TABLE_NAME} SET {nameof(IdempotencyModel.Status)} = @{nameof(IdempotencyModel.Status)}, {nameof(IdempotencyModel.ResponseBody)} = @{nameof(IdempotencyModel.ResponseBody)} WHERE {nameof(IdempotencyModel.Id)} = @{nameof(IdempotencyModel.Id)}")
                    .SetParameter($"@{nameof(IdempotencyModel.Id)}", key)
                    .SetParameter($"@{nameof(IdempotencyModel.Status)}", IdempotencyStatus.Failed)
                    .Build());
            return dbResult > 0 ? PrimitiveResult.Success() : PrimitiveResult.InternalFailure("Error", "Can not update idempotency model");
        }, CancellationToken.None).ConfigureAwait(false);
    }
}