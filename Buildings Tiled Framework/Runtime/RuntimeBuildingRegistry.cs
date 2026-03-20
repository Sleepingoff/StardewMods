using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace BuildingsTiledFramework.Runtime;

public sealed class RuntimeBuildingRegistry
{
    private readonly IMonitor monitor;
    private readonly Dictionary<string, List<RuntimeBuildingDefinition>> candidates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RuntimeBuildingDefinition> activeDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConditionalWeakTable<Building, RuntimeBuildingDefinition> bindings = new();

    public RuntimeBuildingRegistry(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public void Register(RuntimeBuildingDefinition definition)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Runtime building definitions must have an id.", nameof(definition));
        if (definition.Texture is null)
            throw new ArgumentException("Runtime building definitions must have a texture.", nameof(definition));

        if (!this.candidates.TryGetValue(definition.Id, out var definitions))
        {
            definitions = new List<RuntimeBuildingDefinition>();
            this.candidates[definition.Id] = definitions;
        }

        var existingIndex = definitions.FindIndex(p => string.Equals(p.SourcePackUniqueId, definition.SourcePackUniqueId, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            definitions[existingIndex] = definition;
        else
            definitions.Add(definition);

        this.activeDefinitions.TryAdd(definition.Id, definition);
    }

    public RuntimeBuildingDefinition? Get(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return this.activeDefinitions.TryGetValue(id, out var definition)
            ? definition
            : null;
    }

    public IReadOnlyList<RuntimeBuildingDefinition> GetCandidates(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Array.Empty<RuntimeBuildingDefinition>();

        return this.candidates.TryGetValue(id, out var definitions)
            ? definitions
            : Array.Empty<RuntimeBuildingDefinition>();
    }

    public IEnumerable<string> GetConflictIds()
    {
        foreach (var pair in this.candidates)
        {
            if (pair.Value.Count > 1)
                yield return pair.Key;
        }
    }

    public bool Select(string id, string? sourcePackUniqueId)
    {
        if (!this.candidates.TryGetValue(id, out var definitions) || definitions.Count == 0)
            return false;

        RuntimeBuildingDefinition? selectedDefinition = null;
        if (!string.IsNullOrWhiteSpace(sourcePackUniqueId))
        {
            selectedDefinition = definitions.FirstOrDefault(p =>
                string.Equals(p.SourcePackUniqueId, sourcePackUniqueId, StringComparison.OrdinalIgnoreCase));
        }

        selectedDefinition ??= definitions[0];
        this.activeDefinitions[id] = selectedDefinition;
        return true;
    }

    public RuntimeBuildingDefinition? Bind(Building building)
    {
        if (building is null)
            throw new ArgumentNullException(nameof(building));

        this.bindings.Remove(building);

        var buildingType = building.buildingType?.Value;
        var definition = this.Get(buildingType);
        if (definition is null)
            return null;

        this.TryApplyDefinition(building, definition);
        this.bindings.Add(building, definition);
        return definition;
    }

    public RuntimeBuildingDefinition? Get(Building building)
    {
        if (building is null)
            throw new ArgumentNullException(nameof(building));

        if (this.bindings.TryGetValue(building, out var definition))
            return this.ResolveSeasonal(definition, building);

        definition = this.Bind(building);
        return definition is null
            ? null
            : this.ResolveSeasonal(definition, building);
    }

    private RuntimeBuildingDefinition ResolveSeasonal(RuntimeBuildingDefinition definition, Building building)
    {
        if (!definition.HasSeasonalVariants)
            return definition;

        var location = building.GetParentLocation();
        var season = location is null
            ? Game1.currentSeason
            : Game1.GetSeasonForLocation(location).ToString();

        return definition.ResolveForSeason(season);
    }

    private void TryApplyDefinition(Building building, RuntimeBuildingDefinition definition)
    {
        this.TrySetDimensions(building, definition);
        this.TrySetDoor(building, definition);
    }

    private void TrySetDimensions(Building building, RuntimeBuildingDefinition definition)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var appliedWidth = this.TrySetIntMember(building, "tilesWide", definition.Size.X, flags);
        var appliedHeight = this.TrySetIntMember(building, "tilesHigh", definition.Size.Y, flags);

    }

    private void TrySetDoor(Building building, RuntimeBuildingDefinition definition)
    {
        var point = new Point((int)definition.Door.X, (int)definition.Door.Y);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (this.TrySetMember(building, "humanDoor", point, flags))
            return;

        this.monitor.Log(
            $"Failed to apply humanDoor to building '{building.buildingType?.Value}' at ({building.tileX.Value}, {building.tileY.Value}).",
            LogLevel.Warn);
    }

    private bool TrySetMember(object instance, string memberName, Point pointValue, BindingFlags flags)
    {
        var type = instance.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property is not null && this.TryAssignValue(
                property.PropertyType,
                () => property.CanRead ? property.GetValue(instance) : null,
                value => property.SetValue(instance, value),
                pointValue))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && this.TryAssignValue(
                field.FieldType,
                () => field.GetValue(instance),
                value => field.SetValue(instance, value),
                pointValue))
            return true;

        return false;
    }

    private bool TrySetIntMember(object instance, string memberName, int intValue, BindingFlags flags)
    {
        var type = instance.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property is not null && this.TryAssignScalarValue(
                property.PropertyType,
                () => property.CanRead ? property.GetValue(instance) : null,
                value => property.SetValue(instance, value),
                intValue))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && this.TryAssignScalarValue(
                field.FieldType,
                () => field.GetValue(instance),
                value => field.SetValue(instance, value),
                intValue))
            return true;

        return false;
    }

    private bool TryAssignValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, Point pointValue)
    {
        if (targetType == typeof(Point))
        {
            assign(pointValue);
            return true;
        }

        if (targetType == typeof(Vector2))
        {
            assign(new Vector2(pointValue.X, pointValue.Y));
            return true;
        }

        var valueProperty = targetType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is null || !valueProperty.CanWrite)
            return false;

        var targetInstance = getCurrentValue() ?? Activator.CreateInstance(targetType);
        if (targetInstance is null)
            return false;

        if (valueProperty.PropertyType == typeof(Point))
        {
            valueProperty.SetValue(targetInstance, pointValue);
            assign(targetInstance);
            return true;
        }

        if (valueProperty.PropertyType == typeof(Vector2))
        {
            valueProperty.SetValue(targetInstance, new Vector2(pointValue.X, pointValue.Y));
            assign(targetInstance);
            return true;
        }

        return false;
    }

    private bool TryAssignScalarValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, int intValue)
    {
        if (targetType == typeof(int))
        {
            assign(intValue);
            return true;
        }

        var valueProperty = targetType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is null || !valueProperty.CanWrite)
            return false;

        var targetInstance = getCurrentValue() ?? Activator.CreateInstance(targetType);
        if (targetInstance is null)
            return false;

        if (valueProperty.PropertyType == typeof(int))
        {
            valueProperty.SetValue(targetInstance, intValue);
            assign(targetInstance);
            return true;
        }

        return false;
    }
}
