
using Archive.Api.Infrastructure;
using Archive.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Application;

public sealed class DocumentFileService
{
    private readonly ArchiveDbContext _dbContext;
    private readonly FileStorageService _fileStorage;

    public DocumentFileService(
        ArchiveDbContext dbContext,
        FileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _fileStorage = fileStorage;
    }

    public async Task<DocumentFileResult?> GetPartAsync(
        Guid documentId,
        Guid partId,
        CancellationToken cancellationToken = default)
    {
        var part = await _dbContext.DocumentParts
            .AsNoTracking()
            .Where(p =>
                p.Id == partId &&
                p.DocumentId == documentId)
            .Select(p => new
            {
                p.StorageKey,
                p.FileName,
                p.ContentType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (part is null)
            return null;

        var stream = _fileStorage.OpenRead(part.StorageKey);

        return new DocumentFileResult(
            stream,
            part.FileName,
            part.ContentType);
    }
}

public sealed record DocumentFileResult(
    Stream Content,
    string FileName,
    string ContentType);
