namespace Archive.Api.Domain;

public sealed class DocumentIndexValue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }

    public Document Document { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;

    public string? TextValue { get; set; }

    public decimal? NumberValue { get; set; }

    public DateOnly? DateValue { get; set; }
}
