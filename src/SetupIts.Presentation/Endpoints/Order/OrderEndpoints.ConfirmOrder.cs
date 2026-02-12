using MediatR;
using SetupIts.Application.Orders.Confirm;
using SetupIts.Presentation.AppCore;

namespace SetupIts.Presentation.Endpoints;

public static partial class OrderEndpoints
{
    static void AddConfimOrderEndpoint()
    {
        _routeHandlerBuilders.Add((app) =>
        {
            var builder = app.MapPut("{OrderId}/confirm",
                async (
                    string orderId,
                    ISender sender,
                    IResultHandler resultHandler,
                    CancellationToken cancellationToken) =>
                    {
                        var result = await sender.Send(
                        new ConfirmOrderCommand() { Id = orderId },
                        cancellationToken).ConfigureAwait(false);

                        return resultHandler.Handle(result, v => Results.NoContent());

                    });
            return builder;
        });
    }
}
