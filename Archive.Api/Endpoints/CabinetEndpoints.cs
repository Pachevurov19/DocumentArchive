using Archive.Api.Application;

namespace Archive.Api.Endpoints;

public static class CabinetEndpoints
{
    public static IEndpointRouteBuilder MapCabinetEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cabinets")
            .WithTags("Cabinets");

        group.MapGet("/", (CabinetService service) =>
        {
            return Results.Ok(service.GetAll());
        });

        group.MapGet("/{name}", (
            string name,
            CabinetService service) =>
        {
            var cabinet = service.GetByName(name);

            return cabinet is null
                ? Results.NotFound()
                : Results.Ok(cabinet);
        });

        return app;
    }
}
