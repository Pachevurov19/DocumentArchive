using System.Text.Json;

namespace Archive.Api.Contracts;

public sealed class UpdateDocumentIndexesRequest
{
    public Dictionary<string, JsonElement> Values { get; set; }
        = new(StringComparer.Ordinal);
}
