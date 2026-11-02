
namespace Archive.Api.Contracts;

public sealed class UploadReport
{
    public List<UploadItemResult> Stored { get; set; } = [];

    public List<UploadItemResult> Skipped { get; set; } = [];

    public List<UploadItemResult> Failed { get; set; } = [];
}

public sealed class UploadItemResult
{
    public string Name { get; set; } = string.Empty;

    public Guid? DocumentId { get; set; }

    public string? Reason { get; set; }
}
