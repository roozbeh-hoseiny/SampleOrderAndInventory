using MediatR;
using SetupIts.Application.Orders.Cancel;
using SetupIts.Presentation.AppCore;

namespace SetupIts.Presentation.Endpoints;

public static partial class OrderEndpoints
{
    static void AddCancelOrderEndpoint()
    {
        _routeHandlerBuilders.Add((app) =>
        {
            var builder = app.MapPost("{OrderId}/cancel",
                async (
                    string orderId,
                    ISender sender,
                    IResultHandler resultHandler,
                    CancellationToken cancellationToken) =>
                    {
                        var result = await sender.Send(
                        new CancelOrderCommand() { Id = orderId },
                        cancellationToken).ConfigureAwait(false);
                        return resultHandler.Handle(result, v => Results.NoContent());

                    });
            return builder;
        });
    }
}
