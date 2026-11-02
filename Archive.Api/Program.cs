using Archive.Api.Application;
using Archive.Api.Contracts;
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

app.MapGet("/api/cabinets", (CabinetService service) =>
{
    return Results.Ok(service.GetAll());
});

app.MapGet("/api/cabinets/{name}", (
    string name,
    CabinetService service) =>
{
    var cabinet = service.GetByName(name);

    return cabinet is null
        ? Results.NotFound()
        : Results.Ok(cabinet);
});

app.MapGet("/api/documents/{id:guid}",
    async (
        Guid id,
        DocumentQueryService queryService,
        CancellationToken cancellationToken) =>
    {
        var document = await queryService.GetByIdAsync(
            id,
            cancellationToken);

        return document is null
            ? Results.NotFound()
            : Results.Ok(document);
    });

app.MapGet("/api/cabinets/{cabinetName}/documents",
    async (
        string cabinetName,
        DateOnly? from,
        DateOnly? to,
        int? page,
        int? pageSize,
        CabinetService cabinetService,
        DocumentQueryService queryService,
        CancellationToken cancellationToken) =>
    {
        if (cabinetService.GetByName(cabinetName) is null)
        {
            return Results.NotFound(new
            {
                Error = "Cabinet not found."
            });
        }

        try
        {
            var result = await queryService.SearchAsync(
                cabinetName,
                from,
                to,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken);

            return Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                Error = ex.Message
            });
        }
    });

app.MapGet("/api/documents/{documentId:guid}/parts/{partId:guid}/download",
    async (
        Guid documentId,
        Guid partId,
        DocumentFileService fileService,
        CancellationToken cancellationToken) =>
    {
        var file = await fileService.GetPartAsync(
            documentId,
            partId,
            cancellationToken);

        if (file is null)
            return Results.NotFound(new
            {
                Error = "Document part not found."
            });

        return Results.File(
            file.Content,
            contentType: file.ContentType,
            fileDownloadName: file.FileName,
            enableRangeProcessing: true);
    });

app.MapPatch("/api/documents/{id:guid}/indexes",
    async (
        Guid id,
        UpdateDocumentIndexesRequest request,
        DocumentIndexService indexService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var updated = await indexService.UpdateAsync(
                id,
                request,
                cancellationToken);

            return updated
                ? Results.NoContent()
                : Results.NotFound(new
                {
                    Error = "Document not found."
                });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                Error = ex.Message
            });
        }
    });

app.MapPost("/api/cabinets/{cabinetName}/documents/upload",
    async (
        string cabinetName,
        IFormFile file,
        UploadService uploadService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var documentId = await uploadService.UploadFileAsync(
                cabinetName,
                file,
                cancellationToken);

            return Results.Created(
                $"/api/documents/{documentId}",
                new { Id = documentId });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                Error = ex.Message
            });
        }
    })
    .DisableAntiforgery();

app.MapPost("/api/cabinets/{cabinetName}/documents/upload-batch",
    async (
        string cabinetName,
        HttpRequest httpRequest,
        UploadService uploadService,
        CancellationToken cancellationToken) =>
    {
        if (!httpRequest.HasFormContentType)
        {
            return Results.BadRequest(new
            {
                Error = "Expected multipart/form-data."
            });
        }

        var form = await httpRequest.ReadFormAsync(
            cancellationToken);

        var files = form.Files.GetFiles("files");
        var paths = form["paths"];

        var folderName = form["folderName"].ToString();
        var indexFieldName = form["indexFieldName"].ToString();

        if (string.IsNullOrWhiteSpace(folderName))
        {
            return Results.BadRequest(new
            {
                Error = "Folder name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(indexFieldName))
        {
            return Results.BadRequest(new
            {
                Error = "Index field name is required."
            });
        }

        if (files.Count == 0 || files.Count != paths.Count)
        {
            return Results.BadRequest(new
            {
                Error = "Files and paths must have matching counts."
            });
        }

        var request = new UploadRequest
        {
            FolderName = folderName,
            IndexFieldName = indexFieldName
        };

        for (var i = 0; i < files.Count; i++)
        {
            request.Files.Add(new UploadFileInput
            {
                File = files[i],
                RelativePath = paths[i]!
            });
        }

        try
        {
            var report = await uploadService.UploadMultipleAsync(
                cabinetName,
                request,
                cancellationToken);

            return Results.Ok(report);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new
            {
                Error = ex.Message
            });
        }
    })
    .DisableAntiforgery();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
