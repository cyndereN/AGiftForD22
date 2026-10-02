# Blender source

`source/D22_Balanced_Lighting_v18.blend` is the current authoritative D-22
livehouse scene. It contains the measured structure, physical fixtures, stage,
gallery and bar layout used for the Unity migration. `verification/v18.json`
records the lighting review and the render checks that produced this version.

Blender remains the owner of room geometry, layout, UVs and source materials.
Unity owns runtime materials, collisions, probes, baked lighting, cameras and
gameplay. Do not edit the `.blend` inside the Unity project.

To publish a new revision from the repository root:

```sh
BLENDER_BIN=/Applications/Blender.app/Contents/MacOS/Blender \
  scripts/unity/export_d22.sh
```

The export writes deterministic FBX, textures and a manifest to
`unity/D22Game/Assets/D22/Art/`. Commit the Blender source, generated Art and
the validation report together. Blender backup files (`*.blend1`) stay local.
