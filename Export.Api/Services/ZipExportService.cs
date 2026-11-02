
using System.IO.Compression;
using Export.Api.Clients;

namespace Export.Api.Services;

public sealed class ZipExportService
{
    private readonly ArchiveApiClient _archiveClient;

    public ZipExportService(ArchiveApiClient archiveClient)
    {
        _archiveClient = archiveClient;
    }

    public async Task<byte[]> ExportAsync(
        string cabinetName,
        DateOnly? from,
        DateOnly? to,
        string markerField,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cabinetName))
            throw new ArgumentException("Cabinet name is required.");

        var cabinet = await _archiveClient.GetCabinetAsync(
                cabinetName,
                cancellationToken);

        var markerDefinition = cabinet.Fields.FirstOrDefault(f =>
                string.Equals(
                f.Name,
                markerField,
                StringComparison.OrdinalIgnoreCase));

        if (markerDefinition is null ||
            !markerDefinition.Writable ||
            !string.Equals(
                markerDefinition.Type,
                "text",
                StringComparison.OrdinalIgnoreCase) ||
                markerDefinition.MaxLength is < 22)
        {
            throw new ArgumentException(
                "Marker field must be a writable text field " +
                "with a maximum length of at least 22 characters.");
        }

        markerField = markerDefinition.Name;

        var documents = new List<ArchiveDocumentResponse>();

        const int pageSize = 100;
        var page = 1;

        while (true)
        {
            var result = await _archiveClient.GetDocumentsAsync(
                cabinetName,
                from,
                to,
                page,
                pageSize,
                cancellationToken);

            documents.AddRange(result.Items);

            if (result.Items.Count == 0 ||
                documents.Count >= result.TotalCount)
            {
                break;
            }

            page++;
        }

        using var output = new MemoryStream();

        using (var archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            foreach (var document in documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Use the ID to avoid collisions between documents
                // that have the same name.

                var documentFolder = SanitizeSegment(document.Name);

                var sameNameCount = documents.Count(d =>
                    string.Equals(
                        SanitizeSegment(d.Name),
                        documentFolder,
                        StringComparison.OrdinalIgnoreCase));

                if (sameNameCount > 1)
                {
                    var extension = document.Parts.Count == 1
                        ? Path.GetExtension(documentFolder)
                        : string.Empty;

                    var nameWithoutExtension =
                        extension.Length > 0
                            ? documentFolder[..^extension.Length]
                            : documentFolder;

                    documentFolder =
                        $"{nameWithoutExtension}_{document.Id:N}{extension}";
                }



                if (document.Parts.Count == 0)
                {
                    archive.CreateEntry(documentFolder + "/");
                    continue;
                }

                if (document.Parts.Count == 1)
                {
                    var part = document.Parts[0];

                    var entry = archive.CreateEntry(
                        documentFolder,
                        CompressionLevel.Optimal);

                    await using var source =
                        await _archiveClient.DownloadPartAsync(
                            document.Id,
                            part.Id,
                            cancellationToken);

                    await using var destination = entry.Open();

                    await source.CopyToAsync(
                        destination,
                        cancellationToken);

                    continue;
                }


                foreach (var part in document.Parts)
                {
                    var relativePath = NormalizeRelativePath(
                        part.RelativePath,
                        part.FileName);

                    var entryPath =
                        $"{documentFolder}/{relativePath}";

                    var entry = archive.CreateEntry(
                        entryPath,
                        CompressionLevel.Optimal);

                    await using var source =
                        await _archiveClient.DownloadPartAsync(
                            document.Id,
                            part.Id,
                            cancellationToken);

                    await using var destination = entry.Open();

                    await source.CopyToAsync(
                        destination,
                        cancellationToken);
                }
            }
        }

        var zipBytes = output.ToArray();

        var exportDate = DateOnly.FromDateTime(
            DateTime.UtcNow);

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _archiveClient.UpdateExportMarkerAsync(
                document.Id,
                markerField,
                exportDate,
                cancellationToken);
        }

        return zipBytes;
    }

    private static string NormalizeRelativePath(
        string relativePath,
        string fileName)
    {
        var path = string.IsNullOrWhiteSpace(relativePath)
            ? fileName
            : relativePath;

        path = path.Replace('\\', '/');

        var segments = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            throw new InvalidOperationException(
                "Document part has an invalid path.");

        if (path.StartsWith('/') ||
            segments.Any(s =>
                s is "." or ".." ||
                s.Contains(':') ||
                s.Any(char.IsControl)))
        {
            throw new InvalidOperationException(
                "Document part contains an unsafe path.");
        }

        return string.Join(
            "/",
            segments.Select(SanitizeSegment));
    }

    private static string SanitizeSegment(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();

        var sanitized = new string(
            value.Select(c =>
                invalidCharacters.Contains(c) ||
                c is '/' or '\\' or ':'
                    ? '_'
                    : c).ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? "unnamed"
            : sanitized;
    }
}
