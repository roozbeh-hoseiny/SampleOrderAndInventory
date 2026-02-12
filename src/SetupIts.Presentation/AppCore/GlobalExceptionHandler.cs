using Microsoft.AspNetCore.Diagnostics;
using SetupIts.Shared;
using SetupIts.Shared.Primitives;

namespace SetupIts.Presentation.AppCore;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    readonly static PrimitiveError BadRequestError = PrimitiveError.Create(DomainErrorCodes.BadRequest_ErrorCode, "Bad request");

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IResultHandler _resultHandler;

    public GlobalExceptionHandler(
        IServiceScopeFactory serviceScopeFactory,
        IResultHandler resultHandler,
        ILogger<GlobalExceptionHandler> logger)
    {
        this._serviceScopeFactory = serviceScopeFactory;
        this._resultHandler = resultHandler;
        this._logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = this._serviceScopeFactory.CreateScope();
        var resultHandler = scope.ServiceProvider.GetRequiredService<IResultHandler>();

        PrimitiveError error;
        PrimitiveResult<bool> result;

        if (exception is BadHttpRequestException)
        {
            error = BadRequestError;
            result = PrimitiveResult.Failure<bool>(error);
        }
        else
        {
            error = PrimitiveError.Create(DomainErrorCodes.UnhandledException_ErrorCode, exception.Message);
            result = PrimitiveResult.InternalFailure<bool>(error);
        }

        var handlerResult = this._resultHandler.Handle(result);

        await handlerResult.ExecuteAsync(httpContext).ConfigureAwait(false);

        return true;
    }
}