using MediatR;
using SetupIts.Application.Orders.GetOne;
using SetupIts.Presentation.AppCore;

namespace SetupIts.Presentation.Endpoints;

public static partial class OrderEndpoints
{
    static void AddGetOneEndpoint()
    {
        _routeHandlerBuilders.Add((app) =>
        {
            var builder = app.MapGet("{OrderId}",
                async (
                    string orderId,
                    ISender sender,
                    IResultHandler resultHandler,
                    CancellationToken cancellationToken) =>
                    {
                        var result = await sender.Send(
                        new GetOneQuery() { Id = orderId },
                        cancellationToken).ConfigureAwait(false);
                        return resultHandler.Handle(result, v => Results.Ok(v));
                    });
            return builder;
        });
    }
}
