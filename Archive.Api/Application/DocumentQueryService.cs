
using Archive.Api.Contracts;
using Archive.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Application;

public sealed class DocumentQueryService
{
    private readonly ArchiveDbContext _dbContext;

    public DocumentQueryService(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentResponse?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => new DocumentResponse
            {
                Id = d.Id,
                CabinetName = d.CabinetName,
                Name = d.Name,
                StoredAt = d.StoredAt,

                Parts = d.Parts
                    .Select(p => new DocumentPartResponse
                    {
                        Id = p.Id,
                        FileName = p.FileName,
                        RelativePath = p.RelativePath,
                        ContentType = p.ContentType,
                        SizeBytes = p.SizeBytes
                    })
                    .ToList(),

                IndexValues = d.IndexValues
                    .Select(v => new DocumentIndexValueResponse
                    {
                        FieldName = v.FieldName,
                        TextValue = v.TextValue,
                        NumberValue = v.NumberValue,
                        DateValue = v.DateValue
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }


    public async Task<PagedDocumentResponse> SearchAsync(
        string cabinetName,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException(
                "Page must be >= 1 and pageSize must be between 1 and 100.");
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException(
                "From date must not be after To date.");
        }

        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.CabinetName == cabinetName);

        if (from.HasValue)
        {
            var start = new DateTimeOffset(
                from.Value.ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);

            query = query.Where(d => d.StoredAt >= start);
        }

        if (to.HasValue)
        {
            var endExclusive = new DateTimeOffset(
                to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);

            query = query.Where(d => d.StoredAt < endExclusive);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var documents = await query
            .OrderBy(d => d.StoredAt)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentResponse
            {
                Id = d.Id,
                CabinetName = d.CabinetName,
                Name = d.Name,
                StoredAt = d.StoredAt,

                Parts = d.Parts.Select(p =>
                    new DocumentPartResponse
                    {
                        Id = p.Id,
                        FileName = p.FileName,
                        RelativePath = p.RelativePath,
                        ContentType = p.ContentType,
                        SizeBytes = p.SizeBytes
                    }).ToList(),

                IndexValues = d.IndexValues.Select(v =>
                    new DocumentIndexValueResponse
                    {
                        FieldName = v.FieldName,
                        TextValue = v.TextValue,
                        NumberValue = v.NumberValue,
                        DateValue = v.DateValue
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedDocumentResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = documents
        };
    }

}
