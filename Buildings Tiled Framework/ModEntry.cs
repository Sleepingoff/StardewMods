using GenericModConfigMenu;
using BuildingsTiledFramework.Patches;
using BuildingsTiledFramework.Runtime;
using BuildingsTiledFramework.Tiled;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace BuildingsTiledFramework;

public sealed class ModEntry : Mod
{
    private const string GenericModConfigMenuId = "spacechase0.GenericModConfigMenu";
    private const string ReloadCommandName = "btf_reload";
    private static readonly string[] Seasons = new[] { "spring", "summer", "fall", "winter" };

    internal static ModEntry Instance { get; private set; } = null!;

    internal RuntimeBuildingRegistry Registry { get; private set; } = null!;

    internal TiledLoader TiledLoader { get; private set; } = null!;

    internal ITiledObjectProvider? TiledObjectProvider { get; set; }

    private ModConfig Config { get; set; } = new();

    public override void Entry(IModHelper helper)
    {
        Instance = this;
        this.Config = helper.ReadConfig<ModConfig>();
        this.Registry = new RuntimeBuildingRegistry(this.Monitor);
        this.TiledLoader = new TiledLoader(helper, this.Monitor, this.TiledObjectProvider);

        BuildingPatches.Initialize(this.Registry, this.Monitor);

        var harmony = new Harmony(this.ModManifest.UniqueID);
        this.ApplyHarmonyPatches(harmony);

        this.LoadContentPackBuildings();
        this.ApplyConfiguredSelections();
        helper.ConsoleCommands.Add(
            ReloadCommandName,
            "Reload Buildings Tiled Framework TMX definitions from content packs and rebind placed buildings.",
            this.OnReloadCommand);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        this.RegisterGenericModConfigMenu();
    }

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        this.ReloadContentPackBuildings();
        this.RebindLoadedBuildings();
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        foreach (var definition in this.Registry.GetActiveDefinitions())
        {
            if (!e.NameWithoutLocale.IsEquivalentTo(this.GetPreviewTextureAssetName(definition.Id)))
                continue;

            var previewTexture = this.ComposePreviewTexture(definition);
            e.LoadFrom(() => previewTexture, AssetLoadPriority.Exclusive);
            return;
        }

        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            return;

        e.Edit(asset =>
        {
            var data = asset.AsDictionary<string, BuildingData>().Data;
            foreach (var definition in this.Registry.GetActiveDefinitions())
            {
                if (!this.ShouldAddSyntheticBuildingData(definition, data, out var skipReason))
                    continue;

                data[definition.Id] = this.CreateSyntheticBuildingData(definition, data);
            }
        });
    }

    private void ApplyHarmonyPatches(Harmony harmony)
    {
        try
        {
            this.PatchBuildingCore(harmony);
            this.PatchCarpenterMenu(harmony);
            this.PatchBuildingData(harmony);
            this.PatchBuildingGetData(harmony);
            this.PatchBuildingExit(harmony);
            this.PatchAdditionalPlacementTiles(harmony);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to apply one or more Harmony patches: {ex}", LogLevel.Warn);
        }
    }

    private void PatchBuildingCore(Harmony harmony)
    {
        var patchType = typeof(BuildingPatches);
        harmony.Patch(
            AccessTools.Constructor(typeof(Building)),
            postfix: new HarmonyMethod(patchType.GetMethod("BuildingConstructorPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
        harmony.Patch(
            AccessTools.Method(typeof(Building), nameof(Building.doAction)),
            postfix: new HarmonyMethod(patchType.GetMethod("DoActionPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
        harmony.Patch(
            AccessTools.Method(typeof(Building), nameof(Building.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) }),
            prefix: new HarmonyMethod(patchType.GetMethod("DrawPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
        harmony.Patch(
            AccessTools.Method(typeof(GameLocation), nameof(GameLocation.UpdateWhenCurrentLocation)),
            postfix: new HarmonyMethod(patchType.GetMethod("UpdateWhenCurrentLocationPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
    }

    private void PatchCarpenterMenu(Harmony harmony)
    {
        var postfix = new HarmonyMethod(typeof(CarpenterMenuPatches).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
        foreach (var method in typeof(CarpenterMenuPatches)
                     .GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!
                     .Invoke(null, null) as IEnumerable<MethodBase> ?? Enumerable.Empty<MethodBase>())
        {
            harmony.Patch(method, postfix: postfix);
        }
    }

    private void PatchBuildingData(Harmony harmony)
    {
        var patchType = typeof(BuildingDataPatches);
        var prefix = new HarmonyMethod(patchType.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic));
        foreach (var method in patchType
                     .GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!
                     .Invoke(null, null) as IEnumerable<MethodBase> ?? Enumerable.Empty<MethodBase>())
        {
            harmony.Patch(method, prefix: prefix);
        }
    }

    private void PatchBuildingGetData(Harmony harmony)
    {
        var postfix = new HarmonyMethod(typeof(BuildingGetDataPatches).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
        foreach (var method in BuildingGetDataPatches.TargetMethods())
            harmony.Patch(method, postfix: postfix);
    }

    private void PatchBuildingExit(Harmony harmony)
    {
        var patchType = typeof(BuildingExitPatches);
        var postfix = new HarmonyMethod(patchType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
        foreach (var method in patchType
                     .GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!
                     .Invoke(null, null) as IEnumerable<MethodBase> ?? Enumerable.Empty<MethodBase>())
        {
            harmony.Patch(method, postfix: postfix);
        }
    }

    private void PatchAdditionalPlacementTiles(Harmony harmony)
    {
        var postfix = new HarmonyMethod(typeof(AdditionalPlacementTilesPatches).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
        foreach (var method in AdditionalPlacementTilesPatches.TargetMethods())
            harmony.Patch(method, postfix: postfix);
    }

    public RuntimeBuildingDefinition RegisterTiledDefinition(ITiledObject tiledObject, string tiledFilePath)
    {
        var definition = this.TiledLoader.LoadDefinition(tiledObject, tiledFilePath);
        this.Registry.Register(definition);
        return definition;
    }

    private void LoadContentPackBuildings()
    {
        var ownedContentPacks = this.Helper.ContentPacks.GetOwned().ToArray();

        foreach (var contentPack in ownedContentPacks)
        {
            var buildingsDirectory = Path.Combine(contentPack.DirectoryPath, "Buildings");
            if (!Directory.Exists(buildingsDirectory))
                continue;

            this.LoadNonSeasonalContentPackBuildings(contentPack, buildingsDirectory);
            this.LoadSeasonalContentPackBuildings(contentPack, buildingsDirectory);
        }
    }

    private void ReloadContentPackBuildings()
    {
        this.Registry.Clear();
        this.LoadContentPackBuildings();
        this.InvalidatePreviewTextureCache();
        this.ApplyConfiguredSelections();
    }

    private void OnReloadCommand(string command, string[] args)
    {
        try
        {
            var previousCount = this.Registry.GetActiveDefinitions().Count;
            this.ReloadContentPackBuildings();
            this.Helper.GameContent.InvalidateCache("Data/Buildings");
            this.RebindLoadedBuildings();

            var reloadedCount = this.Registry.GetActiveDefinitions().Count;
            var worldState = Context.IsWorldReady ? "world-ready" : "title-screen";
            this.Monitor.Log(
                $"Reloaded BTF definitions via '{ReloadCommandName}'. Definitions: {previousCount} -> {reloadedCount}. State: {worldState}.",
                LogLevel.Info);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to reload BTF definitions via '{ReloadCommandName}': {ex}", LogLevel.Error);
        }
    }

    private void LoadNonSeasonalContentPackBuildings(IContentPack contentPack, string buildingsDirectory)
    {
        var tiledFiles = Directory
            .EnumerateFiles(buildingsDirectory, "*.tmx", SearchOption.TopDirectoryOnly)
            .ToArray()
            .OrderBy(p => p, Comparer<string>.Create(this.CompareBuildingFileOrder));

        foreach (var tiledFile in tiledFiles)
            this.TryRegisterContentPackBuilding(contentPack, tiledFile);
    }

    private void LoadSeasonalContentPackBuildings(IContentPack contentPack, string buildingsDirectory)
    {
        var buildingSubdirectories = Directory
            .EnumerateDirectories(buildingsDirectory, "*", SearchOption.TopDirectoryOnly)
            .Select(path => new { Name = Path.GetFileName(path), Path = path })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .ToDictionary(entry => entry.Name!, entry => entry.Path, StringComparer.OrdinalIgnoreCase);

        var seasonalDirectories = Seasons.ToDictionary(
            season => season,
            season => buildingSubdirectories.TryGetValue(season, out var directory)
                ? directory
                : Path.Combine(buildingsDirectory, season),
            StringComparer.OrdinalIgnoreCase);

        if (seasonalDirectories.Values.All(path => !Directory.Exists(path)))
        {
            return;
        }

        var seasonalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in seasonalDirectories.Values.Where(Directory.Exists))
        {
            foreach (var filePath in Directory.EnumerateFiles(directory, "*.tmx", SearchOption.TopDirectoryOnly))
                seasonalFiles.Add(Path.GetFileName(filePath));
        }

        foreach (var fileName in seasonalFiles.OrderBy(p => p, Comparer<string>.Create(this.CompareBuildingFileOrder)))
        {
            var seasonFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var isComplete = true;

            foreach (var season in Seasons)
            {
                var directory = seasonalDirectories[season];
                var filePath = Path.Combine(directory, fileName);
                if (!File.Exists(filePath))
                {
                    isComplete = false;
                    break;
                }

                seasonFilePaths[season] = filePath;
            }

            if (!isComplete)
            {
                this.Monitor.Log(
                    $"Skipped seasonal building '{fileName}' in content pack '{contentPack.Manifest.UniqueID}' because one or more season folders are missing the file.",
                    LogLevel.Warn);
                continue;
            }

            this.TryRegisterSeasonalContentPackBuilding(contentPack, seasonFilePaths);
        }
    }

    private void TryRegisterContentPackBuilding(IContentPack contentPack, string tiledFilePath)
    {
        try
        {
            var definition = this.TiledLoader.LoadDefinitionFromFile(tiledFilePath, contentPack);
            definition.SourcePackUniqueId = contentPack.Manifest.UniqueID;
            definition.SourcePackName = contentPack.Manifest.Name;
            definition.SourceFilePath = tiledFilePath;

            this.Registry.Register(definition);
            this.LogDefinitionRegistration(definition, isSeasonal: false);
        }
        catch (Exception ex)
        {
            this.Monitor.Log(
                $"Failed to load building definition from '{tiledFilePath}' in content pack '{contentPack.Manifest.UniqueID}': {ex}",
                LogLevel.Error);
        }
    }

    private void TryRegisterSeasonalContentPackBuilding(IContentPack contentPack, IReadOnlyDictionary<string, string> seasonFilePaths)
    {
        try
        {
            RuntimeBuildingDefinition? rootDefinition = null;
            foreach (var season in Seasons)
            {
                var filePath = seasonFilePaths[season];
                var definition = this.TiledLoader.LoadDefinitionFromFile(filePath, contentPack);
                definition.SourcePackUniqueId = contentPack.Manifest.UniqueID;
                definition.SourcePackName = contentPack.Manifest.Name;
                definition.SourceFilePath = filePath;

                if (rootDefinition is null)
                {
                    rootDefinition = definition;
                }
                else if (!string.Equals(rootDefinition.Id, definition.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Seasonal building ids do not match. Expected '{rootDefinition.Id}', but '{filePath}' resolved to '{definition.Id}'.");
                }

                rootDefinition.SetSeasonalVariant(season, definition);
            }

            if (rootDefinition is null)
                return;

            this.Registry.Register(rootDefinition);
            this.LogDefinitionRegistration(rootDefinition, isSeasonal: true);
        }
        catch (Exception ex)
        {
            var samplePath = seasonFilePaths.Values.FirstOrDefault() ?? string.Empty;
            this.Monitor.Log(
                $"Failed to load seasonal building definition from '{samplePath}' in content pack '{contentPack.Manifest.UniqueID}': {ex}",
                LogLevel.Error);
        }
    }

    private void ApplyConfiguredSelections()
    {
        foreach (var buildingId in this.Registry.GetConflictIds())
        {
            this.Config.SelectedContentPacks.TryGetValue(buildingId, out var selectedPack);
            if (!this.Registry.Select(buildingId, selectedPack))
                continue;

            var activeDefinition = this.Registry.Get(buildingId);
            if (activeDefinition is null)
                continue;

            if (string.IsNullOrWhiteSpace(selectedPack) ||
                !string.Equals(selectedPack, activeDefinition.SourcePackUniqueId, StringComparison.OrdinalIgnoreCase))
            {
                this.Config.SelectedContentPacks[buildingId] = activeDefinition.SourcePackUniqueId;
            }
        }

        this.Helper.WriteConfig(this.Config);
        this.Helper.GameContent.InvalidateCache("Data/Buildings");
        this.InvalidatePreviewTextureCache();
        this.RebindLoadedBuildings();
    }

    private void InvalidatePreviewTextureCache()
    {
        foreach (var definition in this.Registry.GetActiveDefinitions())
            this.Helper.GameContent.InvalidateCache(this.GetPreviewTextureAssetName(definition.Id));
    }

    private Texture2D ComposePreviewTexture(RuntimeBuildingDefinition definition)
    {
        var backTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.BackDrawLayer);
        var buildingsTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.BuildingsDrawLayer) ?? definition.Texture;
        var frontTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.FrontDrawLayer);
        var alwaysFrontTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.AlwaysFrontDrawLayer);

        if (backTexture is null
            && frontTexture is null
            && alwaysFrontTexture is null
            && definition.BuildingDrawLayers.Count == 0)
            return buildingsTexture;

        var sourceRect = definition.SourceRect ?? buildingsTexture.Bounds;
        var graphicsDevice = Game1.graphics.GraphicsDevice;
        var renderTarget = new RenderTarget2D(graphicsDevice, sourceRect.Width, sourceRect.Height);
        var spriteBatch = new SpriteBatch(graphicsDevice);
        var previousTargets = graphicsDevice.GetRenderTargets();

        try
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            DrawPreviewLayer(spriteBatch, backTexture, sourceRect);
            foreach (var runtimeLayer in definition.BuildingDrawLayers.Where(p => p.DrawInBackground))
                this.DrawPreviewRuntimeLayer(spriteBatch, runtimeLayer, buildingsTexture);
            DrawPreviewLayer(spriteBatch, buildingsTexture, sourceRect);
            foreach (var runtimeLayer in definition.BuildingDrawLayers.Where(p => !p.DrawInBackground))
                this.DrawPreviewRuntimeLayer(spriteBatch, runtimeLayer, buildingsTexture);
            DrawPreviewLayer(spriteBatch, frontTexture, sourceRect);
            DrawPreviewLayer(spriteBatch, alwaysFrontTexture, sourceRect);
            spriteBatch.End();
        }
        finally
        {
            graphicsDevice.SetRenderTargets(previousTargets);
            spriteBatch.Dispose();
        }

        var composedTexture = new Texture2D(graphicsDevice, sourceRect.Width, sourceRect.Height);
        var pixels = new Color[sourceRect.Width * sourceRect.Height];
        renderTarget.GetData(pixels);
        composedTexture.SetData(pixels);
        renderTarget.Dispose();
        return composedTexture;
    }

    private static void DrawPreviewLayer(SpriteBatch spriteBatch, Texture2D? texture, Rectangle sourceRect)
    {
        if (texture is null)
            return;

        spriteBatch.Draw(texture, Vector2.Zero, sourceRect, Color.White);
    }

    private void DrawPreviewRuntimeLayer(SpriteBatch spriteBatch, RuntimeDrawLayer runtimeLayer, Texture2D defaultTexture)
    {
        var texture = this.ResolvePreviewRuntimeLayerTexture(runtimeLayer, defaultTexture);
        if (texture is null)
            return;

        spriteBatch.Draw(texture, runtimeLayer.DrawPosition.ToVector2(), runtimeLayer.SourceRect, Color.White);
    }

    private Texture2D? ResolvePreviewRuntimeLayerTexture(RuntimeDrawLayer runtimeLayer, Texture2D defaultTexture)
    {
        if (string.IsNullOrWhiteSpace(runtimeLayer.Texture))
            return defaultTexture;

        try
        {
            return Game1.content.Load<Texture2D>(runtimeLayer.Texture);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed to load preview DrawLayer texture '{runtimeLayer.Texture}' for '{runtimeLayer.Id}': {ex.Message}", LogLevel.Warn);
            return null;
        }
    }

    private void RegisterGenericModConfigMenu()
    {
        var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(GenericModConfigMenuId);
        if (gmcm is null)
            return;

        gmcm.Register(
            this.ModManifest,
            reset: this.ResetConfig,
            save: this.SaveConfig);

        var conflictIds = this.Registry.GetConflictIds().OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        if (conflictIds.Length == 0)
        {
            gmcm.AddSectionTitle(this.ModManifest, () => "Building Overrides");
            gmcm.AddParagraph(this.ModManifest, () => "No conflicting building overrides were found in owned content packs.");
            return;
        }

        gmcm.AddSectionTitle(this.ModManifest, () => "Building Overrides");
        gmcm.AddParagraph(this.ModManifest, () => "Choose which content pack supplies the runtime override when multiple packs patch the same building.");

        foreach (var buildingId in conflictIds)
        {
            gmcm.AddTextOption(
                this.ModManifest,
                name: () => buildingId,
                tooltip: () => $"Select the content pack to use for the '{buildingId}' building override.",
                getValue: () => this.GetSelectedPack(buildingId),
                setValue: value => this.SetSelectedPack(buildingId, value),
                allowedValues: this.Registry.GetCandidates(buildingId).Select(p => p.SourcePackUniqueId).ToArray(),
                formatAllowedValue: value => this.FormatPackName(buildingId, value),
                fieldId: $"building-source:{buildingId}");
        }
    }

    private void ResetConfig()
    {
        this.Config = new ModConfig();
        this.ApplyConfiguredSelections();
    }

    private void SaveConfig()
    {
        this.Helper.WriteConfig(this.Config);
        this.ApplyConfiguredSelections();
    }

    private string GetSelectedPack(string buildingId)
    {
        if (this.Config.SelectedContentPacks.TryGetValue(buildingId, out var selectedPack) && !string.IsNullOrWhiteSpace(selectedPack))
            return selectedPack;

        return this.Registry.Get(buildingId)?.SourcePackUniqueId ?? string.Empty;
    }

    private void SetSelectedPack(string buildingId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        this.Config.SelectedContentPacks[buildingId] = value;
        this.Registry.Select(buildingId, value);
        this.RebindLoadedBuildings();
    }

    private string FormatPackName(string buildingId, string sourcePackUniqueId)
    {
        var definition = this.Registry.GetCandidates(buildingId)
            .FirstOrDefault(p => string.Equals(p.SourcePackUniqueId, sourcePackUniqueId, StringComparison.OrdinalIgnoreCase));

        return definition is null
            ? sourcePackUniqueId
            : $"{definition.SourcePackName} ({definition.SourcePackUniqueId})";
    }

    private void RebindLoadedBuildings()
    {
        if (!Context.IsWorldReady)
            return;

        foreach (var location in Game1.locations)
        {
            foreach (var building in this.GetBuildings(location))
                this.Registry.Bind(building);
        }
    }

    private IEnumerable<StardewValley.Buildings.Building> GetBuildings(GameLocation location)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var property = location.GetType().GetProperty("buildings", flags);
        if (property?.GetValue(location) is System.Collections.IEnumerable propertyValues)
        {
            foreach (var value in propertyValues)
            {
                if (value is StardewValley.Buildings.Building building)
                    yield return building;
            }

            yield break;
        }

        var field = location.GetType().GetField("buildings", flags);
        if (field?.GetValue(location) is System.Collections.IEnumerable fieldValues)
        {
            foreach (var value in fieldValues)
            {
                if (value is StardewValley.Buildings.Building building)
                    yield return building;
            }
        }
    }

    private int CompareBuildingFileOrder(string? leftPath, string? rightPath)
    {
        var left = this.ParseUpgradeFileName(leftPath);
        var right = this.ParseUpgradeFileName(rightPath);

        var baseNameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.BaseName, right.BaseName);
        if (baseNameComparison != 0)
            return baseNameComparison;

        if (left.HasNumericSuffix && right.HasNumericSuffix)
            return left.UpgradeLevel.CompareTo(right.UpgradeLevel);

        if (left.HasNumericSuffix != right.HasNumericSuffix)
            return left.HasNumericSuffix ? 1 : -1;

        return StringComparer.OrdinalIgnoreCase.Compare(left.FileName, right.FileName);
    }

    private (string FileName, string BaseName, bool HasNumericSuffix, int UpgradeLevel) ParseUpgradeFileName(string? filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
            return (string.Empty, string.Empty, false, 0);

        var digitIndex = fileName.Length;
        while (digitIndex > 0 && char.IsDigit(fileName[digitIndex - 1]))
            digitIndex--;

        if (digitIndex == fileName.Length)
            return (fileName, fileName, false, 0);

        var baseName = fileName[..digitIndex];
        var numericSuffix = fileName[digitIndex..];
        if (!int.TryParse(numericSuffix, out var upgradeLevel))
            return (fileName, fileName, false, 0);

        return (fileName, baseName, true, upgradeLevel);
    }

    internal BuildingData CreateSyntheticBuildingData(RuntimeBuildingDefinition definition, IDictionary<string, BuildingData>? knownData = null)
    {
        if (knownData is not null
            && !string.IsNullOrWhiteSpace(definition.Inherits)
            && knownData.TryGetValue(definition.Inherits, out var knownBaseData))
        {
            return this.ApplySyntheticBuildingData(CloneBuildingData(knownBaseData), definition);
        }

        if (!string.IsNullOrWhiteSpace(definition.Inherits))
        {
            var allBuildingData = Game1.content.Load<Dictionary<string, BuildingData>>("Data/Buildings");
            if (allBuildingData.TryGetValue(definition.Inherits, out var loadedBaseData))
                return this.ApplySyntheticBuildingData(CloneBuildingData(loadedBaseData), definition);
        }

        return this.ApplySyntheticBuildingData(new BuildingData
        {
            Name = definition.Id,
            NameForGeneralType = definition.Id,
            Description = definition.Id,
            Texture = "Maps/springobjects",
            Size = definition.Size,
            BuildDays = 0,
            BuildMaterials = new List<BuildingMaterial>(),
            DrawLayers = new List<BuildingDrawLayer>(),
            ActionTiles = new List<BuildingActionTile>(),
            AdditionalPlacementTiles = new List<BuildingPlacementTile>(),
            Metadata = new Dictionary<string, string>(),
        }, definition);
    }

    private bool ShouldAddSyntheticBuildingData(RuntimeBuildingDefinition definition, IDictionary<string, BuildingData> data, out string reason)
    {
        if (data.ContainsKey(definition.Id))
        {
            reason = "base game or another asset edit already defines this ID";
            return false;
        }

        if (definition.Id.EndsWith("_prev", StringComparison.OrdinalIgnoreCase))
        {
            reason = "progression-only _prev definitions are not injected into Data/Buildings";
            return false;
        }

        reason = "eligible";
        return true;
    }

    private BuildingData ApplySyntheticBuildingData(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        buildingData.Name ??= definition.Id;
        buildingData.NameForGeneralType ??= buildingData.Name;
        buildingData.Description ??= definition.Id;
        buildingData.Texture = this.GetPreviewTextureAssetName(definition.Id);
        buildingData.Size = definition.Size;
        buildingData.BuildDays = definition.BuildDays ?? buildingData.BuildDays;
        buildingData.BuildCost = definition.BuildCost ?? buildingData.BuildCost;
        buildingData.Builder = definition.Builder ?? buildingData.Builder;
        buildingData.MagicalConstruction = string.Equals(buildingData.Builder, "Wizard", StringComparison.OrdinalIgnoreCase);
        buildingData.BuildMaterials = definition.BuildMaterials.Count > 0
            ? this.CreateSyntheticBuildMaterials(definition)
            : buildingData.BuildMaterials ?? new List<BuildingMaterial>();
        buildingData.DrawLayers ??= new List<BuildingDrawLayer>();
        buildingData.ActionTiles ??= new List<BuildingActionTile>();
        buildingData.AdditionalPlacementTiles ??= new List<BuildingPlacementTile>();
        buildingData.Metadata ??= new Dictionary<string, string>();

        return buildingData;
    }

    internal string GetPreviewTextureAssetName(string definitionId)
    {
        return $"{this.ModManifest.UniqueID}/Preview/{definitionId}";
    }

    private List<BuildingMaterial> CreateSyntheticBuildMaterials(RuntimeBuildingDefinition definition)
    {
        var materials = new List<BuildingMaterial>();
        foreach (var runtimeMaterial in definition.BuildMaterials)
        {
            materials.Add(new BuildingMaterial
            {
                ItemId = runtimeMaterial.ItemId,
                Amount = runtimeMaterial.Amount,
            });
        }

        return materials;
    }

    private static BuildingData CloneBuildingData(BuildingData data)
    {
        var json = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<BuildingData>(json)
            ?? throw new InvalidOperationException($"Failed to clone BuildingData for inherited building '{data.Name ?? "<unknown>"}'.");
    }

    private void LogDefinitionRegistration(RuntimeBuildingDefinition definition, bool isSeasonal)
    {
    }

    private static string FormatBuildMaterialsForLog(RuntimeBuildingDefinition definition)
    {
        if (definition.BuildMaterials.Count == 0)
            return "<none>";

        return string.Join(", ", definition.BuildMaterials.Select(p => $"{p.ItemId} x{p.Amount}"));
    }
}
