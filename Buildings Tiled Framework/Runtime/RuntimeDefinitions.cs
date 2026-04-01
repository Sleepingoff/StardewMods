using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BuildingsTiledFramework.Runtime;

public sealed class RuntimeBuildingDefinition
{
    public const string BackDrawLayer = "Back";
    public const string BuildingsDrawLayer = "Buildings";
    public const string FrontDrawLayer = "Front";
    public const string AlwaysFrontDrawLayer = "AlwaysFront";

    private readonly Dictionary<string, RuntimeBuildingDefinition> seasonalVariants = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D> drawTextures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<RuntimeTileSprite>> tileSprites = new(StringComparer.OrdinalIgnoreCase);

    public string Id { get; set; } = string.Empty;

    public string SourcePackUniqueId { get; set; } = string.Empty;

    public string SourcePackName { get; set; } = string.Empty;

    public string SourceFilePath { get; set; } = string.Empty;

    public string? Inherits { get; set; }

    public Texture2D Texture { get; set; } = null!;

    public Rectangle? SourceRect { get; set; }

    public Point Size { get; set; }

    public Point FootprintOrigin { get; set; }

    public Vector2 Door { get; set; }

    public bool HasExplicitDoor { get; set; }

    public Point? Exit { get; set; }

    public Point? Chimney { get; set; }

    public Point? Mailbox { get; set; }

    public Rectangle? AnimalDoor { get; set; }

    public float? AnimalDoorOpenDuration { get; set; }

    public string? AnimalDoorOpenSound { get; set; }

    public float? AnimalDoorCloseDuration { get; set; }

    public string? AnimalDoorCloseSound { get; set; }

    public int? MaxOccupants { get; set; }

    public bool? AllowsFlooringUnderneath { get; set; }

    public bool DrawShadow { get; set; }

    public int? BuildCost { get; set; }

    public int? BuildDays { get; set; }

    public string? Builder { get; set; }

    public List<RuntimeBuildMaterial> BuildMaterials { get; } = new();

    public List<RuntimeDrawLayer> BuildingDrawLayers { get; } = new();

    public List<RuntimeAction> Actions { get; } = new();

    public HashSet<Point> CollisionTiles { get; } = new();

    public IReadOnlyDictionary<string, Texture2D> DrawTextures => this.drawTextures;
    public IReadOnlyDictionary<string, List<RuntimeTileSprite>> TileSprites => this.tileSprites;

    public IReadOnlyDictionary<string, RuntimeBuildingDefinition> SeasonalVariants => this.seasonalVariants;

    public bool HasSeasonalVariants => this.seasonalVariants.Count > 0;

    public void SetSeasonalVariant(string season, RuntimeBuildingDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(season))
            throw new ArgumentException("Season is required.", nameof(season));
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));

        this.seasonalVariants[season] = definition;
    }

    public RuntimeBuildingDefinition ResolveForSeason(string season)
    {
        if (this.seasonalVariants.TryGetValue(season, out var definition))
            return definition;

        return this;
    }

    public void SetDrawTexture(string drawLayer, Texture2D texture)
    {
        if (string.IsNullOrWhiteSpace(drawLayer))
            throw new ArgumentException("Draw layer is required.", nameof(drawLayer));
        if (texture is null)
            throw new ArgumentNullException(nameof(texture));

        this.drawTextures[drawLayer] = texture;
        if (string.Equals(drawLayer, BuildingsDrawLayer, StringComparison.OrdinalIgnoreCase) || this.Texture is null)
            this.Texture = texture;
    }

    public Texture2D? GetDrawTexture(string drawLayer)
    {
        if (this.drawTextures.TryGetValue(drawLayer, out var texture))
            return texture;

        return string.Equals(drawLayer, BuildingsDrawLayer, StringComparison.OrdinalIgnoreCase)
            ? this.Texture
            : null;
    }

    public void AddTileSprite(string drawLayer, RuntimeTileSprite tileSprite)
    {
        if (!this.tileSprites.TryGetValue(drawLayer, out var sprites))
        {
            sprites = new List<RuntimeTileSprite>();
            this.tileSprites[drawLayer] = sprites;
        }

        sprites.Add(tileSprite);
    }

    public IReadOnlyList<RuntimeTileSprite> GetTileSprites(string drawLayer)
    {
        return this.tileSprites.TryGetValue(drawLayer, out var sprites)
            ? sprites
            : Array.Empty<RuntimeTileSprite>();
    }
}

public sealed class RuntimeAction
{
    public RuntimeAction(Rectangle area, string action, bool triggerOnTouch = false)
    {
        this.Area = area;
        this.Action = action;
        this.TriggerOnTouch = triggerOnTouch;
    }

    public Rectangle Area { get; set; }

    public string Action { get; set; }

    public bool TriggerOnTouch { get; set; }
}

public sealed class RuntimeBuildMaterial
{
    public string ItemId { get; set; } = string.Empty;

    public int Amount { get; set; }
}

public sealed class RuntimeDrawLayer
{
    public string Id { get; set; } = string.Empty;

    public string? Texture { get; set; }

    public Rectangle SourceRect { get; set; }

    public Point DrawPosition { get; set; }

    public bool DrawInBackground { get; set; }

    public float SortTileOffset { get; set; }

    public int FrameDuration { get; set; } = 90;

    public int FrameCount { get; set; } = 1;

    public int FramesPerRow { get; set; } = -1;

    public Point AnimalDoorOffset { get; set; }
}

public sealed class RuntimeTileSprite
{
    public Texture2D Texture { get; set; } = null!;

    public Rectangle SourceRect { get; set; }

    public Point DrawPosition { get; set; }

    public SpriteEffects Effects { get; set; }

    public float Rotation { get; set; }
}
