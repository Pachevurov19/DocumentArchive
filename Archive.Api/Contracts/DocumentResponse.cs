
namespace Archive.Api.Contracts;

public sealed class DocumentResponse
{
    public Guid Id { get; set; }

    public string CabinetName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset StoredAt { get; set; }

    public List<DocumentPartResponse> Parts { get; set; } = [];

    public List<DocumentIndexValueResponse> IndexValues { get; set; } = [];
}

public sealed class DocumentPartResponse
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}

public sealed class DocumentIndexValueResponse
{
    public string FieldName { get; set; } = string.Empty;

    public string? TextValue { get; set; }

    public decimal? NumberValue { get; set; }

    public DateOnly? DateValue { get; set; }
}
