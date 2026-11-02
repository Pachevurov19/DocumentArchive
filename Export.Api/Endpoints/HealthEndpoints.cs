
namespace Export.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/export/archive-status",
            async (
                IHttpClientFactory httpClientFactory,
                CancellationToken cancellationToken) =>
            {
                var client = httpClientFactory.CreateClient("ArchiveApi");

                try
                {
                    using var response = await client.GetAsync(
                        "/api/cabinets",
                        cancellationToken);

                    return Results.Ok(new
                    {
                        ArchiveApiReachable = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode
                    });
                }
                catch (HttpRequestException)
                {
                    return Results.Json(
                        new { Error = "Archive.Api is unreachable." },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                catch (TaskCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    return Results.Json(
                        new { Error = "Archive.Api request timed out." },
                        statusCode: StatusCodes.Status504GatewayTimeout);
                }
            })
            .WithTags("Health");

        return app;
    }
}
