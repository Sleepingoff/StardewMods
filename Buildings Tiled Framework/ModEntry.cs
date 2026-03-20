using GenericModConfigMenu;
using BuildingsTiledFramework.Patches;
using BuildingsTiledFramework.Runtime;
using BuildingsTiledFramework.Tiled;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BuildingsTiledFramework;

public sealed class ModEntry : Mod
{
    private const string GenericModConfigMenuId = "spacechase0.GenericModConfigMenu";
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
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        this.RegisterGenericModConfigMenu();
    }

    private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
    {
        this.RebindLoadedBuildings();
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
        this.RebindLoadedBuildings();
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
}
