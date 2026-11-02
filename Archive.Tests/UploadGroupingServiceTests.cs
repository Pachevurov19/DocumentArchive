
using Archive.Api.Application;
using Archive.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Archive.Tests;

public sealed class UploadGroupingServiceTests
{
    private readonly UploadGroupingService _service = new();

    [Fact]
    public void Group_RootFiles_CreatesSeparateDocuments()
    {
        var request = CreateRequest(
            "invoice.pdf",
            "contract.pdf");

        var result = _service.Group(request);

        Assert.Equal(2, result.Count);

        Assert.Contains(result, group =>
            group.Name == "invoice.pdf" &&
            group.Files.Count == 1);

        Assert.Contains(result, group =>
            group.Name == "contract.pdf" &&
            group.Files.Count == 1);
    }

    [Fact]
    public void Group_FilesInSameSubfolder_CreatesOneDocument()
    {
        var request = CreateRequest(
            "Contracts/contract.pdf",
            "Contracts/appendix.pdf");

        var result = _service.Group(request);

        var group = Assert.Single(result);

        Assert.Equal("Contracts", group.Name);
        Assert.Equal(2, group.Files.Count);
    }

    [Fact]
    public void Group_DeeplyNestedFiles_AreSkipped()
    {
        var request = CreateRequest(
            "Contracts/contract.pdf",
            "Contracts/Images/photo.png");

        var result = _service.Group(request);
        var skipped = _service.GetSkippedPaths(request);

        var group = Assert.Single(result);

        Assert.Equal("Contracts", group.Name);
        Assert.Single(group.Files);

        Assert.Equal(
            "Contracts/Images/photo.png",
            Assert.Single(skipped));
    }

    [Fact]
    public void Group_InvalidRelativePath_ThrowsArgumentException()
    {
        var request = CreateRequest(
            "../secret.txt");

        Assert.Throws<ArgumentException>(() =>
            _service.Group(request));
    }

    [Fact]
    public void Group_WindowsPathSeparators_AreSupported()
    {
        var request = CreateRequest(
            @"Contracts\contract.pdf");

        var result = _service.Group(request);

        var group = Assert.Single(result);

        Assert.Equal("Contracts", group.Name);
        Assert.Single(group.Files);
    }

    private static UploadRequest CreateRequest(
        params string[] paths)
    {
        var request = new UploadRequest();

        foreach (var path in paths)
        {
            request.Files.Add(new UploadFileInput
            {
                RelativePath = path,
                File = new FormFile(
                    Stream.Null,
                    0,
                    0,
                    "files",
                    Path.GetFileName(path))
            });
        }

        return request;
    }
}
