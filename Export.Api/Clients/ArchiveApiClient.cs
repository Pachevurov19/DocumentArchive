
using System.Net.Http.Json;
using System.Text.Json;

namespace Export.Api.Clients;

public sealed class ArchiveApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public ArchiveApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedDocumentsResponse> GetDocumentsAsync(
        string cabinetName,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (from.HasValue)
            query.Add($"from={from.Value:yyyy-MM-dd}");

        if (to.HasValue)
            query.Add($"to={to.Value:yyyy-MM-dd}");

        var url =
            $"/api/cabinets/{Uri.EscapeDataString(cabinetName)}/documents?" +
            string.Join("&", query);

        using var response = await _httpClient.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedDocumentsResponse>(
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException(
                "Archive.Api returned an empty response.");
    }

    public async Task<Stream> DownloadPartAsync(
        Guid documentId,
        Guid partId,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"/api/documents/{documentId}/parts/{partId}/download";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // The returned stream must remain usable after this method returns.
        var content = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        var buffer = new MemoryStream();

        try
        {
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            return buffer;
        }
        catch
        {
            await buffer.DisposeAsync();
            throw;
        }
    }

    public async Task UpdateExportMarkerAsync(
        Guid documentId,
        string markerField,
        DateOnly exportDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(markerField))
        {
            throw new ArgumentException(
                "Export marker field is required.",
                nameof(markerField));
        }

        var markerValue =
            $"Exported on {exportDate:yyyy-MM-dd}";

        var request = new
        {
            Values = new Dictionary<string, string>
            {
                [markerField] = markerValue
            }
        };

        using var response = await _httpClient.PatchAsJsonAsync(
            $"/api/documents/{documentId}/indexes",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task<ArchiveCabinetResponse> GetCabinetAsync(
        string cabinetName,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"/api/cabinets/{Uri.EscapeDataString(cabinetName)}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ArchiveCabinetResponse>(
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Archive API returned an empty cabinet response.");
    }


}

public sealed class PagedDocumentsResponse
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public List<ArchiveDocumentResponse> Items { get; set; } = [];
}

public sealed class ArchiveDocumentResponse
{
    public Guid Id { get; set; }

    public string CabinetName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset StoredAt { get; set; }

    public List<ArchiveDocumentPartResponse> Parts { get; set; } = [];
}

public sealed class ArchiveDocumentPartResponse
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}

public sealed class ArchiveCabinetResponse
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<ArchiveCabinetFieldResponse> Fields { get; set; } = [];
}

public sealed class ArchiveCabinetFieldResponse
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int? MaxLength { get; set; }

    public bool Writable { get; set; }
}