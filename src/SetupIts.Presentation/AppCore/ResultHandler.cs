using SetupIts.Shared;
using SetupIts.Shared.Primitives;
using System.Diagnostics;

namespace SetupIts.Presentation.AppCore;

public sealed class ResultHandler : IResultHandler
{
    private readonly ILogger<ResultHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ResultHandler(ILogger<ResultHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        this._logger = logger;
        this._httpContextAccessor = httpContextAccessor;
    }

    public IResult Handle<T>(PrimitiveResult<T> result) => this.Handle(result, rv => Results.Ok(rv));
    public IResult Handle<T>(PrimitiveResult<T> result, Func<T, IResult> func)
    {
        if (result.IsSuccess)
            return func.Invoke(result.Value);

        var httpContext = this._httpContextAccessor.HttpContext!;
        string traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        string path = httpContext.Request.Path;

        // Log all errors for support/dev team
        this._logger.LogError("Request failed at {Path}, TraceId: {TraceId}, Errors: {@Errors}", path, traceId, result.Errors);

        var statusCode = GetStatus(result.Error);
        var errorMessage = GetMessage(result.Errors);

        return Results.Json(
            new ApiErrorItem(result.Error.Code, errorMessage, null),
            statusCode: statusCode);
    }
    static string GetMessage(PrimitiveError[] errors)
    {
        var visibleErrors = errors.Where(e => !e.Internal).Select(e => e.Message).ToList();
        return
            visibleErrors.Any()
            ? string.Join(Environment.NewLine, visibleErrors)
            : "An error occurred";
    }
    static int GetStatus(PrimitiveError error)
    {
        var result = StatusCodes.Status500InternalServerError;

        if (error.Code.Equals(DomainErrorCodes.AccessDenied_ErrorCode, StringComparison.InvariantCultureIgnoreCase))
            return StatusCodes.Status401Unauthorized; //401

        if (error.Code.Equals(DomainErrorCodes.UnhandledException_ErrorCode, StringComparison.InvariantCultureIgnoreCase))
            return StatusCodes.Status500InternalServerError; //500


        if (error.Code.Equals(DomainErrorCodes.BadRequest_ErrorCode, StringComparison.InvariantCultureIgnoreCase))
            return StatusCodes.Status400BadRequest; //400

        return result;
    }
}
sealed record ApiErrorItem(string Code, string Message, string? Details);