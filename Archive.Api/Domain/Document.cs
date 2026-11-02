namespace Archive.Api.Domain
{
    public sealed class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string CabinetName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset StoredAt { get; set; }
            = DateTimeOffset.UtcNow;

        public ICollection<DocumentPart> Parts { get; set; }
            = new List<DocumentPart>();

        public ICollection<DocumentIndexValue> IndexValues { get; set; }
            = new List<DocumentIndexValue>();
    }
}
