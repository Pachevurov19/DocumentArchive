
using Export.Api.Services;

namespace Export.Api.Endpoints;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/export/{cabinetName}",
            async (
                string cabinetName,
                DateOnly? from,
                DateOnly? to,
                string? markerField,
                ZipExportService exportService,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(cabinetName))
                {
                    return Results.BadRequest(new
                    {
                        Error = "Cabinet name is required."
                    });
                }

                if (from.HasValue && to.HasValue && from > to)
                {
                    return Results.BadRequest(new
                    {
                        Error = "From date must not be after To date."
                    });
                }

                try
                {
                    var zipBytes = await exportService.ExportAsync(
                        cabinetName,
                        from,
                        to,
                        markerField ?? string.Empty,
                        cancellationToken);

                    var fileName =
                        $"{cabinetName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

                    return Results.File(
                        zipBytes,
                        contentType: "application/zip",
                        fileDownloadName: fileName);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        Error = ex.Message
                    });
                }
                catch (HttpRequestException ex)
                {
                    return Results.Problem(
                        title: "Archive API request failed",
                        detail: ex.Message,
                        statusCode: StatusCodes.Status502BadGateway);
                }
            })
            .WithTags("Exports");

        return app;
    }
}
