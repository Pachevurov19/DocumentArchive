
using Archive.Api.Application;
using Archive.Api.Contracts;

namespace Archive.Api.Endpoints;

public static class DocumentIndexEndpoints
{
    public static IEndpointRouteBuilder MapDocumentIndexEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Document Indexes");

        group.MapPatch("/{id:guid}/indexes",
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

        return app;
    }
}
