namespace BuildingsTiledFramework.Tiled;

public interface ITiledObject
{
    string? Get(string key);

    int GetInt(string key);

    bool Has(string key);

    int TileX { get; }

    int TileY { get; }

    int TileWidth { get; }

    int TileHeight { get; }
}

public interface ITiledObjectProvider
{
    ITiledObject Load(string filePath);
}
