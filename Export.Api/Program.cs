using Export.Api.Clients;
using Export.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var archiveApiBaseUrl =
    builder.Configuration["ArchiveApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "ArchiveApi:BaseUrl is not configured.");

builder.Services.AddHttpClient("ArchiveApi", client =>
{
    client.BaseAddress = new Uri(archiveApiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<ArchiveApiClient>(client =>
{
    client.BaseAddress = new Uri(archiveApiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<ZipExportService>();

var app = builder.Build();


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
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.Json(
                new { Error = "Archive.Api request timed out." },
                statusCode: StatusCodes.Status504GatewayTimeout);
        }
    });

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
        catch (HttpRequestException ex)
        {
            return Results.Problem(
                title: "Archive API request failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
