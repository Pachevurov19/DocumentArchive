
using Archive.Api.Application;
using Archive.Api.Contracts;

namespace Archive.Api.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cabinets/{cabinetName}/documents")
            .WithTags("Uploads");

        group.MapPost("/upload",
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

        group.MapPost("/upload-batch",
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

        return app;
    }
}
