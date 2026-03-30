using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Menus;
using BuildingsTiledFramework.Runtime;
using System.Reflection;
using System.Collections;
using XTileLocation = xTile.Dimensions.Location;

namespace BuildingsTiledFramework.Patches;

[HarmonyPatch]
internal static class BuildingPatches
{
    private static RuntimeBuildingRegistry registry = null!;
    private static IMonitor monitor = null!;
    private static string? lastTriggeredTouchActionKey;
    private const string TouchActionLogPrefix = "[TouchAction]";

    public static void Initialize(RuntimeBuildingRegistry runtimeRegistry, IMonitor modMonitor)
    {
        registry = runtimeRegistry;
        monitor = modMonitor;
    }

    [HarmonyPatch(typeof(Building), MethodType.Constructor)]
    [HarmonyPostfix]
    private static void BuildingConstructorPostfix(Building __instance)
    {
        try
        {
            registry.Bind(__instance);
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to bind runtime definition for building '{__instance.buildingType?.Value}': {ex}", LogLevel.Error);
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.doAction))]
    [HarmonyPostfix]
    private static void DoActionPostfix(Building __instance, Vector2 tileLocation, Farmer who, ref bool __result)
    {
        if (__result)
            return;

        var definition = registry.Get(__instance);
        if (definition is null || definition.Actions.Count == 0)
            return;

        var localTile = new Point(
            (int)tileLocation.X - __instance.tileX.Value,
            (int)tileLocation.Y - __instance.tileY.Value);

        foreach (var runtimeAction in definition.Actions)
        {
            if (runtimeAction.TriggerOnTouch)
                continue;

            if (!runtimeAction.Area.Contains(localTile))
                continue;

            var location = __instance.GetParentLocation();
            if (location is null)
                return;

            location.performAction(runtimeAction.Action, who, new XTileLocation((int)tileLocation.X, (int)tileLocation.Y));
            __result = true;
            return;
        }
    }

    [HarmonyPatch(typeof(Building), nameof(Building.draw), new[] { typeof(SpriteBatch) })]
    [HarmonyPrefix]
    private static bool DrawPrefix(Building __instance, SpriteBatch b)
    {
        var definition = registry.Get(__instance);
        if (definition is null)
            return true;

        var backTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.BackDrawLayer);
        if (backTexture is not null)
            DrawLayerTexture(b, __instance, definition, backTexture, GetLayerDepth(__instance, definition, RuntimeBuildingDefinition.BackDrawLayer));

        var buildingsTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.BuildingsDrawLayer);
        if (buildingsTexture is null)
            return true;

        if (definition.DrawShadow)
        {
            DrawShadowTexture(b, __instance, definition, buildingsTexture, GetLayerDepth(__instance, definition, RuntimeBuildingDefinition.BuildingsDrawLayer));
        }
        DrawLayerTexture(b, __instance, definition, buildingsTexture, GetLayerDepth(__instance, definition, RuntimeBuildingDefinition.BuildingsDrawLayer));

        var frontTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.FrontDrawLayer);
        if (frontTexture is not null)
            DrawLayerTexture(b, __instance, definition, frontTexture, GetLayerDepth(__instance, definition, RuntimeBuildingDefinition.FrontDrawLayer));

        var alwaysFrontTexture = definition.GetDrawTexture(RuntimeBuildingDefinition.AlwaysFrontDrawLayer);
        if (alwaysFrontTexture is not null)
            DrawLayerTexture(b, __instance, definition, alwaysFrontTexture, GetLayerDepth(__instance, definition, RuntimeBuildingDefinition.AlwaysFrontDrawLayer));

        return false;
    }

    private static void DrawLayerTexture(SpriteBatch b, Building building, RuntimeBuildingDefinition definition, Texture2D texture, float depth)
    {
        var sourceRect = definition.SourceRect ?? texture.Bounds;
        var destinationRectangle = GetDestinationRectangle(building, definition, sourceRect);
        b.Draw(texture, destinationRectangle, sourceRect, Color.White, 0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    private static void DrawShadowTexture(SpriteBatch b, Building building, RuntimeBuildingDefinition definition, Texture2D texture, float baseDepth)
    {
        var sourceRect = definition.SourceRect ?? texture.Bounds;
        var destinationRectangle = GetDestinationRectangle(building, definition, sourceRect);
        var shadowDestination = new Rectangle(
            destinationRectangle.X,
            destinationRectangle.Bottom,
            destinationRectangle.Width,
            destinationRectangle.Height);

        b.Draw(
            texture,
            shadowDestination,
            sourceRect,
            Color.Black * 0.35f,
            0f,
            Vector2.Zero,
            SpriteEffects.FlipVertically,
            Math.Max(0f, baseDepth - 0.0001f));
    }

    private static Rectangle GetDestinationRectangle(Building building, RuntimeBuildingDefinition definition, Rectangle sourceRect)
    {
        var screenPosition = Game1.GlobalToLocal(
            Game1.viewport,
            new Vector2(
                (building.tileX.Value - definition.FootprintOrigin.X) * Game1.tileSize,
                (building.tileY.Value - definition.FootprintOrigin.Y) * Game1.tileSize));

        return new Rectangle(
            (int)screenPosition.X,
            (int)screenPosition.Y,
            sourceRect.Width * 4,
            sourceRect.Height * 4);
    }

    private static float GetLayerDepth(Building building, RuntimeBuildingDefinition definition, string drawLayer)
    {
        var baseDepth = Math.Max(0f, ((building.tileY.Value + definition.Size.Y) * Game1.tileSize - 24f) / 10000f);
        return drawLayer switch
        {
            RuntimeBuildingDefinition.BackDrawLayer => 0.0000001f,
            RuntimeBuildingDefinition.FrontDrawLayer => Math.Min(0.9998f, baseDepth + 0.0001f),
            RuntimeBuildingDefinition.AlwaysFrontDrawLayer => Math.Min(0.9999f, baseDepth + 0.0002f),
            _ => baseDepth,
        };
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.UpdateWhenCurrentLocation))]
    [HarmonyPostfix]
    private static void UpdateWhenCurrentLocationPostfix(GameLocation __instance)
    {
        try
        {
            monitor.Log(
                $"{TouchActionLogPrefix} UpdateWhenCurrentLocation on '{__instance.NameOrUniqueName}' ({__instance.GetType().FullName}).",
                LogLevel.Debug);
            var buildings = GetLocationBuildings(__instance);
            monitor.Log(
                $"{TouchActionLogPrefix} Location '{__instance.NameOrUniqueName}' exposed {buildings.Count} building(s) for touch evaluation.",
                LogLevel.Debug);
            if (buildings.Count == 0)
            {
                lastTriggeredTouchActionKey = null;
                return;
            }

            var playerTile = Game1.player.TilePoint;
            foreach (var building in buildings)
            {
                var definition = registry.Get(building);
                if (definition is null)
                {
                    monitor.Log(
                        $"{TouchActionLogPrefix} Skipped building '{building.buildingType?.Value}' at ({building.tileX.Value}, {building.tileY.Value}) because no runtime definition is bound.",
                        LogLevel.Debug);
                    continue;
                }

                var touchActions = definition.Actions.Where(action => action.TriggerOnTouch).ToList();
                if (touchActions.Count == 0)
                {
                    monitor.Log(
                        $"{TouchActionLogPrefix} Runtime building '{definition.Id}' at ({building.tileX.Value}, {building.tileY.Value}) has no TouchAction entries after normalization.",
                        LogLevel.Debug);
                    continue;
                }

                var localTile = new Point(
                    playerTile.X - building.tileX.Value,
                    playerTile.Y - building.tileY.Value);
                monitor.Log(
                    $"{TouchActionLogPrefix} Evaluating building '{definition.Id}' at ({building.tileX.Value}, {building.tileY.Value}) in '{__instance.NameOrUniqueName}': playerTile=({playerTile.X}, {playerTile.Y}), localTile=({localTile.X}, {localTile.Y}), touchActions={touchActions.Count}.",
                    LogLevel.Debug);

                foreach (var runtimeAction in touchActions)
                {
                    var containsPlayer = runtimeAction.Area.Contains(localTile);
                    monitor.Log(
                        $"{TouchActionLogPrefix} Checked area {FormatRectangle(runtimeAction.Area)} for '{definition.Id}': action='{runtimeAction.Action}', containsPlayer={containsPlayer}.",
                        LogLevel.Debug);
                    if (!containsPlayer)
                        continue;

                    var actionKey = $"{__instance.NameOrUniqueName}:{building.tileX.Value}:{building.tileY.Value}:{runtimeAction.Action}:{playerTile.X}:{playerTile.Y}";
                    if (string.Equals(lastTriggeredTouchActionKey, actionKey, StringComparison.Ordinal))
                    {
                        monitor.Log(
                            $"{TouchActionLogPrefix} Skipped duplicate trigger for '{definition.Id}': action='{runtimeAction.Action}', actionKey='{actionKey}'.",
                            LogLevel.Debug);
                        return;
                    }

                    monitor.Log(
                        $"{TouchActionLogPrefix} Player entered TouchAction area for '{definition.Id}': action='{runtimeAction.Action}', worldTile=({playerTile.X}, {playerTile.Y}), localArea={FormatRectangle(runtimeAction.Area)}.",
                        LogLevel.Debug);
                    if (TryPerformTouchAction(__instance, runtimeAction.Action, playerTile))
                    {
                        lastTriggeredTouchActionKey = actionKey;
                        monitor.Log(
                            $"{TouchActionLogPrefix} Trigger succeeded for '{definition.Id}': action='{runtimeAction.Action}', actionKey='{actionKey}'.",
                            LogLevel.Debug);
                    }
                    else
                    {
                        monitor.Log(
                            $"{TouchActionLogPrefix} Trigger failed for '{definition.Id}': action='{runtimeAction.Action}', worldTile=({playerTile.X}, {playerTile.Y}).",
                            LogLevel.Debug);
                    }

                    return;
                }
            }

            lastTriggeredTouchActionKey = null;
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to process runtime building touch actions for location '{__instance.NameOrUniqueName}': {ex}", LogLevel.Error);
        }
    }

    private static List<Building> GetLocationBuildings(GameLocation location)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var property = location.GetType().GetProperty("buildings", flags) ?? location.GetType().GetProperty("Buildings", flags);
        var field = property is null ? location.GetType().GetField("buildings", flags) ?? location.GetType().GetField("Buildings", flags) : null;
        var value = property?.GetValue(location) ?? field?.GetValue(location);
        if (value is not IEnumerable enumerable)
        {
            monitor.Log(
                $"{TouchActionLogPrefix} Location '{location.NameOrUniqueName}' ({location.GetType().FullName}) does not expose an enumerable buildings collection.",
                LogLevel.Debug);
            return new List<Building>();
        }

        var buildings = new List<Building>();
        foreach (var item in enumerable)
        {
            if (item is Building building)
                buildings.Add(building);
        }

        return buildings;
    }

    private static void ApplyBlueprintState(CarpenterMenu menu, RuntimeBuildingDefinition definition)
    {
        var blueprint = GetMemberValue<object>(menu, "blueprint");
        if (blueprint is null)
            return;

        TrySetIntMember(blueprint, "tilesWide", definition.Size.X);
        TrySetIntMember(blueprint, "tilesHigh", definition.Size.Y);
        TrySetPointLikeMember(blueprint, "humanDoor", new Point((int)definition.Door.X, (int)definition.Door.Y));
        SetMemberValueIfPresent(blueprint, "textureName", ModEntry.Instance.GetPreviewTextureAssetName(definition.Id));
        SetMemberValueIfPresent(blueprint, "Texture", ModEntry.Instance.GetPreviewTextureAssetName(definition.Id));
        TrySetAreaLikeMember(blueprint, "sourceRect", definition.SourceRect ?? definition.Texture.Bounds);
        TrySetAreaLikeMember(blueprint, "SourceRect", definition.SourceRect ?? definition.Texture.Bounds);
        if (definition.AnimalDoor is Rectangle animalDoor)
            TrySetAreaLikeMember(blueprint, "animalDoor", animalDoor);
    }

    private static T? GetMemberValue<T>(object instance, string memberName) where T : class
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property?.GetValue(instance) is T propertyValue)
            return propertyValue;

        var field = type.GetField(memberName, flags);
        return field?.GetValue(instance) as T;
    }

    private static bool TrySetIntMember(object instance, string memberName, int value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignScalarValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignScalarValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static bool TrySetPointLikeMember(object instance, string memberName, Point value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignPointValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignPointValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static bool TrySetAreaLikeMember(object instance, string memberName, Rectangle value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignAreaValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignAreaValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static bool TryPerformTouchAction(GameLocation location, string action, Point playerTile)
    {
        if (string.IsNullOrWhiteSpace(action))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var methods = typeof(GameLocation).GetMethods(flags)
            .Where(method => string.Equals(method.Name, "performTouchAction", StringComparison.Ordinal))
            .ToArray();
        if (methods.Length == 0)
        {
            monitor.Log("Could not find GameLocation.performTouchAction while processing a runtime building TouchAction.", LogLevel.Warn);
            return false;
        }

        var splitAction = action.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tileVector = new Vector2(playerTile.X, playerTile.Y);
        var tileLocation = new XTileLocation(playerTile.X, playerTile.Y);
        monitor.Log(
            $"{TouchActionLogPrefix} Resolving GameLocation.performTouchAction for action='{action}' at worldTile=({playerTile.X}, {playerTile.Y}). OverloadCount={methods.Length}.",
            LogLevel.Debug);

        foreach (var method in methods)
        {
            if (!TryBuildTouchActionArguments(method, action, splitAction, tileVector, tileLocation, out var args))
                continue;

            try
            {
                monitor.Log(
                    $"{TouchActionLogPrefix} Invoking overload '{FormatMethodSignature(method)}' for action='{action}'.",
                    LogLevel.Debug);
                method.Invoke(location, args);
                monitor.Log(
                    $"{TouchActionLogPrefix} GameLocation.performTouchAction completed for action='{action}' using '{FormatMethodSignature(method)}'.",
                    LogLevel.Debug);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                monitor.Log($"Runtime building TouchAction '{action}' threw while invoking GameLocation.performTouchAction: {ex.InnerException ?? ex}", LogLevel.Warn);
                return false;
            }
        }

        monitor.Log($"Could not match a GameLocation.performTouchAction overload for runtime building TouchAction '{action}'.", LogLevel.Warn);
        return false;
    }

    private static bool TryBuildTouchActionArguments(
        MethodInfo method,
        string action,
        string[] splitAction,
        Vector2 tileVector,
        XTileLocation tileLocation,
        out object?[] args)
    {
        var parameters = method.GetParameters();
        args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType == typeof(string))
            {
                args[i] = action;
                continue;
            }

            if (parameterType == typeof(string[]))
            {
                args[i] = splitAction;
                continue;
            }

            if (parameterType == typeof(Vector2))
            {
                args[i] = tileVector;
                continue;
            }

            if (parameterType == typeof(XTileLocation))
            {
                args[i] = tileLocation;
                continue;
            }

            if (parameterType == typeof(Farmer))
            {
                args[i] = Game1.player;
                continue;
            }

            monitor.Log(
                $"{TouchActionLogPrefix} Rejected overload '{FormatMethodSignature(method)}' for action='{action}' because parameter {i} type '{parameterType.FullName}' is unsupported.",
                LogLevel.Debug);
            return false;
        }

        monitor.Log(
            $"{TouchActionLogPrefix} Matched overload '{FormatMethodSignature(method)}' for action='{action}'.",
            LogLevel.Debug);
        return true;
    }

    private static string FormatRectangle(Rectangle rectangle)
    {
        return $"({rectangle.X}, {rectangle.Y}, {rectangle.Width}, {rectangle.Height})";
    }

    private static string FormatMethodSignature(MethodInfo method)
    {
        return $"{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})";
    }

    private static bool TryAssignScalarValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, int intValue)
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

    private static bool TryAssignPointValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, Point pointValue)
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

    private static bool TryAssignAreaValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, Rectangle areaValue)
    {
        var targetInstance = getCurrentValue() ?? Activator.CreateInstance(targetType);
        if (targetInstance is null)
            return false;

        var assignedAny = false;
        assignedAny |= TrySetScalarMember(targetInstance, "X", areaValue.X);
        assignedAny |= TrySetScalarMember(targetInstance, "Y", areaValue.Y);
        assignedAny |= TrySetScalarMember(targetInstance, "Width", areaValue.Width);
        assignedAny |= TrySetScalarMember(targetInstance, "Height", areaValue.Height);

        if (!assignedAny)
            return false;

        assign(targetInstance);
        return true;
    }

    private static bool TrySetScalarMember(object instance, string memberName, int value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null)
        {
            if (TryAssignScalarValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
                return true;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
        {
            if (TryAssignScalarValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
                return true;
        }

        return false;
    }

    private static void SetMemberValueIfPresent(object instance, string memberName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
            field.SetValue(instance, value);
    }

}

[HarmonyPatch]
internal static class CarpenterMenuPatches
{
    private static RuntimeBuildingRegistry Registry => ModEntry.Instance.Registry;
    private static IMonitor Monitor => ModEntry.Instance.Monitor;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(CarpenterMenu))
            .Where(method => string.Equals(method.Name, "SetNewActiveBlueprint", StringComparison.Ordinal));
    }

    [HarmonyPostfix]
    private static void Postfix(CarpenterMenu __instance)
    {
        try
        {
            var previewBuilding = GetMemberValue<Building>(__instance, "currentBuilding");
            if (previewBuilding is null)
                return;

            TryReloadBuildingData(previewBuilding);
            var definition = Registry.Bind(previewBuilding);
            if (definition is null)
                return;

            ApplyBlueprintState(__instance, definition);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to patch CarpenterMenu blueprint state: {ex}", LogLevel.Error);
        }
    }

    private static void TryReloadBuildingData(Building building)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var method = building.GetType().GetMethod("ReloadBuildingData", flags, binder: null, new[] { typeof(bool), typeof(bool) }, modifiers: null);
        if (method is null)
            return;

        method.Invoke(building, new object[] { false, false });
    }

    private static T? GetMemberValue<T>(object instance, string memberName) where T : class
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property?.GetValue(instance) is T propertyValue)
            return propertyValue;

        var field = type.GetField(memberName, flags);
        return field?.GetValue(instance) as T;
    }

    private static void ApplyBlueprintState(CarpenterMenu menu, RuntimeBuildingDefinition definition)
    {
        var blueprint = GetMemberValue<object>(menu, "blueprint");
        if (blueprint is null)
            return;

        TrySetIntMember(blueprint, "tilesWide", definition.Size.X);
        TrySetIntMember(blueprint, "tilesHigh", definition.Size.Y);
        TrySetPointLikeMember(blueprint, "humanDoor", new Point((int)definition.Door.X, (int)definition.Door.Y));
        SetMemberValueIfPresent(blueprint, "textureName", ModEntry.Instance.GetPreviewTextureAssetName(definition.Id));
        SetMemberValueIfPresent(blueprint, "Texture", ModEntry.Instance.GetPreviewTextureAssetName(definition.Id));
        TrySetAreaLikeMember(blueprint, "sourceRect", definition.SourceRect ?? definition.Texture.Bounds);
        TrySetAreaLikeMember(blueprint, "SourceRect", definition.SourceRect ?? definition.Texture.Bounds);
        if (definition.AnimalDoor is Rectangle animalDoor)
            TrySetAreaLikeMember(blueprint, "animalDoor", animalDoor);
    }

    private static bool TrySetIntMember(object instance, string memberName, int value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignScalarValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignScalarValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static bool TrySetPointLikeMember(object instance, string memberName, Point value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignPointValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignPointValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static bool TrySetAreaLikeMember(object instance, string memberName, Rectangle value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && TryAssignAreaValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
            return true;

        var field = type.GetField(memberName, flags);
        if (field is not null && TryAssignAreaValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
            return true;

        return false;
    }

    private static void SetMemberValueIfPresent(object instance, string memberName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
            field.SetValue(instance, value);
    }

    private static bool TryAssignScalarValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, int intValue)
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

    private static bool TryAssignPointValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, Point pointValue)
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

    private static bool TryAssignAreaValue(Type targetType, Func<object?> getCurrentValue, Action<object> assign, Rectangle areaValue)
    {
        var targetInstance = getCurrentValue() ?? Activator.CreateInstance(targetType);
        if (targetInstance is null)
            return false;

        var assignedAny = false;
        assignedAny |= TrySetScalarMember(targetInstance, "X", areaValue.X);
        assignedAny |= TrySetScalarMember(targetInstance, "Y", areaValue.Y);
        assignedAny |= TrySetScalarMember(targetInstance, "Width", areaValue.Width);
        assignedAny |= TrySetScalarMember(targetInstance, "Height", areaValue.Height);

        if (!assignedAny)
            return false;

        assign(targetInstance);
        return true;
    }

    private static bool TrySetScalarMember(object instance, string memberName, int value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null)
        {
            if (TryAssignScalarValue(property.PropertyType, () => property.CanRead ? property.GetValue(instance) : null, v => property.SetValue(instance, v), value))
                return true;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
        {
            if (TryAssignScalarValue(field.FieldType, () => field.GetValue(instance), v => field.SetValue(instance, v), value))
                return true;
        }

        return false;
    }

    private static void MergeBlueprintAdditionalPlacementTiles(object blueprint, RuntimeBuildingDefinition definition)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = blueprint.GetType();

        var property = type.GetProperty("additionalPlacementTiles", flags) ?? type.GetProperty("AdditionalPlacementTiles", flags);
        var field = property is null ? type.GetField("additionalPlacementTiles", flags) ?? type.GetField("AdditionalPlacementTiles", flags) : null;
        var collectionType = property?.PropertyType ?? field?.FieldType;
        if (collectionType is null)
            return;

        var itemType = GetCollectionItemType(collectionType);
        if (itemType is null)
            return;

        var placementTilesValue = property?.GetValue(blueprint) ?? field?.GetValue(blueprint);
        if (placementTilesValue is null)
        {
            var listType = typeof(List<>).MakeGenericType(itemType);
            placementTilesValue = Activator.CreateInstance(listType);
            if (placementTilesValue is null)
                return;

            if (property is not null && property.CanWrite)
                property.SetValue(blueprint, placementTilesValue);
            else if (field is not null)
                field.SetValue(blueprint, placementTilesValue);
            else
                return;
        }

        if (placementTilesValue is not IList placementTiles)
            return;

        var mailboxTiles = GetBlueprintMailboxActionTiles(blueprint);
        RemoveBlueprintMailboxPlacementTiles(placementTiles, mailboxTiles);

        if (definition.Mailbox is not Point mailboxTile || IsInsideFootprint(mailboxTile, definition.Size))
            return;

        placementTiles.Add(CreateBlueprintPlacementTile(itemType, mailboxTile, onlyNeedsToBePassable: false));
    }

    private static HashSet<Point> GetBlueprintMailboxActionTiles(object blueprint)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = blueprint.GetType();
        var property = type.GetProperty("actionTiles", flags) ?? type.GetProperty("ActionTiles", flags);
        var field = property is null ? type.GetField("actionTiles", flags) ?? type.GetField("ActionTiles", flags) : null;
        var actionTilesValue = property?.GetValue(blueprint) ?? field?.GetValue(blueprint);
        if (actionTilesValue is not IList actionTiles)
            return new HashSet<Point>();

        var result = new HashSet<Point>();
        foreach (var item in actionTiles)
        {
            if (item is null)
                continue;

            var action = GetUntypedMemberValue(item, "Action") as string;
            if (!string.Equals(action, "Mailbox", StringComparison.OrdinalIgnoreCase))
                continue;

            var point = TryReadPointLikeMember(item, "Tile");
            if (point is Point tile)
                result.Add(tile);
        }

        return result;
    }

    private static void RemoveBlueprintMailboxPlacementTiles(IList placementTiles, HashSet<Point> mailboxTiles)
    {
        if (mailboxTiles.Count == 0)
            return;

        for (var i = placementTiles.Count - 1; i >= 0; i--)
        {
            var item = placementTiles[i];
            if (item is null)
                continue;

            var tileArea = TryReadRectangleLikeMember(item, "TileArea");
            if (tileArea is not Rectangle area || area.Width != 1 || area.Height != 1)
                continue;

            if (mailboxTiles.Contains(new Point(area.X, area.Y)))
                placementTiles.RemoveAt(i);
        }
    }

    private static object CreateBlueprintPlacementTile(Type itemType, Point tile, bool onlyNeedsToBePassable)
    {
        var instance = Activator.CreateInstance(itemType)
            ?? throw new InvalidOperationException($"Could not create blueprint AdditionalPlacementTiles item for tile '{tile}'.");

        TrySetAreaLikeMember(instance, "TileArea", new Rectangle(tile.X, tile.Y, 1, 1));
        SetScalarMember(instance, "OnlyNeedsToBePassable", onlyNeedsToBePassable);
        return instance;
    }

    private static bool IsInsideFootprint(Point tile, Point size)
    {
        return tile.X >= 0 && tile.Y >= 0 && tile.X < size.X && tile.Y < size.Y;
    }

    private static Type? GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments().FirstOrDefault();

        return collectionType.GetInterfaces()
            .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IList<>))
            ?.GetGenericArguments()
            .FirstOrDefault();
    }

    private static object? GetUntypedMemberValue(object instance, string memberName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();
        var property = type.GetProperty(memberName, flags);
        if (property is not null)
            return property.GetValue(instance);

        var field = type.GetField(memberName, flags);
        return field?.GetValue(instance);
    }

    private static Point? TryReadPointLikeMember(object instance, string memberName)
    {
        var value = GetUntypedMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is Point point)
            return point;

        if (value is Vector2 vector)
            return new Point((int)vector.X, (int)vector.Y);

        var x = TryReadIntLikeMember(value, "X") ?? TryReadIntLikeMember(value, "x");
        var y = TryReadIntLikeMember(value, "Y") ?? TryReadIntLikeMember(value, "y");
        if (x is null || y is null)
            return null;

        return new Point(x.Value, y.Value);
    }

    private static Rectangle? TryReadRectangleLikeMember(object instance, string memberName)
    {
        var value = GetUntypedMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is Rectangle rectangle)
            return rectangle;

        var x = TryReadIntLikeMember(value, "X");
        var y = TryReadIntLikeMember(value, "Y");
        var width = TryReadIntLikeMember(value, "Width");
        var height = TryReadIntLikeMember(value, "Height");
        if (x is null || y is null || width is null || height is null)
            return null;

        return new Rectangle(x.Value, y.Value, width.Value, height.Value);
    }

    private static int? TryReadIntLikeMember(object instance, string memberName)
    {
        var value = GetUntypedMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is int intValue)
            return intValue;

        var valueProperty = value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty?.GetValue(value) is int wrappedInt)
            return wrappedInt;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static void SetScalarMember(object instance, string memberName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(instance, ConvertScalarValue(value, property.PropertyType));
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
            field.SetValue(instance, ConvertScalarValue(value, field.FieldType));
    }

    private static object ConvertScalarValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        var valueProperty = targetType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is not null && valueProperty.CanWrite)
        {
            var targetInstance = Activator.CreateInstance(targetType)
                ?? throw new InvalidOperationException($"Could not create scalar wrapper type '{targetType.FullName}'.");
            valueProperty.SetValue(targetInstance, Convert.ChangeType(value, valueProperty.PropertyType));
            return targetInstance;
        }

        return Convert.ChangeType(value, targetType);
    }
}

[HarmonyPatch]
internal static class BuildingDataPatches
{
    private static RuntimeBuildingRegistry Registry => ModEntry.Instance.Registry;
    private static IMonitor Monitor => ModEntry.Instance.Monitor;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(Building))
            .Where(method => string.Equals(method.Name, "LoadFromBuildingData", StringComparison.Ordinal));
    }

    [HarmonyPrefix]
    private static void Prefix(Building __instance, object[] __args)
    {
        if (__args.Length == 0 || __args[0] is not BuildingData buildingData)
            return;

        var definition = Registry.Get(__instance.buildingType?.Value);
        if (definition is null)
            return;

        ApplyHumanDoor(buildingData, definition);
        ApplyAnimalDoor(buildingData, definition);
        ApplyPreviewTexture(buildingData, definition);
        buildingData.CollisionMap = BuildCollisionMap(definition);
        ApplyChimneyMetadata(buildingData, definition);
        ApplyMaxOccupants(buildingData, definition);
        ApplyAllowsFlooringUnderneath(buildingData, definition);
        MergeDrawLayers(buildingData, definition);
        MergeAdditionalPlacementTiles(buildingData, definition);
        MergeActionTiles(buildingData, definition);
    }

    private static string BuildCollisionMap(RuntimeBuildingDefinition definition)
    {
        var rows = new string[definition.Size.Y];
        for (var y = 0; y < definition.Size.Y; y++)
        {
            var chars = new char[definition.Size.X];
            for (var x = 0; x < definition.Size.X; x++)
                chars[x] = definition.CollisionTiles.Contains(new Point(x, y)) ? 'X' : 'O';

            rows[y] = new string(chars);
        }

        return string.Join('\n', rows);
    }

    private static void MergeActionTiles(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        var property = typeof(BuildingData).GetProperty("ActionTiles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null || !property.CanRead)
            return;

        var actionTilesValue = property.GetValue(buildingData);
        if (actionTilesValue is not IList actionTiles)
            return;

        var itemType = GetCollectionItemType(property.PropertyType);
        if (itemType is null)
            return;

        foreach (var runtimeAction in definition.Actions)
        {
            if (runtimeAction.TriggerOnTouch)
                continue;

            for (var y = runtimeAction.Area.Top; y < runtimeAction.Area.Bottom; y++)
            {
                for (var x = runtimeAction.Area.Left; x < runtimeAction.Area.Right; x++)
                {
                    actionTiles.Add(CreateActionTile(itemType, definition.Id, x, y, runtimeAction.Action));
                }
            }
        }
    }

    private static void MergeAdditionalPlacementTiles(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        var property = typeof(BuildingData).GetProperty("AdditionalPlacementTiles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null || !property.CanRead)
            return;

        var placementTilesValue = property.GetValue(buildingData);
        var itemType = GetCollectionItemType(property.PropertyType);
        if (itemType is null)
            return;

        if (placementTilesValue is null)
        {
            if (!property.CanWrite)
                return;

            var listType = typeof(List<>).MakeGenericType(itemType);
            placementTilesValue = Activator.CreateInstance(listType);
            if (placementTilesValue is null)
                return;

            property.SetValue(buildingData, placementTilesValue);
        }

        if (placementTilesValue is not IList placementTiles)
            return;

        var vanillaMailboxTiles = GetMailboxActionTiles(buildingData);
        RemoveMailboxPlacementTiles(placementTiles, vanillaMailboxTiles);

        if (definition.Mailbox is not Point mailboxTile || IsInsideFootprint(mailboxTile, definition.Size))
            return;

        placementTiles.Add(CreatePlacementTile(itemType, mailboxTile, onlyNeedsToBePassable: false));
    }

    private static void MergeDrawLayers(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        if (definition.BuildingDrawLayers.Count == 0)
            return;

        var property = typeof(BuildingData).GetProperty("DrawLayers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null || !property.CanRead)
            return;

        var drawLayersValue = property.GetValue(buildingData);
        if (drawLayersValue is not IList drawLayers)
            return;

        RemoveAnimalDoorDrawLayers(drawLayers);

        var itemType = GetCollectionItemType(property.PropertyType);
        if (itemType is null)
            return;

        foreach (var runtimeLayer in definition.BuildingDrawLayers)
            drawLayers.Add(CreateDrawLayer(itemType, runtimeLayer));
    }

    private static void ApplyHumanDoor(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        SetPointLikeMember(buildingData, "HumanDoor", new Point((int)definition.Door.X, (int)definition.Door.Y));
    }

    private static void ApplyAnimalDoor(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        if (definition.AnimalDoor is not Rectangle animalDoor)
            return;

        SetAreaLikeMember(buildingData, "AnimalDoor", animalDoor);

        if (definition.AnimalDoorOpenDuration is float openDuration)
            SetScalarMember(buildingData, "AnimalDoorOpenDuration", openDuration);
        if (!string.IsNullOrWhiteSpace(definition.AnimalDoorOpenSound))
            SetMemberValue(buildingData, "AnimalDoorOpenSound", definition.AnimalDoorOpenSound);
        if (definition.AnimalDoorCloseDuration is float closeDuration)
            SetScalarMember(buildingData, "AnimalDoorCloseDuration", closeDuration);
        if (!string.IsNullOrWhiteSpace(definition.AnimalDoorCloseSound))
            SetMemberValue(buildingData, "AnimalDoorCloseSound", definition.AnimalDoorCloseSound);
    }

    private static void ApplyMaxOccupants(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        if (definition.MaxOccupants is not int maxOccupants)
            return;

        SetScalarMember(buildingData, "MaxOccupants", maxOccupants);
    }

    private static void ApplyAllowsFlooringUnderneath(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        if (definition.AllowsFlooringUnderneath is not bool allowsFlooringUnderneath)
            return;

        SetScalarMember(buildingData, "AllowsFlooringUnderneath", allowsFlooringUnderneath);
    }

    private static void ApplyPreviewTexture(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        SetMemberValue(buildingData, "Texture", ModEntry.Instance.GetPreviewTextureAssetName(definition.Id));
        SetScalarMember(buildingData, "TextureSpriteRow", 0);
    }

    private static void ApplyChimneyMetadata(BuildingData buildingData, RuntimeBuildingDefinition definition)
    {
        if (definition.Chimney is not Point chimney)
            return;

        var property = typeof(BuildingData).GetProperty("Metadata", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null || !property.CanRead)
            return;

        var metadataValue = property.GetValue(buildingData);
        if (metadataValue is null)
        {
            var metadataType = property.PropertyType;
            metadataValue = Activator.CreateInstance(metadataType);
            if (metadataValue is null || !property.CanWrite)
                return;

            property.SetValue(buildingData, metadataValue);
        }

        if (metadataValue is not IDictionary metadata)
            return;

        metadata[ResolveChimneyMetadataKey(definition.Id)] = $"{chimney.X} {chimney.Y}";
    }

    private static string ResolveChimneyMetadataKey(string definitionId)
    {
        var suffixStart = definitionId.Length;
        while (suffixStart > 0 && char.IsDigit(definitionId[suffixStart - 1]))
            suffixStart--;

        var suffix = definitionId[suffixStart..];
        if (string.IsNullOrEmpty(suffix) || suffix == "1")
            return "ChimneyPosition";

        return $"ChimneyPosition{suffix}";
    }

    private static Type? GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments().FirstOrDefault();

        return collectionType.GetInterfaces()
            .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IList<>))
            ?.GetGenericArguments()
            .FirstOrDefault();
    }

    private static object CreateActionTile(Type itemType, string definitionId, int x, int y, string action)
    {
        var instance = Activator.CreateInstance(itemType)
            ?? throw new InvalidOperationException($"Could not create ActionTiles item for building '{definitionId}'.");

        SetMemberValue(instance, "Id", $"{definitionId}_{action}_{x}_{y}");
        SetMemberValue(instance, "Action", action);
        SetPointLikeMember(instance, "Tile", new Point(x, y));
        return instance;
    }

    private static object CreateDrawLayer(Type itemType, RuntimeDrawLayer runtimeLayer)
    {
        var instance = Activator.CreateInstance(itemType)
            ?? throw new InvalidOperationException($"Could not create DrawLayers item for '{runtimeLayer.Id}'.");

        SetMemberValue(instance, "Id", runtimeLayer.Id);
        SetMemberValue(instance, "Texture", runtimeLayer.Texture);
        SetAreaLikeMember(instance, "SourceRect", runtimeLayer.SourceRect);
        SetPointLikeMember(instance, "DrawPosition", runtimeLayer.DrawPosition);
        SetScalarMember(instance, "DrawInBackground", runtimeLayer.DrawInBackground);
        SetScalarMember(instance, "SortTileOffset", runtimeLayer.SortTileOffset);
        SetMemberValue(instance, "OnlyDrawIfChestHasContents", null!);
        SetScalarMember(instance, "FrameDuration", runtimeLayer.FrameDuration);
        SetScalarMember(instance, "FrameCount", runtimeLayer.FrameCount);
        SetScalarMember(instance, "FramesPerRow", runtimeLayer.FramesPerRow);
        SetPointLikeMember(instance, "AnimalDoorOffset", runtimeLayer.AnimalDoorOffset);
        return instance;
    }

    private static object CreatePlacementTile(Type itemType, Point tile, bool onlyNeedsToBePassable)
    {
        var instance = Activator.CreateInstance(itemType)
            ?? throw new InvalidOperationException($"Could not create AdditionalPlacementTiles item for tile '{tile}'.");

        SetAreaLikeMember(instance, "TileArea", new Rectangle(tile.X, tile.Y, 1, 1));
        SetScalarMember(instance, "OnlyNeedsToBePassable", onlyNeedsToBePassable);
        return instance;
    }

    private static void RemoveAnimalDoorDrawLayers(IList drawLayers)
    {
        for (var i = drawLayers.Count - 1; i >= 0; i--)
        {
            var item = drawLayers[i];
            if (item is null)
                continue;

            var id = GetMemberValue(item, "Id") as string;
            if (id is not null && id.Contains("AnimalDoor", StringComparison.OrdinalIgnoreCase))
                drawLayers.RemoveAt(i);
        }
    }

    private static HashSet<Point> GetMailboxActionTiles(BuildingData buildingData)
    {
        var mailboxTiles = new HashSet<Point>();
        var property = typeof(BuildingData).GetProperty("ActionTiles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.GetValue(buildingData) is not IList actionTiles)
            return mailboxTiles;

        foreach (var item in actionTiles)
        {
            if (item is null)
                continue;

            var action = GetMemberValue(item, "Action") as string;
            if (!string.Equals(action, "Mailbox", StringComparison.OrdinalIgnoreCase))
                continue;

            var point = TryReadPointLikeMember(item, "Tile");
            if (point is Point tile)
                mailboxTiles.Add(tile);
        }

        return mailboxTiles;
    }

    private static void RemoveMailboxPlacementTiles(IList placementTiles, HashSet<Point> mailboxTiles)
    {
        if (mailboxTiles.Count == 0)
            return;

        for (var i = placementTiles.Count - 1; i >= 0; i--)
        {
            var item = placementTiles[i];
            if (item is null)
                continue;

            var tileArea = TryReadRectangleLikeMember(item, "TileArea");
            if (tileArea is not Rectangle area || area.Width != 1 || area.Height != 1)
                continue;

            if (mailboxTiles.Contains(new Point(area.X, area.Y)))
                placementTiles.RemoveAt(i);
        }
    }

    private static bool IsInsideFootprint(Point tile, Point size)
    {
        return tile.X >= 0 && tile.Y >= 0 && tile.X < size.X && tile.Y < size.Y;
    }

    private static void SetMemberValue(object instance, string memberName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
        {
            field.SetValue(instance, value);
            return;
        }
    }

    private static object? GetMemberValue(object instance, string memberName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null)
            return property.GetValue(instance);

        var field = type.GetField(memberName, flags);
        return field?.GetValue(instance);
    }

    private static Point? TryReadPointLikeMember(object instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is Point point)
            return point;

        if (value is Vector2 vector)
            return new Point((int)vector.X, (int)vector.Y);

        var x = TryReadIntLikeMember(value, "X") ?? TryReadIntLikeMember(value, "x");
        var y = TryReadIntLikeMember(value, "Y") ?? TryReadIntLikeMember(value, "y");
        if (x is null || y is null)
            return null;

        return new Point(x.Value, y.Value);
    }

    private static Rectangle? TryReadRectangleLikeMember(object instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is Rectangle rectangle)
            return rectangle;

        var x = TryReadIntLikeMember(value, "X");
        var y = TryReadIntLikeMember(value, "Y");
        var width = TryReadIntLikeMember(value, "Width");
        var height = TryReadIntLikeMember(value, "Height");
        if (x is null || y is null || width is null || height is null)
            return null;

        return new Rectangle(x.Value, y.Value, width.Value, height.Value);
    }

    private static int? TryReadIntLikeMember(object instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        if (value is null)
            return null;

        if (value is int intValue)
            return intValue;

        var valueProperty = value.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty?.GetValue(value) is int wrappedInt)
            return wrappedInt;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return null;
        }
    }

    private static void SetPointLikeMember(object instance, string memberName, Point pointValue)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            if (property.PropertyType == typeof(Point))
            {
                property.SetValue(instance, pointValue);
                return;
            }

            var pointInstance = Activator.CreateInstance(property.PropertyType);
            if (pointInstance is not null)
            {
                SetMemberValue(pointInstance, "X", pointValue.X);
                SetMemberValue(pointInstance, "Y", pointValue.Y);
                property.SetValue(instance, pointInstance);
            }

            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is null)
            return;

        if (field.FieldType == typeof(Point))
        {
            field.SetValue(instance, pointValue);
            return;
        }

        var fieldPointInstance = Activator.CreateInstance(field.FieldType);
        if (fieldPointInstance is null)
            return;

        SetMemberValue(fieldPointInstance, "X", pointValue.X);
        SetMemberValue(fieldPointInstance, "Y", pointValue.Y);
        field.SetValue(instance, fieldPointInstance);
    }

    private static void SetAreaLikeMember(object instance, string memberName, Rectangle areaValue)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            var areaInstance = Activator.CreateInstance(property.PropertyType);
            if (areaInstance is null)
                return;

            SetScalarMember(areaInstance, "X", areaValue.X);
            SetScalarMember(areaInstance, "Y", areaValue.Y);
            SetScalarMember(areaInstance, "Width", areaValue.Width);
            SetScalarMember(areaInstance, "Height", areaValue.Height);
            property.SetValue(instance, areaInstance);
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is null)
            return;

        var fieldAreaInstance = Activator.CreateInstance(field.FieldType);
        if (fieldAreaInstance is null)
            return;

        SetScalarMember(fieldAreaInstance, "X", areaValue.X);
        SetScalarMember(fieldAreaInstance, "Y", areaValue.Y);
        SetScalarMember(fieldAreaInstance, "Width", areaValue.Width);
        SetScalarMember(fieldAreaInstance, "Height", areaValue.Height);
        field.SetValue(instance, fieldAreaInstance);
    }

    private static void SetScalarMember(object instance, string memberName, object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = instance.GetType();

        var property = type.GetProperty(memberName, flags);
        if (property is not null && property.CanWrite)
        {
            property.SetValue(instance, ConvertScalarValue(value, property.PropertyType));
            return;
        }

        var field = type.GetField(memberName, flags);
        if (field is not null)
            field.SetValue(instance, ConvertScalarValue(value, field.FieldType));
    }

    private static object ConvertScalarValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        var valueProperty = targetType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valueProperty is not null && valueProperty.CanWrite)
        {
            var targetInstance = Activator.CreateInstance(targetType)
                ?? throw new InvalidOperationException($"Could not create scalar wrapper type '{targetType.FullName}'.");
            valueProperty.SetValue(targetInstance, Convert.ChangeType(value, valueProperty.PropertyType));
            return targetInstance;
        }

        return Convert.ChangeType(value, targetType);
    }
}

internal static class BuildingGetDataPatches
{
    private static RuntimeBuildingRegistry Registry => ModEntry.Instance.Registry;

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(Building))
            .Where(method => string.Equals(method.Name, "getData", StringComparison.Ordinal)
                && method.ReturnType == typeof(BuildingData)
                && method.GetParameters().Length == 0);
    }

    internal static void Postfix(Building __instance, ref BuildingData? __result)
    {
        if (__result is not null)
            return;

        var definition = Registry.Get(__instance.buildingType?.Value);
        if (definition is null)
            return;

        __result = ModEntry.Instance.CreateSyntheticBuildingData(definition);
    }
}

[HarmonyPatch]
internal static class BuildingExitPatches
{
    private static RuntimeBuildingRegistry Registry => ModEntry.Instance.Registry;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(Building))
            .Where(method => string.Equals(method.Name, "getPointForHumanDoor", StringComparison.Ordinal));
    }

    [HarmonyPostfix]
    private static void Postfix(Building __instance, ref Point __result)
    {
        var definition = Registry.Get(__instance);
        if (definition?.Exit is not Point exit)
            return;

        __result = exit;
    }
}

internal static class AdditionalPlacementTilesPatches
{
    private static RuntimeBuildingRegistry Registry => ModEntry.Instance.Registry;

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(Building))
            .Where(method => string.Equals(method.Name, "GetAdditionalPlacementTiles", StringComparison.Ordinal)
                && method.GetParameters().Length == 0);
    }

    internal static void Postfix(Building __instance, ref IEnumerable<BuildingPlacementTile> __result)
    {
        var definition = Registry.Get(__instance);
        if (definition?.Mailbox is not Point mailboxTile)
            return;

        if (!string.Equals(__instance.buildingType?.Value, "Farmhouse", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(__instance.buildingType?.Value, "Cabin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var placementTiles = (__result ?? Enumerable.Empty<BuildingPlacementTile>()).ToList();
        placementTiles.RemoveAll(tile => IsVanillaMailboxPlacement(tile, definition.Size, mailboxTile));

        if (!IsInsideFootprint(mailboxTile, definition.Size) && !placementTiles.Any(tile => MatchesTile(tile, mailboxTile)))
            placementTiles.Add(CreatePlacementTile(mailboxTile, onlyNeedsToBePassable: false));

        __result = placementTiles;
    }

    private static bool IsVanillaMailboxPlacement(BuildingPlacementTile tile, Point footprintSize, Point runtimeMailboxTile)
    {
        if (tile is null)
            return false;

        var area = tile.TileArea;
        if (area.Width != 1 || area.Height != 1)
            return false;

        var point = new Point(area.X, area.Y);
        if (point == runtimeMailboxTile)
            return false;

        return !IsInsideFootprint(point, footprintSize) && !tile.OnlyNeedsToBePassable;
    }

    private static bool MatchesTile(BuildingPlacementTile tile, Point point)
    {
        var area = tile.TileArea;
        return area.Width == 1 && area.Height == 1 && area.X == point.X && area.Y == point.Y;
    }

    private static bool IsInsideFootprint(Point tile, Point size)
    {
        return tile.X >= 0 && tile.Y >= 0 && tile.X < size.X && tile.Y < size.Y;
    }

    private static BuildingPlacementTile CreatePlacementTile(Point tile, bool onlyNeedsToBePassable)
    {
        return new BuildingPlacementTile
        {
            TileArea = new Rectangle(tile.X, tile.Y, 1, 1),
            OnlyNeedsToBePassable = onlyNeedsToBePassable,
        };
    }
}
