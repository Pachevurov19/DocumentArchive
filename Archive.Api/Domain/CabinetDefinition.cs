namespace Archive.Api.Domain
{
    public sealed class CabinetDefinition
    {
        public string Name { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public List<CabinetFieldDefinition> Fields { get; set; } = [];
    }

    public sealed class CabinetFieldDefinition
    {
        public string Name { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public int? MaxLength { get; set; }

        public bool Writable { get; set; }
    }
}
