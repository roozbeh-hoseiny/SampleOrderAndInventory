using MediatR;
using SetupIts.Presentation.AppCore;
using SetupIts.Presentation.Endpoints.Order.Contracts;

namespace SetupIts.Presentation.Endpoints;

public static partial class OrderEndpoints
{
    static void AddCreateOrderEndpoint()
    {
        _routeHandlerBuilders.Add((app) =>
        {
            var builder = app.MapPost(string.Empty,
                async (
                    CreateOrderApiRequest request,
                    ISender sender,
                    IResultHandler resultHandler,
                    CancellationToken cancellationToken) =>
                    {
                        var result = await sender.Send(
                        CreateOrderApiRequest.Map(request),
                        cancellationToken).ConfigureAwait(false);

                        return resultHandler.Handle(result, v => Results.Created("", v.OrderId));

                    });
            return builder;
        });
    }
}
