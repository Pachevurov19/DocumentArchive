namespace Archive.Api.Contracts;

public sealed class UploadFileInput
{
    public IFormFile File { get; set; } = null!;

    public string RelativePath { get; set; } = string.Empty;
}
