using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using BuildingsTiledFramework.Runtime;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace BuildingsTiledFramework.Tiled;

public sealed class TiledLoader
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ITiledObjectProvider? tiledObjectProvider;

    public TiledLoader(IModHelper helper, IMonitor monitor, ITiledObjectProvider? tiledObjectProvider = null)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.tiledObjectProvider = tiledObjectProvider;
    }

    public RuntimeBuildingDefinition LoadDefinition(ITiledObject tiledObject, string? tiledFilePath = null, IContentPack? contentPack = null)
    {
        if (tiledObject is null)
            throw new ArgumentNullException(nameof(tiledObject));

        var id = this.ResolveDefinitionId(tiledObject, tiledFilePath);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("Tiled building object is missing required property 'id', and no tiled file name was provided.");

        var texturePath = tiledObject.Get("texture");
        if (string.IsNullOrWhiteSpace(texturePath))
            throw new InvalidOperationException($"Tiled building '{id}' is missing required property 'texture'.");

        var width = tiledObject.GetInt("width");
        var height = tiledObject.GetInt("height");
        var doorX = tiledObject.GetInt("doorX");
        var doorY = tiledObject.GetInt("doorY");

        var definition = new RuntimeBuildingDefinition
        {
            Id = id,
            Texture = this.LoadTexture(texturePath, contentPack),
            Size = new Point(width, height),
            Door = new Vector2(doorX, doorY),
            HasExplicitDoor = tiledObject.Has("doorX") || tiledObject.Has("doorY"),
            AllowsFlooringUnderneath = TryParseBool(tiledObject.Get("AllowsFlooringUnderneath")),
            DrawShadow = TryParseBool(tiledObject.Get("DrawShadow")) ?? false,
            BuildCost = TryParseInt(tiledObject.Get("BuildCost")),
            BuildDays = TryParseInt(tiledObject.Get("BuildDays")),
            Builder = NormalizeBuilder(tiledObject.Get("Builder")),
        };

        if (tiledObject.Has("sourceX") && tiledObject.Has("sourceY") && tiledObject.Has("sourceWidth") && tiledObject.Has("sourceHeight"))
        {
            definition.SourceRect = new Rectangle(
                tiledObject.GetInt("sourceX"),
                tiledObject.GetInt("sourceY"),
                tiledObject.GetInt("sourceWidth"),
                tiledObject.GetInt("sourceHeight"));
        }

        if (tiledObject.Has("Action"))
        {
            var action = tiledObject.Get("Action");
            if (!string.IsNullOrWhiteSpace(action))
            {
                definition.Actions.Add(new RuntimeAction(
                    new Rectangle(tiledObject.TileX, tiledObject.TileY, Math.Max(1, tiledObject.TileWidth), Math.Max(1, tiledObject.TileHeight)),
                    action));
            }
        }

        if (tiledObject.Has("TouchAction"))
        {
            var touchAction = tiledObject.Get("TouchAction");
            if (!string.IsNullOrWhiteSpace(touchAction))
            {
                definition.Actions.Add(new RuntimeAction(
                    new Rectangle(tiledObject.TileX, tiledObject.TileY, Math.Max(1, tiledObject.TileWidth), Math.Max(1, tiledObject.TileHeight)),
                    touchAction,
                    triggerOnTouch: true));
            }
        }

        this.PopulateBuildMaterials(definition, tiledObject.Get("BuildMaterials"), "<inline object>");

        return definition;
    }

    public RuntimeBuildingDefinition LoadDefinitionFromFile(string tiledFilePath, IContentPack? contentPack = null)
    {
        if (string.Equals(Path.GetExtension(tiledFilePath), ".tmx", StringComparison.OrdinalIgnoreCase))
            return this.LoadDefinitionFromTmxFile(tiledFilePath, contentPack);

        if (this.tiledObjectProvider is null)
            throw new InvalidOperationException("No tiled object provider has been configured.");

        var tiledObject = this.tiledObjectProvider.Load(tiledFilePath);
        return this.LoadDefinition(tiledObject, tiledFilePath, contentPack);
    }

    private RuntimeBuildingDefinition LoadDefinitionFromTmxFile(string tiledFilePath, IContentPack? contentPack)
    {
        if (contentPack is null)
            throw new InvalidOperationException($"TMX file '{tiledFilePath}' requires a content pack context.");

        var document = XDocument.Load(tiledFilePath);
        var mapElement = document.Root ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' is empty.");

        var mapWidth = this.GetRequiredIntAttribute(mapElement, "width");
        var mapHeight = this.GetRequiredIntAttribute(mapElement, "height");
        var tileWidth = this.GetRequiredIntAttribute(mapElement, "tilewidth");
        var tileHeight = this.GetRequiredIntAttribute(mapElement, "tileheight");

        var tilesetElement = mapElement.Element("tileset")
            ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' is missing a tileset.");
        var imageElement = tilesetElement.Element("image")
            ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' is missing a tileset image.");

        var firstGid = this.GetOptionalIntAttribute(tilesetElement, "firstgid") ?? 1;
        var columns = this.GetRequiredIntAttribute(tilesetElement, "columns");
        var tilesetTileWidth = this.GetRequiredIntAttribute(tilesetElement, "tilewidth");
        var tilesetTileHeight = this.GetRequiredIntAttribute(tilesetElement, "tileheight");
        var imageSource = imageElement.Attribute("source")?.Value
            ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' is missing an image source.");

        var tilesetTexture = this.LoadTilesetTextureFromSource(imageSource, tiledFilePath, contentPack);
        var drawTextures = this.ComposeDrawTextures(
            mapElement,
            tilesetTexture,
            firstGid,
            columns,
            tilesetTileWidth,
            tilesetTileHeight,
            mapWidth,
            mapHeight,
            tileWidth,
            tileHeight);

        var properties = this.ReadProperties(mapElement.Element("properties"));
        var id = this.ResolveDefinitionId(new DictionaryTiledObject(properties), tiledFilePath)
            ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' could not resolve a building id.");
        var footprintBounds = this.GetFootprintBounds(mapElement, mapWidth, mapHeight);
        var definition = new RuntimeBuildingDefinition
        {
            Id = id,
            Size = new Point(footprintBounds.Width, footprintBounds.Height),
            FootprintOrigin = new Point(footprintBounds.X, footprintBounds.Y),
            Inherits = properties.TryGetValue("inherits", out var inherits) && !string.IsNullOrWhiteSpace(inherits)
                ? this.NormalizeBuildingId(inherits)
                : null,
            AllowsFlooringUnderneath = this.GetNullableBoolProperty(properties, "AllowsFlooringUnderneath"),
            DrawShadow = this.GetNullableBoolProperty(properties, "DrawShadow") ?? false,
            BuildCost = this.GetNullableIntProperty(properties, "BuildCost"),
            BuildDays = this.GetNullableIntProperty(properties, "BuildDays"),
            Builder = NormalizeBuilder(this.GetStringProperty(properties, "Builder")),
        };

        foreach (var pair in drawTextures)
            definition.SetDrawTexture(pair.Key, pair.Value);

        if (definition.Texture is null)
            throw new InvalidOperationException($"TMX file '{tiledFilePath}' did not produce a Buildings draw texture.");

        if (properties.TryGetValue("exitX", out var exitXRaw)
            && int.TryParse(exitXRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exitX)
            && properties.TryGetValue("exitY", out var exitYRaw)
            && int.TryParse(exitYRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exitY))
        {
            definition.Exit = new Point(exitX, exitY);
        }

        if (properties.TryGetValue("maxOccupants", out var maxOccupantsRaw)
            && int.TryParse(maxOccupantsRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxOccupants))
        {
            definition.MaxOccupants = maxOccupants;
        }

        this.PopulateBuildMaterials(definition, this.GetStringProperty(properties, "BuildMaterials"), tiledFilePath);

        this.ReadObjectLayerData(mapElement, tileWidth, tileHeight, definition, firstGid, columns, tilesetTileWidth, tilesetTileHeight);

        var rawCollisionTiles = this.ReadCollisionTiles(mapElement, mapWidth);
        foreach (var collisionTile in rawCollisionTiles)
            definition.CollisionTiles.Add(collisionTile);

        this.NormalizeToFootprint(definition);

        var defaultDoor = definition.Actions.FirstOrDefault(action => !action.TriggerOnTouch);
        var defaultDoorX = definition.HasExplicitDoor || HasDoorProperty(properties)
            ? (int)definition.Door.X
            : defaultDoor?.Area.X ?? 0;
        var defaultDoorY = definition.HasExplicitDoor || HasDoorProperty(properties)
            ? (int)definition.Door.Y
            : defaultDoor?.Area.Y ?? Math.Max(0, mapHeight - 1);
        definition.Door = new Vector2(
            this.GetIntProperty(properties, "doorX", defaultDoorX),
            this.GetIntProperty(properties, "doorY", defaultDoorY));
        definition.HasExplicitDoor = definition.HasExplicitDoor || HasDoorProperty(properties);

        this.RemoveNonBlockingTiles(definition);

        return definition;
    }

    private Texture2D LoadTexture(string texturePath, IContentPack? contentPack)
    {
        if (texturePath.StartsWith(".", StringComparison.Ordinal))
        {
            var assetName = texturePath[1..].TrimStart('/', '\\');
            if (string.IsNullOrWhiteSpace(assetName))
                throw new InvalidOperationException("Texture property started with '.' but no game asset name was provided.");

            return this.helper.GameContent.Load<Texture2D>(assetName);
        }

        if (contentPack is null)
            throw new InvalidOperationException(
                $"Texture path '{texturePath}' is content-pack-relative, but no content pack context was provided.");

        return contentPack.ModContent.Load<Texture2D>(texturePath);
    }

    private string? ResolveDefinitionId(ITiledObject tiledObject, string? tiledFilePath)
    {
        var explicitId = tiledObject.Get("id");
        if (!string.IsNullOrWhiteSpace(explicitId))
            return this.NormalizeBuildingId(explicitId);

        if (string.IsNullOrWhiteSpace(tiledFilePath))
            return null;

        var fileName = Path.GetFileNameWithoutExtension(tiledFilePath);
        return this.NormalizeBuildingId(fileName);
    }

    private string NormalizeBuildingId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return id;

        var suffixIndex = id.Length;
        while (suffixIndex > 0 && char.IsDigit(id[suffixIndex - 1]))
            suffixIndex--;

        var baseName = id[..suffixIndex];
        var suffix = id[suffixIndex..];

        return baseName.ToLowerInvariant() switch
        {
            "farmhouse" => "FarmHouse" + suffix,
            "cabin" => "Cabin" + suffix,
            _ => id
        };
    }

    private Texture2D LoadTilesetTextureFromSource(string imageSource, string tiledFilePath, IContentPack contentPack)
    {
        var tmxDirectory = Path.GetDirectoryName(tiledFilePath)
            ?? throw new InvalidOperationException($"TMX file '{tiledFilePath}' does not have a valid directory.");
        var fullImagePath = Path.GetFullPath(Path.Combine(tmxDirectory, imageSource));
        var contentPackDirectory = Path.GetFullPath(contentPack.DirectoryPath);
        var normalizedContentPackDirectory = NormalizePathForComparison(contentPackDirectory);
        var normalizedFullImagePath = NormalizePathForComparison(fullImagePath);

        var isInsideContentPack =
            string.Equals(normalizedFullImagePath, normalizedContentPackDirectory, StringComparison.Ordinal)
            || normalizedFullImagePath.StartsWith(normalizedContentPackDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        if (!isInsideContentPack)
            throw new InvalidOperationException($"Tileset image '{imageSource}' points outside the content pack.");

        var relativePath = Path.GetRelativePath(contentPackDirectory, fullImagePath);

        var gameAssetCandidates = this.GetGameAssetNamesFromTilesetSource(imageSource);
        foreach (var gameAssetName in gameAssetCandidates)
        {
            var parsedAssetName = this.helper.GameContent.ParseAssetName(gameAssetName);
            if (!this.helper.GameContent.DoesAssetExist<Texture2D>(parsedAssetName))
                continue;

            try
            {
                var texture = Game1.content.Load<Texture2D>(gameAssetName);
                return texture;
            }
            catch (Exception ex)
            {
                this.monitor.Log(
                    $"Tileset image source '{imageSource}' in '{tiledFilePath}' requested game asset '{gameAssetName}', but it could not be loaded. Falling back to content pack path '{relativePath}'. Error: {ex.Message}",
                    LogLevel.Warn);
            }
        }

        return contentPack.ModContent.Load<Texture2D>(relativePath);
    }

    private static string NormalizePathForComparison(string path)
    {
        var fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Normalize(NormalizationForm.FormC);
    }

    private List<string> GetGameAssetNamesFromTilesetSource(string imageSource)
    {
        var fileName = Path.GetFileName(imageSource);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.StartsWith(".", StringComparison.Ordinal))
            return new List<string>();

        var assetFileName = fileName[1..];
        if (string.IsNullOrWhiteSpace(assetFileName))
            return new List<string>();

        var assetBaseName = Path.GetFileNameWithoutExtension(assetFileName);
        if (string.IsNullOrWhiteSpace(assetBaseName))
            return new List<string>();

        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return;

            var normalized = candidate.Replace('\\', '/').Trim().Trim('/');
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            if (seen.Add(normalized))
                candidates.Add(normalized);
        }

        var sourceDirectory = Path.GetDirectoryName(imageSource)?.Replace('\\', '/').Trim();
        if (!string.IsNullOrWhiteSpace(sourceDirectory) && sourceDirectory != ".")
            AddCandidate($"{sourceDirectory.Trim('/')}/{assetBaseName}");

        AddCandidate($"Maps/{assetBaseName}");
        AddCandidate($"Buildings/{assetBaseName}");
        AddCandidate($"LooseSprites/{assetBaseName}");
        AddCandidate($"TileSheets/{assetBaseName}");
        AddCandidate(assetBaseName);

        return candidates;
    }

    private Dictionary<string, Texture2D> ComposeDrawTextures(
        XElement mapElement,
        Texture2D tilesetTexture,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight,
        int mapWidth,
        int mapHeight,
        int tileWidth,
        int tileHeight)
    {
        var drawGroups = mapElement.Elements("layer")
            .GroupBy(this.ResolveDrawLayer)
            .ToList();
        var textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in drawGroups)
        {
            var texture = this.ComposeLayerGroupTexture(
                group,
                tilesetTexture,
                firstGid,
                columns,
                tilesetTileWidth,
                tilesetTileHeight,
                mapWidth,
                mapHeight,
                tileWidth,
                tileHeight);

            if (texture is not null)
            {
                textures[group.Key] = texture;
            }
        }

        return textures;
    }

    private Texture2D? ComposeLayerGroupTexture(
        IEnumerable<XElement> layers,
        Texture2D tilesetTexture,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight,
        int mapWidth,
        int mapHeight,
        int tileWidth,
        int tileHeight)
    {
        var graphicsDevice = Game1.graphics.GraphicsDevice;
        var renderTarget = new RenderTarget2D(
            graphicsDevice,
            mapWidth * tileWidth,
            mapHeight * tileHeight);

        var spriteBatch = new SpriteBatch(graphicsDevice);
        var previousTargets = graphicsDevice.GetRenderTargets();
        var drewAnyTile = false;
        try
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            foreach (var layerElement in layers)
            {
                var gids = this.ReadLayerData(layerElement);
                for (var index = 0; index < gids.Count; index++)
                {
                    var gid = gids[index];
                    if (gid < firstGid)
                        continue;

                    drewAnyTile = true;
                    var tileIndex = gid - firstGid;
                    var sourceRectangle = new Rectangle(
                        (tileIndex % columns) * tilesetTileWidth,
                        (tileIndex / columns) * tilesetTileHeight,
                        tilesetTileWidth,
                        tilesetTileHeight);

                    var destinationRectangle = new Rectangle(
                        (index % mapWidth) * tileWidth,
                        (index / mapWidth) * tileHeight,
                        tileWidth,
                        tileHeight);

                    spriteBatch.Draw(tilesetTexture, destinationRectangle, sourceRectangle, Color.White);
                }
            }

            spriteBatch.End();
        }
        finally
        {
            graphicsDevice.SetRenderTargets(previousTargets);
            spriteBatch.Dispose();
        }

        return drewAnyTile ? renderTarget : null;
    }

    private List<int> ReadLayerData(XElement layerElement)
    {
        var dataElement = layerElement.Element("data")
            ?? throw new InvalidOperationException($"Layer '{layerElement.Attribute("name")?.Value}' is missing data.");
        if (!string.Equals(dataElement.Attribute("encoding")?.Value, "csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only CSV-encoded TMX layer data is supported.");

        return dataElement.Value
            .Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.Parse(value, CultureInfo.InvariantCulture))
            .ToList();
    }

    private void ReadObjectLayerData(
        XElement mapElement,
        int tileWidth,
        int tileHeight,
        RuntimeBuildingDefinition definition,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight)
    {
        foreach (var objectGroup in mapElement.Elements("objectgroup"))
        {
            foreach (var objectElement in objectGroup.Elements("object"))
            {
                var properties = this.ReadProperties(objectElement.Element("properties"));
                var x = this.GetRequiredFloatAttribute(objectElement, "x");
                var y = this.GetRequiredFloatAttribute(objectElement, "y");
                var width = this.GetOptionalFloatAttribute(objectElement, "width") ?? tileWidth;
                var height = this.GetOptionalFloatAttribute(objectElement, "height") ?? tileHeight;
                var gid = this.GetOptionalIntAttribute(objectElement, "gid");
                var tileRectangle = new Rectangle(
                    (int)(x / tileWidth),
                    (int)(y / tileHeight),
                    Math.Max(1, (int)Math.Ceiling(width / tileWidth)),
                    Math.Max(1, (int)Math.Ceiling(height / tileHeight)));
                properties.TryGetValue("Role", out var role);
                properties.TryGetValue("Action", out var action);
                properties.TryGetValue("TouchAction", out var touchAction);

                this.ApplyObjectRole(
                    definition,
                    role,
                    properties,
                    tileRectangle,
                    (int)x,
                    (int)y,
                    gid,
                    (int)Math.Round(width),
                    (int)Math.Round(height),
                    firstGid,
                    columns,
                    tilesetTileWidth,
                    tilesetTileHeight);

                if (this.TryMapLegacySpecialActionToRole(
                        definition,
                        action,
                        properties,
                        tileRectangle,
                        (int)x,
                        (int)y,
                        gid,
                        (int)Math.Round(width),
                        (int)Math.Round(height),
                        firstGid,
                        columns,
                        tilesetTileWidth,
                        tilesetTileHeight))
                    continue;

                var runtimeAction = this.ResolveRuntimeAction(action, role);
                if (!string.IsNullOrWhiteSpace(runtimeAction))
                {
                    if (string.Equals(runtimeAction, "Mailbox", StringComparison.OrdinalIgnoreCase))
                        definition.Mailbox ??= new Point(tileRectangle.X, tileRectangle.Y);

                    definition.Actions.Add(new RuntimeAction(tileRectangle, runtimeAction));
                }

                if (!string.IsNullOrWhiteSpace(touchAction))
                {
                    var objectGroupName = objectGroup.Attribute("name")?.Value ?? "<unnamed>";
                    var objectId = objectElement.Attribute("id")?.Value ?? "<none>";
                    definition.Actions.Add(new RuntimeAction(tileRectangle, touchAction, triggerOnTouch: true));
                    this.monitor.Log(
                        $"[TouchAction] Parsed TMX TouchAction for '{definition.Id}': area={FormatRectangle(tileRectangle)}, action='{touchAction}', objectGroup='{objectGroupName}', objectId='{objectId}'.",
                        LogLevel.Debug);
                }
            }
        }
    }

    private void ApplyObjectRole(
        RuntimeBuildingDefinition definition,
        string? role,
        IReadOnlyDictionary<string, string> properties,
        Rectangle tileRectangle,
        int pixelX,
        int pixelY,
        int? gid,
        int objectWidth,
        int objectHeight,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight)
    {
        if (string.IsNullOrWhiteSpace(role))
            return;

        if (string.Equals(role, "HumanDoor", StringComparison.OrdinalIgnoreCase))
        {
            definition.Door = new Vector2(tileRectangle.X, tileRectangle.Y);
            definition.HasExplicitDoor = true;
            return;
        }

        if (string.Equals(role, "Chimney", StringComparison.OrdinalIgnoreCase))
        {
            definition.Chimney = new Point(pixelX, pixelY);
            return;
        }

        if (string.Equals(role, "Mailbox", StringComparison.OrdinalIgnoreCase))
        {
            definition.Mailbox = new Point(tileRectangle.X, tileRectangle.Y);
            return;
        }

        if (string.Equals(role, "AnimalDoor", StringComparison.OrdinalIgnoreCase))
        {
            definition.AnimalDoor = tileRectangle;
            return;
        }

        if (string.Equals(role, "AnimalDoorClosed", StringComparison.OrdinalIgnoreCase))
        {
            definition.BuildingDrawLayers.Add(this.CreateAnimalDoorDrawLayer(definition, "Closed", properties, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight));
            return;
        }

        if (string.Equals(role, "AnimalDoorOpen", StringComparison.OrdinalIgnoreCase))
        {
            definition.BuildingDrawLayers.Add(this.CreateAnimalDoorDrawLayer(definition, "Open", properties, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight));
        }
    }

    private bool TryMapLegacySpecialActionToRole(
        RuntimeBuildingDefinition definition,
        string? action,
        IReadOnlyDictionary<string, string> properties,
        Rectangle tileRectangle,
        int pixelX,
        int pixelY,
        int? gid,
        int objectWidth,
        int objectHeight,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight)
    {
        if (string.IsNullOrWhiteSpace(action))
            return false;

        if (string.Equals(action, "HumanDoor", StringComparison.OrdinalIgnoreCase))
        {
            this.ApplyObjectRole(definition, "HumanDoor", properties, tileRectangle, pixelX, pixelY, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight);
            return true;
        }

        if (string.Equals(action, "Chimney", StringComparison.OrdinalIgnoreCase))
        {
            this.ApplyObjectRole(definition, "Chimney", properties, tileRectangle, pixelX, pixelY, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight);
            return true;
        }

        if (action.StartsWith("AnimalDoor", StringComparison.OrdinalIgnoreCase))
        {
            this.ApplyObjectRole(definition, "AnimalDoor", properties, tileRectangle, pixelX, pixelY, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight);
            this.TryParseAnimalDoorAction(definition, action);
            return true;
        }

        return false;
    }

    private RuntimeDrawLayer CreateAnimalDoorDrawLayer(
        RuntimeBuildingDefinition definition,
        string state,
        IReadOnlyDictionary<string, string> properties,
        int? gid,
        int objectWidth,
        int objectHeight,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight)
    {
        var animalDoor = definition.AnimalDoor ?? Rectangle.Empty;
        var basePixelX = animalDoor.X * Game1.tileSize;
        var basePixelY = animalDoor.Y * Game1.tileSize;
        var sourceRect = this.ResolveAnimalDoorSourceRect(properties, gid, objectWidth, objectHeight, firstGid, columns, tilesetTileWidth, tilesetTileHeight);

        return new RuntimeDrawLayer
        {
            Id = $"{definition.Id}_AnimalDoor_{state}_{definition.BuildingDrawLayers.Count}",
            Texture = null,
            SourceRect = sourceRect,
            DrawPosition = new Point(
                basePixelX + this.GetIntProperty(properties, "DrawOffsetX", 0),
                basePixelY + this.GetIntProperty(properties, "DrawOffsetY", 0)),
            DrawInBackground = this.GetBoolProperty(properties, "DrawInBackground", false),
            SortTileOffset = this.GetFloatProperty(properties, "SortTileOffset", string.Equals(state, "Open", StringComparison.OrdinalIgnoreCase) ? 1f : 0.02f),
            FrameDuration = this.GetIntProperty(properties, "FrameDuration", 90),
            FrameCount = this.GetIntProperty(properties, "FrameCount", 1),
            FramesPerRow = this.GetIntProperty(properties, "FramesPerRow", -1),
            AnimalDoorOffset = new Point(
                this.GetIntProperty(properties, "AnimalDoorOffsetX", 0),
                this.GetIntProperty(properties, "AnimalDoorOffsetY", 0)),
        };
    }

    private Rectangle ResolveAnimalDoorSourceRect(
        IReadOnlyDictionary<string, string> properties,
        int? gid,
        int objectWidth,
        int objectHeight,
        int firstGid,
        int columns,
        int tilesetTileWidth,
        int tilesetTileHeight)
    {
        if (gid is int tileGid && tileGid >= firstGid)
        {
            var tileIndex = tileGid - firstGid;
            return new Rectangle(
                (tileIndex % columns) * tilesetTileWidth,
                (tileIndex / columns) * tilesetTileHeight,
                Math.Max(tilesetTileWidth, objectWidth),
                Math.Max(tilesetTileHeight, objectHeight));
        }

        return new Rectangle(
            this.GetIntProperty(properties, "SourceX", 0),
            this.GetIntProperty(properties, "SourceY", 0),
            this.GetIntProperty(properties, "SourceWidth", 16),
            this.GetIntProperty(properties, "SourceHeight", 16));
    }

    private void TryParseAnimalDoorAction(RuntimeBuildingDefinition definition, string action)
    {
        var parts = action.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5)
            return;

        if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var openDuration))
            definition.AnimalDoorOpenDuration = openDuration;

        definition.AnimalDoorOpenSound = parts[2];

        if (float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var closeDuration))
            definition.AnimalDoorCloseDuration = closeDuration;

        definition.AnimalDoorCloseSound = parts[4];

    }

    private static bool HasDoorProperty(IReadOnlyDictionary<string, string> properties)
    {
        return properties.ContainsKey("doorX") || properties.ContainsKey("doorY");
    }

    private string? ResolveRuntimeAction(string? action, string? role)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            if (string.Equals(role, "Mailbox", StringComparison.OrdinalIgnoreCase))
                return "Mailbox";

            return null;
        }

        if (string.Equals(action, "ClubCard", StringComparison.OrdinalIgnoreCase))
            return "LumberPile";

        if (string.Equals(action, "Mailbox", StringComparison.OrdinalIgnoreCase))
            return "Mailbox";

        return action;
    }

    private List<Point> ReadCollisionTiles(XElement mapElement, int mapWidth)
    {
        var collisionTiles = new List<Point>();
        foreach (var layer in mapElement.Elements("layer"))
        {
            var layerName = layer.Attribute("name")?.Value?.Trim() ?? "<unnamed>";
            var resolvedDrawLayer = this.ResolveDrawLayer(layer);
            var includeForCollision = string.Equals(resolvedDrawLayer, RuntimeBuildingDefinition.BuildingsDrawLayer, StringComparison.OrdinalIgnoreCase);

            if (!includeForCollision)
                continue;

            var gids = this.ReadLayerData(layer);
            for (var index = 0; index < gids.Count; index++)
            {
                if (gids[index] <= 0)
                    continue;

                collisionTiles.Add(new Point(index % mapWidth, index / mapWidth));
            }
        }

        return collisionTiles;
    }

    private Rectangle GetFootprintBounds(XElement mapElement, int mapWidth, int mapHeight)
    {
        var occupiedTiles = new List<Point>();
        foreach (var layer in mapElement.Elements("layer"))
        {
            var resolvedDrawLayer = this.ResolveDrawLayer(layer);
            if (!string.Equals(resolvedDrawLayer, RuntimeBuildingDefinition.BackDrawLayer, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(resolvedDrawLayer, RuntimeBuildingDefinition.BuildingsDrawLayer, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var gids = this.ReadLayerData(layer);
            for (var index = 0; index < gids.Count; index++)
            {
                if (gids[index] <= 0)
                    continue;

                occupiedTiles.Add(new Point(index % mapWidth, index / mapWidth));
            }
        }

        if (occupiedTiles.Count == 0)
            return new Rectangle(0, 0, mapWidth, mapHeight);

        var minX = occupiedTiles.Min(p => p.X);
        var minY = occupiedTiles.Min(p => p.Y);
        var maxX = occupiedTiles.Max(p => p.X);
        var maxY = occupiedTiles.Max(p => p.Y);
        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void NormalizeToFootprint(RuntimeBuildingDefinition definition)
    {
        var origin = definition.FootprintOrigin;
        if (origin == Point.Zero)
            return;

        definition.Door = new Vector2(definition.Door.X - origin.X, definition.Door.Y - origin.Y);

        if (definition.Mailbox is Point mailbox)
            definition.Mailbox = new Point(mailbox.X - origin.X, mailbox.Y - origin.Y);

        if (definition.AnimalDoor is Rectangle animalDoor)
            definition.AnimalDoor = new Rectangle(animalDoor.X - origin.X, animalDoor.Y - origin.Y, animalDoor.Width, animalDoor.Height);

        for (var i = 0; i < definition.BuildingDrawLayers.Count; i++)
        {
            var drawLayer = definition.BuildingDrawLayers[i];
            drawLayer.DrawPosition = new Point(
                drawLayer.DrawPosition.X - origin.X * Game1.tileSize,
                drawLayer.DrawPosition.Y - origin.Y * Game1.tileSize);
        }

        for (var i = 0; i < definition.Actions.Count; i++)
        {
            var action = definition.Actions[i];
            definition.Actions[i] = new RuntimeAction(
                new Rectangle(action.Area.X - origin.X, action.Area.Y - origin.Y, action.Area.Width, action.Area.Height),
                action.Action,
                action.TriggerOnTouch);

            if (action.TriggerOnTouch)
            {
                this.monitor.Log(
                    $"[TouchAction] Normalized TouchAction for '{definition.Id}': {FormatRectangle(action.Area)} -> {FormatRectangle(definition.Actions[i].Area)}, action='{action.Action}', origin=({origin.X}, {origin.Y}).",
                    LogLevel.Debug);
            }
        }

        var normalizedCollisionTiles = definition.CollisionTiles
            .Select(p => new Point(p.X - origin.X, p.Y - origin.Y))
            .ToHashSet();
        definition.CollisionTiles.Clear();
        foreach (var point in normalizedCollisionTiles)
            definition.CollisionTiles.Add(point);
    }

    private void RemoveNonBlockingTiles(RuntimeBuildingDefinition definition)
    {
        return;
    }

    private Dictionary<string, string> ReadProperties(XElement? propertiesElement)
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (propertiesElement is null)
            return properties;

        foreach (var propertyElement in propertiesElement.Elements("property"))
        {
            var name = propertyElement.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            properties[name] = propertyElement.Attribute("value")?.Value ?? propertyElement.Value;
        }

        return properties;
    }

    private string ResolveDrawLayer(XElement layerElement)
    {
        var layerName = layerElement.Attribute("name")?.Value?.Trim();
        if (string.Equals(layerName, RuntimeBuildingDefinition.BackDrawLayer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(layerName, RuntimeBuildingDefinition.BuildingsDrawLayer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(layerName, RuntimeBuildingDefinition.FrontDrawLayer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(layerName, RuntimeBuildingDefinition.AlwaysFrontDrawLayer, StringComparison.OrdinalIgnoreCase))
        {
            return layerName;
        }

        var drawProperties = this.ReadProperties(layerElement.Element("properties"));
        if (drawProperties.TryGetValue("Draw", out var drawLayer) && !string.IsNullOrWhiteSpace(drawLayer))
            return drawLayer.Trim();

        return layerName ?? RuntimeBuildingDefinition.BuildingsDrawLayer;
    }

    private int GetIntProperty(IReadOnlyDictionary<string, string> properties, string key, int defaultValue)
    {
        return properties.TryGetValue(key, out var rawValue) && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private float GetFloatProperty(IReadOnlyDictionary<string, string> properties, string key, float defaultValue)
    {
        return properties.TryGetValue(key, out var rawValue) && float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    private bool GetBoolProperty(IReadOnlyDictionary<string, string> properties, string key, bool defaultValue)
    {
        return properties.TryGetValue(key, out var rawValue) && bool.TryParse(rawValue, out var value)
            ? value
            : defaultValue;
    }

    private bool? GetNullableBoolProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var rawValue)
            ? TryParseBool(rawValue)
            : null;
    }

    private int? GetNullableIntProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var rawValue)
            ? TryParseInt(rawValue)
            : null;
    }

    private string? GetStringProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var rawValue)
            ? rawValue
            : null;
    }

    private static bool? TryParseBool(string? rawValue)
    {
        return bool.TryParse(rawValue, out var value)
            ? value
            : null;
    }

    private static int? TryParseInt(string? rawValue)
    {
        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static string FormatRectangle(Rectangle rectangle)
    {
        return $"({rectangle.X}, {rectangle.Y}, {rectangle.Width}, {rectangle.Height})";
    }

    private static string? NormalizeBuilder(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var trimmed = rawValue.Trim();
        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
            return null;
        if (string.Equals(trimmed, "Robin", StringComparison.OrdinalIgnoreCase))
            return "Robin";
        if (string.Equals(trimmed, "Wizard", StringComparison.OrdinalIgnoreCase))
            return "Wizard";

        return trimmed;
    }

    private void PopulateBuildMaterials(RuntimeBuildingDefinition definition, string? rawValue, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return;

        var tokens = rawValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length % 2 != 0)
        {
            this.monitor.Log(
                $"BuildMaterials in '{sourceName}' must contain itemId/amount pairs. Raw value: '{rawValue}'.",
                LogLevel.Warn);
            return;
        }

        for (var i = 0; i < tokens.Length; i += 2)
        {
            var itemId = tokens[i];
            var amountRaw = tokens[i + 1];
            if (!int.TryParse(amountRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
            {
                this.monitor.Log(
                    $"BuildMaterials in '{sourceName}' has invalid amount '{amountRaw}' for item '{itemId}'.",
                    LogLevel.Warn);
                continue;
            }

            definition.BuildMaterials.Add(new RuntimeBuildMaterial
            {
                ItemId = itemId,
                Amount = amount,
            });
        }
    }

    private int GetRequiredIntAttribute(XElement element, string attributeName)
    {
        return this.GetOptionalIntAttribute(element, attributeName)
            ?? throw new InvalidOperationException($"Element '{element.Name}' is missing required attribute '{attributeName}'.");
    }

    private int? GetOptionalIntAttribute(XElement element, string attributeName)
    {
        var rawValue = element.Attribute(attributeName)?.Value;
        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private float GetRequiredFloatAttribute(XElement element, string attributeName)
    {
        return this.GetOptionalFloatAttribute(element, attributeName)
            ?? throw new InvalidOperationException($"Element '{element.Name}' is missing required attribute '{attributeName}'.");
    }

    private float? GetOptionalFloatAttribute(XElement element, string attributeName)
    {
        var rawValue = element.Attribute(attributeName)?.Value;
        return float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private sealed class DictionaryTiledObject : ITiledObject
    {
        private readonly IReadOnlyDictionary<string, string> values;

        public DictionaryTiledObject(IReadOnlyDictionary<string, string> values)
        {
            this.values = values;
        }

        public int TileX => 0;

        public int TileY => 0;

        public int TileWidth => 1;

        public int TileHeight => 1;

        public string? Get(string key)
        {
            return this.values.TryGetValue(key, out var value)
                ? value
                : null;
        }

        public int GetInt(string key)
        {
            return this.values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        public bool Has(string key)
        {
            return this.values.ContainsKey(key);
        }
    }
}
