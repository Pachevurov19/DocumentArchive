namespace Archive.Api.Domain
{
    public sealed class DocumentPart
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public string StorageKey { get; set; } = string.Empty;

        public string ContentType { get; set; }
            = "application/octet-stream";

        public long SizeBytes { get; set; }
    }
}
