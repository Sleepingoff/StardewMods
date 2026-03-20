namespace BuildingsTiledFramework;

public sealed class ModConfig
{
    public Dictionary<string, string> SelectedContentPacks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
