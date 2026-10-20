# Livehouse Blender export

The Blender source is `blender/source/D22_Balanced_Lighting_v18.blend`. From the
repository root, export only changed FBX groups while refreshing the manifest:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b blender/source/D22_Balanced_Lighting_v18.blend --python scripts/unity/export_d22.py -- --groups Architecture
```

Omit `--groups Architecture` when geometry in the other groups has changed.
The script reads the `.blend` without saving it and writes models, textures,
and `assets/D22/Art/Data/d22-export.json`. In Unity, use
**D22 > Publish > Update Art From Blender** to rebuild the scene and run its
placement, material, lighting, and boundary checks.
