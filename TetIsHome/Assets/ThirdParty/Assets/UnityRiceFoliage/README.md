# Rice Foliage URP

A project-local Unity asset containing five rice growth stages,
an alpha-clipped URP foliage shader, wind animation, generated LOD prefabs, and
a simple GPU-instanced field renderer.

## Requirements

- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP)

## Setup

Keep this folder at `Assets/ThirdParty/UnityRiceFoliage`, then run
**Tools > Rice Foliage > Generate Editable Assets**.

The generator creates editable meshes, materials, prefabs, a foliage library,
and a demo field prefab under `Assets/_Project/Art/Vegetation/Rice`.

## Quick use

- Drag one of the generated `Rice_StageXX.prefab` files into a scene.
- For a larger field, drag `Rice_DemoField.prefab` into a scene and adjust
  `Count`, `Field Size`, `Stage Index`, and the random scale range.
- Add `RiceWindController` once to the scene to override global wind values.

## Regeneration

Running the generator again updates its generated meshes/materials/prefabs.
Do not keep manual changes inside `Assets/_Project/Art/Vegetation/Rice`;
duplicate an asset elsewhere first if you want to customize it.

## Texture notes

The downloaded FS22 replacement textures require separate alpha/normal maps
from the base game. Those maps were not present in the source folder. For a
self-contained asset, the ready-to-use stage textures are newly generated
transparent foliage cutouts. Converted source diffuse textures are retained in
`Runtime/Textures/Original` for reference and are not used by default.

See `Documentation~/Manual.md` for more details.
