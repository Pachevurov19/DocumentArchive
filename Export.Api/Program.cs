using Export.Api.Clients;
using Export.Api.Endpoints;
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

app.MapHealthEndpoints();
app.MapExportEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
