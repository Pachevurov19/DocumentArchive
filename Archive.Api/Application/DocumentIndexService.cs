
using System.Globalization;
using System.Text.Json;
using Archive.Api.Contracts;
using Archive.Api.Domain;
using Archive.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Application;

public sealed class DocumentIndexService
{
    private readonly ArchiveDbContext _dbContext;
    private readonly CabinetService _cabinetService;

    public DocumentIndexService(
        ArchiveDbContext dbContext,
        CabinetService cabinetService)
    {
        _dbContext = dbContext;
        _cabinetService = cabinetService;
    }

    public async Task<bool> UpdateAsync(
        Guid documentId,
        UpdateDocumentIndexesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Values is null)
            throw new ArgumentException("Values are required.");

        var document = await _dbContext.Documents
            .Include(d => d.IndexValues)
            .FirstOrDefaultAsync(
                d => d.Id == documentId,
                cancellationToken);

        if (document is null)
            return false;

        var cabinet = _cabinetService.GetByName(
            document.CabinetName)
            ?? throw new InvalidOperationException(
                "Document cabinet configuration is missing.");

        // Validate all values before modifying the document.
        var updates = new List<DocumentIndexValue>();

        foreach (var (fieldName, jsonValue) in request.Values)
        {
            var definition = cabinet.Fields.FirstOrDefault(
                f => f.Name == fieldName);

            if (definition is null)
                throw new ArgumentException(
                    $"Unknown index field: {fieldName}");

            if (!definition.Writable)
                throw new ArgumentException(
                    $"Index field '{fieldName}' is read-only.");

            var value = new DocumentIndexValue
            {
                FieldName = fieldName
            };

            switch (definition.Type)
            {
                case "text":
                    if (jsonValue.ValueKind != JsonValueKind.String)
                        throw new ArgumentException(
                            $"Field '{fieldName}' must be text.");

                    value.TextValue = jsonValue.GetString();

                    if (definition.MaxLength is int maxLength &&
                        value.TextValue!.Length > maxLength)
                        throw new ArgumentException(
                            $"Field '{fieldName}' exceeds {maxLength} characters.");

                    break;

                case "number":
                    if (jsonValue.ValueKind != JsonValueKind.Number ||
                        !jsonValue.TryGetDecimal(out var number))
                        throw new ArgumentException(
                            $"Field '{fieldName}' must be a number.");

                    value.NumberValue = number;
                    break;

                case "date":
                    if (jsonValue.ValueKind != JsonValueKind.String ||
                        !DateOnly.TryParseExact(
                            jsonValue.GetString(),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var date))
                        throw new ArgumentException(
                            $"Field '{fieldName}' must be a date in yyyy-MM-dd format.");

                    value.DateValue = date;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported index field type: {definition.Type}");
            }

            updates.Add(value);
        }

        foreach (var update in updates)
        {
            var existing = document.IndexValues.FirstOrDefault(
                v => v.FieldName == update.FieldName);

            if (existing is null)
            {
                document.IndexValues.Add(update);
            }
            else
            {
                existing.TextValue = update.TextValue;
                existing.NumberValue = update.NumberValue;
                existing.DateValue = update.DateValue;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
