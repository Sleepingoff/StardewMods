# Buildings Tiled Framework

SMAPI mod for overriding Stardew Valley buildings at runtime from Tiled `.tmx` files.

This framework does not patch `Data/Buildings` through content patching.
It loads runtime definitions from dependent content packs and applies them with Harmony.

## Features

- Runtime building definitions loaded from TMX
- Runtime texture replacement
- Runtime action tiles from object layers
- Runtime `HumanDoor` override
- Runtime `AnimalDoor` override
- Runtime `CollisionMap` generation from TMX
- Seasonal building variants
- GMCM conflict selection when multiple content packs replace the same building

## Content Pack Structure

Only content packs which declare this mod in `ContentPackFor` are scanned.

```text
[YourPack]/
├── manifest.json
├── Assets/
│   └── Farmhouse.png
└── Buildings/
    ├── Coop.tmx
    ├── Barn.tmx
    ├── Farmhouse.tmx
    ├── Cabin.tmx
    ├── spring/
    │   └── Farmhouse.tmx
    ├── summer/
    │   └── Farmhouse.tmx
    ├── fall/
    │   └── Farmhouse.tmx
    └── winter/
        └── Farmhouse.tmx
```

## Manifest Example

```json
{
  "Name": "Example Building Pack",
  "Author": "YourName",
  "Version": "1.0.0",
  "Description": "Runtime building overrides for Buildings Tiled Framework.",
  "UniqueID": "YourName.ExampleBuildingPack",
  "ContentPackFor": {
    "UniqueID": "slO.BuildingsTiledFramework"
  }
}
```

## File Naming

If `id` is not set in TMX root properties, the file name is used.

Examples:

- `Coop.tmx` -> `Coop`
- `Barn.tmx` -> `Barn`
- `Farmhouse.tmx` -> `FarmHouse`
- `Cabin.tmx` -> `Cabin`

Upgrade ordering is based on a numeric suffix.

Examples:

- `Coop.tmx`
- `Coop1.tmx`
- `Coop2.tmx`
- `Coop3.tmx`

These are loaded in ascending numeric order.

## Seasonal Variants

Seasonal TMX files must exist in all four folders.

```text
Buildings/spring/Farmhouse.tmx
Buildings/summer/Farmhouse.tmx
Buildings/fall/Farmhouse.tmx
Buildings/winter/Farmhouse.tmx
```

If one season is missing, that seasonal building set is skipped.

## Supported TMX Subset

Supported:

- Orthogonal maps
- CSV tile layer data
- Single tileset image
- Object layers with `Action` and `Role`
- Root properties described below

Not supported:

- Infinite maps
- Full Tiled feature coverage
- Arbitrary vanilla `BuildingData` replacement

## Root Properties

Put these under `<map><properties>`.

Optional properties:

- `id`
  - Explicit building ID
- `doorX`
  - Legacy `HumanDoor` X tile
- `doorY`
  - Legacy `HumanDoor` Y tile
- `exitX`
  - Outside exit tile X used by human-door exit logic
- `exitY`
  - Outside exit tile Y used by human-door exit logic
- `maxOccupants`
  - Runtime `BuildingData.MaxOccupants`

Example:

```xml
<properties>
  <property name="id" value="Barn"/>
  <property name="doorX" type="int" value="1"/>
  <property name="doorY" type="int" value="3"/>
  <property name="exitX" type="int" value="64"/>
  <property name="exitY" type="int" value="15"/>
  <property name="maxOccupants" type="int" value="8"/>
</properties>
```

Prefer `Role=HumanDoor` over `doorX` / `doorY`.

## Tileset Image

The tileset image comes from the TMX `<tileset><image source="...">` path.

Example:

```xml
<tileset firstgid="1" name="Barn" tilewidth="16" tileheight="16" tilecount="64" columns="16">
  <image source="../../Assets/Barn.png" width="256" height="64"/>
</tileset>
```

The path is resolved relative to the TMX file and must stay inside the content pack.

## Tile Layers

Layer grouping is resolved like this:

1. If the layer name is `Buildings`, `Front`, or `AlwaysFront`, that name is used.
2. Otherwise, the optional layer property `Draw` is used.
3. If neither applies, the layer name is used as-is.

Supported draw groups:

- `Buildings`
- `Front`
- `AlwaysFront`

Example:

```xml
<layer name="Base">
  <properties>
    <property name="Draw" type="string" value="Buildings"/>
  </properties>
</layer>

<layer name="RoofFront">
  <properties>
    <property name="Draw" type="string" value="Front"/>
  </properties>
</layer>

<layer name="TreeTop">
  <properties>
    <property name="Draw" type="string" value="AlwaysFront"/>
  </properties>
</layer>
```

## Collision Rules

Collision comes only from layers resolved as `Buildings`.

Rules:

- `gid == 0` -> passable
- `gid > 0` -> blocked
- `Back`, `Front`, and `AlwaysFront` do not add collision
- runtime action tiles are cleared back to passable
- `HumanDoor` is cleared back to passable
- `AnimalDoor` is cleared back to passable

The resulting map is injected into runtime `BuildingData.CollisionMap`.

## Placement Footprint

The placement and move footprint is derived from occupied tiles in these layer names only:

- `Back`
- `Buildings`

`Front` and `AlwaysFront` do not expand the move/placement footprint.

## Object Layers

Any object layer name is accepted. `Actions` is recommended.

Object coordinates are pixel-based and are converted into local building tile coordinates using `tilewidth` and `tileheight`.

### Common Object Properties

| Property | Type     | Required | Meaning                                          |
| -------- | -------- | -------- | ------------------------------------------------ |
| `Role`   | `string` | No       | Structural meaning for the object                |
| `Action` | `string` | No       | Stardew action string or reserved runtime action |

An object with neither `Role` nor `Action` is ignored.

### Supported `Role` Values

| Role               | Meaning                                                   |
| ------------------ | --------------------------------------------------------- |
| `HumanDoor`        | Sets `BuildingData.HumanDoor`                             |
| `AnimalDoor`       | Sets `BuildingData.AnimalDoor` area                       |
| `AnimalDoorClosed` | Adds a closed animal-door `DrawLayers` entry              |
| `AnimalDoorOpen`   | Adds an open animal-door `DrawLayers` entry               |
| `Chimney`          | Writes chimney metadata from pixel position               |
| `Mailbox`          | Marks mailbox tile and injects `Mailbox` action if needed |

### Supported `Action` Values

| Action                                                               | Meaning                                |
| -------------------------------------------------------------------- | -------------------------------------- |
| `Warp FarmHouse 3 11`                                                | Normal Stardew action string example   |
| `ClubCard`                                                           | Mapped internally to `LumberPile`      |
| `AnimalDoor <openDuration> <openSound> <closeDuration> <closeSound>` | Sets animal-door timing and sound data |

### Animal Door Draw Object Properties

These are used only on `Role=AnimalDoorClosed` and `Role=AnimalDoorOpen`.
Use tile objects. The selected tile image and object size are used as the source sprite automatically.

| Property            | Type     | Meaning                                  |
| ------------------- | -------- | ---------------------------------------- |
| `SortTileOffset`    | `float`  | Draw order adjustment                    |
| `DrawOffsetX`       | `int`    | X offset from the `AnimalDoor` base draw position |
| `DrawOffsetY`       | `int`    | Y offset from the `AnimalDoor` base draw position |
| `AnimalDoorOffsetX` | `int`    | `AnimalDoorOffset.X`                     |
| `AnimalDoorOffsetY` | `int`    | `AnimalDoorOffset.Y`                     |

Legacy `SourceX`, `SourceY`, `SourceWidth`, `SourceHeight` still work as fallback, but tile objects are the intended format.

## Human Door

Use a one-tile object with `Role=HumanDoor`.

Example:

```xml
<objectgroup name="Actions">
  <object x="16" y="48" width="16" height="16">
    <properties>
      <property name="Role" type="string" value="HumanDoor"/>
      <property name="Action" type="string" value="Warp FarmHouse 3 11"/>
    </properties>
  </object>
</objectgroup>
```

This does two things:

- sets `BuildingData.HumanDoor`
- creates a runtime interaction tile that runs the warp action

## Animal Door

Use an object with `Role=AnimalDoor` to define the door tile area.

If you also want door timing and sound settings, add:

```text
Action = AnimalDoor <openDuration> <openSound> <closeDuration> <closeSound>
```

Example for a barn:

```xml
<objectgroup name="Actions">
  <object x="48" y="48" width="32" height="16">
    <properties>
      <property name="Role" type="string" value="AnimalDoor"/>
      <property name="Action" type="string" value="AnimalDoor 0.4 doorCreak 0.6 doorCreakReverse"/>
    </properties>
  </object>
</objectgroup>
```

This sets:

- `BuildingData.AnimalDoor`
- `BuildingData.AnimalDoorOpenDuration`
- `BuildingData.AnimalDoorOpenSound`
- `BuildingData.AnimalDoorCloseDuration`
- `BuildingData.AnimalDoorCloseSound`

Notes:

- the object rectangle is the full animal-door area
- this reserved `AnimalDoor ...` action is not added as a normal runtime `doAction` tile

### Animal Door Draw Layers

Animal-door sprites can also be authored through object layers.
Each object becomes one runtime `BuildingData.DrawLayers` entry.

Example matching a coop-style animal door:

```xml
<objectgroup name="Actions">
  <object x="0" y="0" width="16" height="16">
    <properties>
      <property name="Role" type="string" value="AnimalDoorClosed"/>
      <property name="SortTileOffset" type="float" value="0.02"/>
      <property name="AnimalDoorOffsetX" type="int" value="0"/>
      <property name="AnimalDoorOffsetY" type="int" value="-13"/>
    </properties>
  </object>

  <object x="0" y="0" width="16" height="16">
    <properties>
      <property name="Role" type="string" value="AnimalDoorOpen"/>
      <property name="SortTileOffset" type="float" value="1.0"/>
      <property name="AnimalDoorOffsetX" type="int" value="0"/>
      <property name="AnimalDoorOffsetY" type="int" value="0"/>
    </properties>
  </object>
</objectgroup>
```

If runtime animal-door draw layers are present, vanilla animal-door draw layers are removed and replaced.

## Mailbox

For `FarmHouse` and `Cabin`, mailbox behavior uses a mailbox marker object.

Use either:

- `Role=Mailbox`
- `Action=Mailbox`
- or both on the same object

Example:

```xml
<objectgroup name="Actions">
  <object x="144" y="64" width="16" height="16">
    <properties>
      <property name="Role" type="string" value="Mailbox"/>
    </properties>
  </object>
</objectgroup>
```

This does one thing:

- injects a `Mailbox` action tile into runtime `BuildingData.ActionTiles`

## Chimney

Use `Role=Chimney`.

The chimney uses the object's pixel position, not tile coordinates.

Example:

```xml
<objectgroup name="Actions">
  <object x="125" y="8" width="16" height="16">
    <properties>
      <property name="Role" type="string" value="Chimney"/>
    </properties>
  </object>
</objectgroup>
```

This writes runtime `BuildingData.Metadata` values like:

```text
ChimneyPosition = "125 8"
```

If the building ID ends in a number, the framework writes `ChimneyPositionN`.

## Club Card / Lumber Pile

Use `Action=ClubCard`.

Example:

```xml
<objectgroup name="Actions">
  <object x="16" y="32" width="16" height="16">
    <properties>
      <property name="Action" type="string" value="ClubCard"/>
    </properties>
  </object>
</objectgroup>
```

This is mapped to the vanilla `LumberPile` action.

## Full Example

```xml
<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.12.0" orientation="orthogonal" renderorder="right-down" width="7" height="4" tilewidth="16" tileheight="16" infinite="0">
  <tileset firstgid="1" name="Barn" tilewidth="16" tileheight="16" tilecount="64" columns="16">
    <image source="../../Assets/Barn.png" width="256" height="64"/>
  </tileset>

  <properties>
    <property name="maxOccupants" type="int" value="8"/>
  </properties>

  <layer name="Back" width="7" height="4">
    <data encoding="csv">
0,0,0,0,0,0,0,
0,0,0,0,0,0,0,
0,0,0,0,0,0,0,
0,1,1,1,1,1,0
</data>
  </layer>

  <layer name="Buildings" width="7" height="4">
    <data encoding="csv">
0,0,0,0,0,0,0,
1,1,1,1,1,1,1,
1,1,1,1,1,1,1,
1,1,1,1,1,1,1
</data>
  </layer>

  <layer name="RoofFront" width="7" height="4">
    <properties>
      <property name="Draw" type="string" value="Front"/>
    </properties>
    <data encoding="csv">
0,0,0,0,0,0,0,
0,0,0,0,0,0,0,
0,0,0,0,0,0,0,
0,0,0,0,0,0,0
</data>
  </layer>

  <objectgroup name="Actions">
    <object x="16" y="48" width="16" height="16">
      <properties>
        <property name="Role" type="string" value="HumanDoor"/>
        <property name="Action" type="string" value="Warp Barn 9 7"/>
      </properties>
    </object>

    <object x="48" y="48" width="32" height="16">
      <properties>
        <property name="Role" type="string" value="AnimalDoor"/>
        <property name="Action" type="string" value="AnimalDoor 0.4 doorCreak 0.6 doorCreakReverse"/>
      </properties>
    </object>
  </objectgroup>
</map>
```

## GMCM

If multiple dependent content packs provide the same building ID:

- all definitions are registered as candidates
- GMCM lets you pick which pack is active for that building ID
- the selection is stored in this mod's config

GMCM is optional. If it is not installed, the first active definition stays selected.

## Logs

Normal diagnostic spam has been removed.

The mod now mainly logs:

- content load failures
- seasonal set problems
- runtime patch errors
- important warnings such as failing to apply `humanDoor`

## Known Limits

- This is still a limited TMX subset, not a full Tiled runtime
- Building UI replacement is partial and focused on runtime behavior
- Some vanilla systems still use their own special-case logic and may require extra Harmony patches
