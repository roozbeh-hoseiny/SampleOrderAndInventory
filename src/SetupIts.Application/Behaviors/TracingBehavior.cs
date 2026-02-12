namespace SetupIts.Application.Behaviors;
using MediatR;
using SetupIts.Hosting;
using SetupIts.Shared.Primitives;
using System.Diagnostics;


public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new(ActivitySources.MediatR);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (Activity.Current is null)
            return await next();

        using var activity = ActivitySource.StartActivity(
            $"Command {typeof(TRequest).Name}",
            ActivityKind.Internal);

        activity?.SetTag("mediatr.command", typeof(TRequest).Name);

        try
        {
            var response = await next();

            // Check if it's a PrimitiveResult and mark failures
            if (response is IPrimitiveResult result && result.IsFailure)
            {
                activity?.SetTag("result.success", false);
                activity?.SetTag("error.code", result.Error.Code);
                activity?.SetTag("error.message", result.Error.Message);
                activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            }
            else
            {
                activity?.SetTag("result.success", true);
            }

            return response;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex, timestamp: DateTimeOffset.Now);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}

public sealed class ExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(PrimitiveResult<>))
            {
                return CreateFailureResponse(ex);
            }
            throw;
        }
    }

    private static TResponse CreateFailureResponse(Exception ex)
    {
        return (TResponse)typeof(PrimitiveResult<>)
            .MakeGenericType(typeof(TResponse).GenericTypeArguments[0])
            .GetMethod(nameof(PrimitiveResult<TResponse>.Failure))!
            .Invoke(null, new object[] { PrimitiveError.CreateInternal("Application.Error", ex.Message) })!;
    }
}
