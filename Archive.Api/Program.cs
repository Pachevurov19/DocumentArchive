using Archive.Api.Application;
using Archive.Api.Contracts;
using Archive.Api.Endpoints;
using Archive.Api.Infrastructure;
using Archive.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ArchiveDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString(
            "ArchiveDatabase"));
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<CabinetService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddSingleton<UploadGroupingService>();
builder.Services.AddScoped<DocumentQueryService>();
builder.Services.AddScoped<DocumentIndexService>();
builder.Services.AddScoped<DocumentFileService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ArchiveDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.MapCabinetEndpoints();
app.MapDocumentEndpoints();
app.MapDocumentIndexEndpoints();
app.MapUploadEndpoints();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
