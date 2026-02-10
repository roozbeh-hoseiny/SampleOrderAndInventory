using Dapper;
using System.Data;

namespace SetupIts.Infrastructure;

public struct DapperCommandDefinitionBuilder
{
    private string _commandText = string.Empty;
    private CommandType _commandType = CommandType.StoredProcedure;
    private DynamicParameters? _parameters = null;
    private CancellationToken? _cancellationToken = default;
    private CommandFlags _commandFlags = CommandFlags.None;
    private IDbTransaction? _dbTransaction = null;

    public DapperCommandDefinitionBuilder() { }

    public DapperCommandDefinitionBuilder SetProcedureName(string procedureName)
    {
        this._commandText = procedureName;
        this._commandType = CommandType.StoredProcedure;

        return this;
    }
    public DapperCommandDefinitionBuilder SetQueryText(string query)
    {
        this._commandText = query;
        this._commandType = CommandType.Text;

        return this;
    }
    public DapperCommandDefinitionBuilder SetParameter<T>(string name, T? value, DbType? dbType = null)
    {
        this._parameters ??= new DynamicParameters();
        if (dbType.HasValue)
            this._parameters.Add(name, value, dbType);
        else
            this._parameters.Add(name, value);

        return this;
    }
    public DapperCommandDefinitionBuilder SetParameterIfNotNull<T>(string name, T? value, DbType? dbType = null)
    {
        if (value is null) return this;

        this._parameters ??= new DynamicParameters();
        if (dbType.HasValue)
            this._parameters.Add(name, value, dbType);
        else
            this._parameters.Add(name, value);

        return this;
    }
    public DapperCommandDefinitionBuilder WithCancellationToken(CancellationToken? cancellationToken)
    {
        this._cancellationToken = cancellationToken;

        return this;
    }
    public DapperCommandDefinitionBuilder SetCommandFlags(CommandFlags commandFlags)
    {
        this._commandFlags = commandFlags;

        return this;
    }
    public DapperCommandDefinitionBuilder SetBuffered()
    {
        this._commandFlags = CommandFlags.Buffered;
        return this;
    }
    public DapperCommandDefinitionBuilder SetPipelined()
    {
        this._commandFlags = CommandFlags.Pipelined;
        return this;
    }
    public DapperCommandDefinitionBuilder SetNoCache()
    {
        this._commandFlags = CommandFlags.NoCache;
        return this;
    }
    public DapperCommandDefinitionBuilder SetTransaction(IDbTransaction dbTransaction)
    {
        this._dbTransaction = dbTransaction;
        return this;
    }

    public static DapperCommandDefinitionBuilder Procedure(string procedureName) =>
        new DapperCommandDefinitionBuilder().SetProcedureName(procedureName);
    public static DapperCommandDefinitionBuilder StreamedProcedure(string procedureName) =>
        new DapperCommandDefinitionBuilder().SetProcedureName(procedureName).SetPipelined();
    public static DapperCommandDefinitionBuilder Query(string query) =>
        new DapperCommandDefinitionBuilder().SetQueryText(query);

    public readonly CommandDefinition Build()
    {
        DynamicParameters? parameters = null;

        if (this._parameters != null)
        {
            parameters = this._parameters;
        }

        return new CommandDefinition(
                commandText: this._commandText,
                parameters: parameters,
                commandType: this._commandType,
                flags: this._commandFlags,
                transaction: this._dbTransaction,
                cancellationToken: this._cancellationToken ?? CancellationToken.None);
    }
    public CommandDefinition Build(CancellationToken cancellationToken) =>
        this.WithCancellationToken(cancellationToken).Build();

}
