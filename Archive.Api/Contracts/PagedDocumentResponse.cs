
namespace Archive.Api.Contracts;

public sealed class PagedDocumentResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<DocumentResponse> Items { get; set; } = [];
}
