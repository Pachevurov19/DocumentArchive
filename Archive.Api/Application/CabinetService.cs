
using System.Text.Json;
using Archive.Api.Domain;

namespace Archive.Api.Application;

public sealed class CabinetService
{
    private readonly IReadOnlyList<CabinetDefinition> _cabinets;

    public CabinetService(IWebHostEnvironment environment)
    {
        var filePath = Path.Combine(
            environment.ContentRootPath,
            "cabinets.json");

        var json = File.ReadAllText(filePath);

        var configuration =
            JsonSerializer.Deserialize<CabinetConfiguration>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        _cabinets = configuration?.Cabinets
            ?? throw new InvalidOperationException(
                "Invalid cabinet configuration.");
    }

    public IReadOnlyList<CabinetDefinition> GetAll()
        => _cabinets;

    public CabinetDefinition? GetByName(string name)
        => _cabinets.FirstOrDefault(
            x => string.Equals(
                x.Name,
                name,
                StringComparison.OrdinalIgnoreCase));

    private sealed class CabinetConfiguration
    {
        public List<CabinetDefinition> Cabinets { get; set; } = [];
    }
}
