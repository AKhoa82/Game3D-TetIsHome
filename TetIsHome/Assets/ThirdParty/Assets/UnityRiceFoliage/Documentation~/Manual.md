# Manual

## What the generator creates

`Tools > Rice Foliage > Generate Editable Assets` creates:

- `Meshes/`: crossed-card LOD0 and reduced crossed-card LOD1 meshes.
- `Materials/`: one material for each growth stage.
- `Prefabs/`: one LOD prefab for each stage and a demo instanced field.
- `RiceFoliageLibrary.asset`: references used by runtime components.

All generated assets are written to
`Assets/_Project/Art/Vegetation/Rice`. Source textures, shader, and scripts are
loaded from `Assets/ThirdParty/UnityRiceFoliage`.

## Material controls

- **Base Color**: overall tint.
- **Alpha Cutoff**: increase to reduce halos; decrease to retain fine leaves.
- **Smoothness**: keep low for natural foliage.
- **Wind Strength / Speed / Scale**: local material wind settings.

When a `RiceWindController` exists, its global wind values override the
material wind values.

## Growth stages

The asset maps its five textures to FS-style IDs `01`, `02`, `03`, `04`, and
`06`. Stage `05` was absent from the downloaded source package. Runtime stage
indices are continuous from `0` to `4`.

## Performance

The demo field uses `Graphics.DrawMeshInstanced` in batches of 1023 instances.
It is intended as a compact starting point. For extremely large fields, replace
it with a project-specific indirect-instancing or terrain-detail solution.

## Built-in RP and HDRP

The included shader targets URP. Meshes and textures remain reusable, but the
materials need a shader replacement in Built-in RP or HDRP.
