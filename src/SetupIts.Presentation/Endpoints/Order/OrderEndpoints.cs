namespace SetupIts.Presentation.Endpoints;

public static partial class OrderEndpoints
{
    static List<Func<IEndpointRouteBuilder, RouteHandlerBuilder>> _routeHandlerBuilders = new();
    static OrderEndpoints()
    {
        AddCreateOrderEndpoint();
        AddConfimOrderEndpoint();
        AddCancelOrderEndpoint();
        AddGetOneEndpoint();
    }
    public static void AddAllEndpoitnts(WebApplication app, RouteGroupBuilder? group = null)
    {
        IEndpointRouteBuilder routeBuilder = group ?? (IEndpointRouteBuilder)app;

        foreach (var route in _routeHandlerBuilders)
        {
            route.Invoke(routeBuilder);
        }
    }
}