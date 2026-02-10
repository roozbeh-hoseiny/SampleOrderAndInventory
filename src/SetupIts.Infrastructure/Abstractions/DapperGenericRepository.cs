using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetupIts.Domain.Abstractios;
using SetupIts.Hosting;
using SetupIts.Shared.Helpers;
using SetupIts.Shared.Primitives;

namespace SetupIts.Infrastructure.Abstractions;

public abstract class DapperGenericRepository
{
    private readonly static JsonSerializerSettings _defaultJsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    #region " Fields "
    private readonly string _connectionString;
    #endregion

    #region " Constructor "
    public DapperGenericRepository(string connectionString)
    {
        this._connectionString = connectionString;
    }
    public DapperGenericRepository(IOptionsMonitor<SetupItsGlobalOptions> opts)
    {
        this._connectionString = opts.CurrentValue.Connectionstring;
    }
    #endregion

    public virtual async Task<PrimitiveResult> SaveAsync<TEntity, TId>(
        TEntity entity,
        Func<DapperCommandDefinitionBuilder> commandBuilder,
        CancellationToken cancellationToken)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
        => await this.ExecuteTransactionAsync<TEntity, TId>(commandBuilder.Invoke(), entity, cancellationToken);

    public virtual Task<PrimitiveResult<TEntity>> QueryFirstOrDefaultAsync<TEntity, TId>(
        TId id,
        string[] fields,
        string tableName,
        CancellationToken cancellationToken)
    {
        return this.GetOneAsync(
            id,
            fields,
            tableName,
            async (connection, command) =>
            {
                var result = await connection.QueryFirstOrDefaultAsync<TEntity>(command).ConfigureAwait(false);
                if (result is null)
                    return PrimitiveResult.InternalFailure<TEntity>(
                        "Entity_Not_Found.Error",
                        $"entity '{typeof(TEntity)}' with given id: '{id}' not found");
                return result;
            },
            cancellationToken);
    }

    public virtual async Task<PrimitiveResult<TOut>> SaveAsync<TEntity, TId, TOut>(
        TEntity entity,
        Func<DapperCommandDefinitionBuilder> commandBuilder,
        CancellationToken cancellationToken)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
        => await this.ExecuteScalarTransactionAsync<TEntity, TId, TOut>(commandBuilder.Invoke(), entity, cancellationToken);

    protected async Task<TOut> RunDbCommand<TOut>(Func<SqlConnection, Task<TOut>> func)
    {
        await using var connection = new SqlConnection(this._connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        var result = await func.Invoke(connection).ConfigureAwait(false);
        return result;
    }

    protected virtual async Task<PrimitiveResult<TEntity>> GetOneAsync<TEntity, TId>(
        TId id,
        string[] fields,
        string tableName,
        Func<SqlConnection, CommandDefinition, Task<PrimitiveResult<TEntity>>> func,
        CancellationToken cancellationToken)
    {
        var fieldsStr = fields.Length == 0
           ? "*"
           : string.Join(',', fields.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct());

        var query = $"SELECT TOP 1 {fieldsStr} FROM {tableName} WHERE id = @Id; ";

        var command = DapperCommandDefinitionBuilder
            .Query(query)
            .SetParameter("Id", id)
            .Build(cancellationToken);

        using var connection = new SqlConnection(this._connectionString);
        await connection.OpenAsync();

        return await func.Invoke(connection, command).ConfigureAwait(false);
    }

    protected async Task<PrimitiveResult> ExecuteTransactionAsync<TEntity, TId>(
        DapperCommandDefinitionBuilder commandBuilder,
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
    {
        using var connection = new SqlConnection(this._connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var command = commandBuilder
                .SetTransaction(transaction)
                .Build(cancellationToken);
            var result = await connection.ExecuteAsync(command).ConfigureAwait(false);

            var now = DateTimeOffset.Now;
            var t = 0;
            foreach (var @event in entity.Events)
            {
                var outbox = new
                {
                    Id = IdHelper.CreateNewUlid(now.AddMilliseconds(++t)),
                    OccurredAt = DateTimeOffset.Now,
                    Type = @event.GetType().Name,
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(@event, _defaultJsonSerializerSettings),
                    Processed = false
                };

                await connection.ExecuteAsync(
                    @"INSERT INTO OutboxMessage (Id, OccurredAt, Type, Data, Processed)
                      VALUES (@Id, @OccurredAt, @Type, @Data, @Processed)",
                    outbox,
                    transaction);
            }
            transaction.Commit();

            entity.ClearDomainEvents();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        return PrimitiveResult.Success();
    }

    async Task<PrimitiveResult<TOut>> ExecuteScalarTransactionAsync<TEntity, TId, TOut>(
        DapperCommandDefinitionBuilder commandBuilder,
        TEntity entity,
        CancellationToken cancellationToken)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
    {
        using var connection = new SqlConnection(this._connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        TOut result = default;

        try
        {
            var command = commandBuilder
                .SetTransaction(transaction)
                .Build(cancellationToken);
            var dbResult = await connection.ExecuteScalarAsync(command).ConfigureAwait(false);

            var now = DateTimeOffset.Now;
            var t = 0;
            foreach (var @event in entity.Events)
            {
                var outbox = new
                {
                    Id = IdHelper.CreateNewUlid(now.AddMilliseconds(++t)),
                    OccurredAt = DateTimeOffset.Now,
                    Type = @event.GetType().Name,
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(@event, _defaultJsonSerializerSettings),
                    Processed = false
                };

                await connection.ExecuteAsync(
                    @"INSERT INTO OutboxMessage (Id, OccurredAt, Type, Data, Processed)
                      VALUES (@Id, @OccurredAt, @Type, @Data, @Processed)",
                    outbox,
                    transaction);
            }
            transaction.Commit();
            result = (TOut)dbResult;
            entity.ClearDomainEvents();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        return result;
    }

    protected static string QuoteName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[]";

        var text = input.Trim();

        if (text.StartsWith("["))
            text = text[1..];

        if (text.EndsWith("]"))
            text = text[..^1];

        return $"[{text}]";
    }
}
