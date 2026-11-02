
using Archive.Api.Contracts;
using Archive.Api.Domain;
using Archive.Api.Infrastructure;
using Archive.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Application;

public sealed class UploadService
{
    private readonly ArchiveDbContext _dbContext;
    private readonly CabinetService _cabinetService;
    private readonly FileStorageService _fileStorage;
    private readonly UploadGroupingService _groupingService;

    public UploadService(
    ArchiveDbContext dbContext,
    CabinetService cabinetService,
    FileStorageService fileStorage,
    UploadGroupingService groupingService)
    {
        _dbContext = dbContext;
        _cabinetService = cabinetService;
        _fileStorage = fileStorage;
        _groupingService = groupingService;
    }

    public async Task<Guid> UploadFileAsync(
        string cabinetName,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var cabinet = _cabinetService.GetByName(cabinetName);

        if (cabinet is null)
        {
            throw new ArgumentException(
                $"Cabinet '{cabinetName}' does not exist.");
        }

        var fileName = Path.GetFileName(
            file.FileName.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName is "." or "..")
        {
            throw new ArgumentException(
                "Invalid file name.");
        }

        string? storageKey = null;

        try
        {
            await using var stream = file.OpenReadStream();

            storageKey = await _fileStorage.SaveAsync(
                stream,
                cancellationToken);

            var document = new Document
            {
                CabinetName = cabinet.Name,
                Name = fileName,
                StoredAt = DateTimeOffset.UtcNow
            };

            document.Parts.Add(new DocumentPart
            {
                FileName = fileName,
                RelativePath = fileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            });

            _dbContext.Documents.Add(document);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return document.Id;
        }
        catch
        {
            if (storageKey is not null)
            {
                _fileStorage.Delete(storageKey);
            }

            throw;
        }
    }



    public async Task<UploadReport> UploadMultipleAsync(
        string cabinetName,
        UploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var cabinet = _cabinetService.GetByName(cabinetName)
            ?? throw new ArgumentException(
                $"Cabinet '{cabinetName}' does not exist.");

        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FolderName))
        {
            throw new ArgumentException(
                "Folder name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IndexFieldName))
        {
            throw new ArgumentException(
                "Index field name is required.");
        }

        var indexField = cabinet.Fields.FirstOrDefault(field =>
            string.Equals(
                field.Name,
                request.IndexFieldName,
                StringComparison.OrdinalIgnoreCase));

        if (indexField is null)
        {
            throw new ArgumentException(
                $"Index field '{request.IndexFieldName}' does not exist.");
        }

        if (!indexField.Writable ||
            !string.Equals(
                indexField.Type,
                "text",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Selected index field must be a writable text field.");
        }

        if (indexField.MaxLength.HasValue &&
            request.FolderName.Length > indexField.MaxLength.Value)
        {
            throw new ArgumentException(
                $"Folder name exceeds the maximum length " +
                $"of {indexField.MaxLength.Value} characters.");
        }

        var groups = _groupingService.Group(request);
        var skippedPaths = _groupingService.GetSkippedPaths(request);

        var report = new UploadReport();

        foreach (var skippedPath in skippedPaths)
        {
            report.Skipped.Add(new UploadItemResult
            {
                Name = skippedPath,
                Reason = "File skipped because it is nested deeper than one subfolder."
            });
        }

        foreach (var group in groups)
        {
            var savedKeys = new List<string>();

            try
            {
                var document = new Document
                {
                    CabinetName = cabinet.Name,
                    Name = group.Name,
                    StoredAt = DateTimeOffset.UtcNow
                };

                document.IndexValues.Add(new DocumentIndexValue
                {
                    FieldName = indexField.Name,
                    TextValue = request.FolderName
                });

                foreach (var input in group.Files)
                {
                    var path = input.RelativePath.Replace('\\', '/');
                    var segments = path.Split('/');

                    var relativePartPath = segments.Length == 1
                        ? segments[0]
                        : string.Join("/", segments.Skip(1));

                    await using var stream = input.File.OpenReadStream();

                    var storageKey = await _fileStorage.SaveAsync(
                        stream,
                        cancellationToken);

                    savedKeys.Add(storageKey);

                    document.Parts.Add(new DocumentPart
                    {
                        FileName = segments[^1],
                        RelativePath = relativePartPath,
                        StorageKey = storageKey,
                        ContentType = input.File.ContentType,
                        SizeBytes = input.File.Length
                    });
                }

                _dbContext.Documents.Add(document);

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                report.Stored.Add(new UploadItemResult
                {
                    Name = group.Name,
                    DocumentId = document.Id
                });
            }
            catch (OperationCanceledException)
            {
                foreach (var key in savedKeys)
                    _fileStorage.Delete(key);

                throw;
            }
            catch (Exception)
            {
                _dbContext.ChangeTracker.Clear();

                foreach (var key in savedKeys)
                    _fileStorage.Delete(key);

                report.Failed.Add(new UploadItemResult
                {
                    Name = group.Name,
                    Reason = "Document could not be stored."
                });
            }
        }

        return report;
    }


}
