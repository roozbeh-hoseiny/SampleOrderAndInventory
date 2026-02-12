using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SetupIts.Domain.Abstractios;
using SetupIts.Hosting;
using SetupIts.Shared.Helpers;
using SetupIts.Shared.Primitives;
using System.Buffers;
using System.Data;
using static Dapper.SqlMapper;

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
    protected readonly ICurrentTransactionScope _currentTransactionScope;
    protected readonly IOptionsMonitor<SetupItsGlobalOptions> _opts;
    private readonly bool _isReadonly;
    #endregion

    #region " Constructor "
    public DapperGenericRepository(
        ICurrentTransactionScope currentTransactionScope,
        IOptionsMonitor<SetupItsGlobalOptions> opts,
        bool isReadonly = false)
    {
        var builder = new SqlConnectionStringBuilder(opts.CurrentValue.ConnectionString);
        if (isReadonly)
            builder.ApplicationIntent = ApplicationIntent.ReadOnly;

        this._connectionString = builder.ConnectionString;
        this._currentTransactionScope = currentTransactionScope;
        this._opts = opts;
        this._isReadonly = isReadonly;
    }
    #endregion

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

    public virtual async Task<PrimitiveResult<MultipleReaderResult<T1, T2>>> QueryMultipleAsync<T1, T2>(
        DapperCommandDefinitionBuilder commandBuilder,
        Func<GridReader, Task<T1>> mapper1,
        Func<GridReader, Task<T2>> mapper2,
        CancellationToken cancellationToken)
    {
        return await this.WithConnectionAsync(
            async (connection) =>
            {
                var dbResult = await connection.QueryMultipleAsync(commandBuilder.Build(cancellationToken)).ConfigureAwait(false);
                if (dbResult is null) return PrimitiveResult.InternalFailure<MultipleReaderResult<T1, T2>>("Error", "null query multiple result");

                var result = new MultipleReaderResult<T1, T2>()
                {
                    Item1 = await mapper1(dbResult).ConfigureAwait(false),
                    Item2 = await mapper2(dbResult).ConfigureAwait(false)
                };
                return PrimitiveResult.Success(result);

            },
            cancellationToken)
            .ConfigureAwait(false);
    }
    public virtual async Task<PrimitiveResult<MultipleReaderResult<T1, T2, T3>>> QueryMultipleAsync<T1, T2, T3>(
        DapperCommandDefinitionBuilder commandBuilder,
        Func<GridReader, Task<T1>> mapper1,
        Func<GridReader, Task<T2>> mapper2,
        Func<GridReader, Task<T3>> mapper3,
        CancellationToken cancellationToken)
    {
        return await this.WithConnectionAsync(
            async (connection) =>
            {
                var dbResult = await connection.QueryMultipleAsync(commandBuilder.Build(cancellationToken)).ConfigureAwait(false);
                if (dbResult is null) return PrimitiveResult.InternalFailure<MultipleReaderResult<T1, T2, T3>>("Error", "null query multiple result");

                var result = new MultipleReaderResult<T1, T2, T3>()
                {
                    Item1 = await mapper1(dbResult).ConfigureAwait(false),
                    Item2 = await mapper2(dbResult).ConfigureAwait(false),
                    Item3 = await mapper3(dbResult).ConfigureAwait(false)
                };
                return PrimitiveResult.Success(result);

            },
            cancellationToken)
            .ConfigureAwait(false);
    }
    public virtual async Task<PrimitiveResult<MultipleReaderResult<T1, T2, T3, T4>>> QueryMultipleAsync<T1, T2, T3, T4>(
        DapperCommandDefinitionBuilder commandBuilder,
        Func<GridReader, Task<T1>> mapper1,
        Func<GridReader, Task<T2>> mapper2,
        Func<GridReader, Task<T3>> mapper3,
        Func<GridReader, Task<T4>> mapper4,
        CancellationToken cancellationToken)
    {
        return await this.WithConnectionAsync(
            async (connection) =>
            {
                var dbResult = await connection.QueryMultipleAsync(commandBuilder.Build(cancellationToken)).ConfigureAwait(false);
                if (dbResult is null) return PrimitiveResult.InternalFailure<MultipleReaderResult<T1, T2, T3, T4>>("Error", "null query multiple result");

                var result = new MultipleReaderResult<T1, T2, T3, T4>()
                {
                    Item1 = await mapper1(dbResult).ConfigureAwait(false),
                    Item2 = await mapper2(dbResult).ConfigureAwait(false),
                    Item3 = await mapper3(dbResult).ConfigureAwait(false),
                    Item4 = await mapper4(dbResult).ConfigureAwait(false)
                };
                return PrimitiveResult.Success(result);

            },
            cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<PrimitiveResult<TOut>> SaveAsync<TEntity, TId, TOut>(
        TEntity entity,
        DapperCommandDefinitionBuilder commandBuilder,
        CancellationToken cancellationToken)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
    {
        var transaction = await this._currentTransactionScope.GetCurrentTransaction(cancellationToken)
            .ConfigureAwait(false);

        var connection = transaction.Connection;

        var command = commandBuilder
                   .SetTransaction(transaction)
                   .Build(cancellationToken);

        var dbResult = await connection.ExecuteScalarAsync<TOut>(command)
            .ConfigureAwait(false);

        if (dbResult is null)
        {
            return PrimitiveResult.InternalFailure<TOut>("Error", "Db null result");
        }

        await this.AddEntityEvents<TEntity, TId>(entity, transaction);

        return dbResult!;

    }

    protected async Task<TOut> RunDbCommand<TOut>(Func<SqlConnection, Task<TOut>> func, CancellationToken cancellationToken)
    {
        var result = await this.WithConnectionAsync(func, cancellationToken)
            .ConfigureAwait(false);
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


        var result = await this.WithConnectionAsync(
            (connection) => func.Invoke(connection, command),
            cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    protected async Task<PrimitiveResult> AddEntityEvents<TEntity, TId>(TEntity entity, SqlTransaction transaction)
        where TEntity : EntityBase<TId>
        where TId : IEquatable<TId>
    {
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
                IsIntegrationEvent = @event.IsIntegrationEvent,
                Processed = false
            };

            await transaction.Connection.ExecuteAsync(
                @"INSERT INTO OutboxMessage (Id, OccurredAt, Type, Data, IsIntegrationEvent, Processed)
                      VALUES (@Id, @OccurredAt, @Type, @Data, @IsIntegrationEvent, @Processed)",
                outbox,
                transaction);
        }
        entity.ClearDomainEvents();

        return PrimitiveResult.Success();
    }

    protected async ValueTask<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(this._connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
    protected async Task<TResult> WithConnectionAsync<TResult>(Func<SqlConnection, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        var connection = await this._currentTransactionScope.GetCurrentConnection(cancellationToken);
        return await action(connection);
    }

    protected static string QuoteName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[]";

        ReadOnlySpan<char> span = input.AsSpan().Trim();

        var brackets = SearchValues.Create(['[', ']']);

        if (!span.IsEmpty && brackets.Contains(span[0]))
            span = span[1..];

        if (!span.IsEmpty && brackets.Contains(span[^1]))
            span = span[..^1];

        return $"[{span}]";
    }
}
