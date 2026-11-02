using Archive.Api.Application;

namespace Archive.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(
        this IEndpointRouteBuilder app)
    {
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
            })
            .WithTags("Documents");

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
            })
            .WithTags("Documents");

        app.MapGet(
            "/api/documents/{documentId:guid}/parts/{partId:guid}/download",
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
                {
                    return Results.NotFound(new
                    {
                        Error = "Document part not found."
                    });
                }

                return Results.File(
                    file.Content,
                    contentType: file.ContentType,
                    fileDownloadName: file.FileName,
                    enableRangeProcessing: true);
            })
            .WithTags("Documents");

        return app;
    }
}
