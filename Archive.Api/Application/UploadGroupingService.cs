
using Archive.Api.Contracts;

namespace Archive.Api.Application;

public sealed record UploadDocumentGroup(
    string Name,
    IReadOnlyList<UploadFileInput> Files);

public sealed class UploadGroupingService
{
    public IReadOnlyList<UploadDocumentGroup> Group(
        UploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var groups = new Dictionary<string, List<UploadFileInput>>(
            StringComparer.Ordinal);

        foreach (var input in request.Files)
        {
            if (input.File is null)
            {
                throw new ArgumentException(
                    "Upload contains a missing file.");
            }

            var path = input.RelativePath.Replace('\\', '/');

            var segments = path.Split('/');

            // Validate the relative path.
            if (segments.Length == 0 ||
                segments.Any(segment =>
                    string.IsNullOrWhiteSpace(segment) ||
                    segment is "." or ".." ||
                    segment.Contains(':') ||
                    segment.Any(char.IsControl)) ||
                path.StartsWith('/'))
            {
                throw new ArgumentException(
                    $"Invalid relative path: {path}");
            }

            // Ignore files located deeper than one subfolder.
            // Examples:
            // invoice.pdf                  -> accepted
            // Contracts/contract.pdf       -> accepted
            // Contracts/Images/photo.png   -> skipped
            if (segments.Length > 2)
            {
                continue;
            }

            var documentName = segments[0];

            if (!groups.TryGetValue(
                documentName,
                out var files))
            {
                files = [];
                groups.Add(documentName, files);
            }

            files.Add(input);
        }

        return groups.Select(group =>
            new UploadDocumentGroup(
                group.Key,
                group.Value)).ToList();
    }


    public IReadOnlyList<string> GetSkippedPaths(
        UploadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var skippedPaths = new List<string>();

        foreach (var input in request.Files)
        {
            if (input.File is null)
            {
                continue;
            }

            var path = input.RelativePath.Replace('\\', '/');
            var segments = path.Split('/');

            if (segments.Length > 2)
            {
                skippedPaths.Add(path);
            }
        }

        return skippedPaths;
    }


}
