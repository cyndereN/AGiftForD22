"""Normalize the Meshy pickup transform without creating or editing geometry."""
import bpy
import math
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.name = 'D22_RecordShop_MeshyBottle'
source = ROOT / 'assets/recordshop/v3/meshy/RS311-BeerBottle.glb'
bpy.ops.import_scene.gltf(filepath=str(source))
objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']

def bounds():
    bpy.context.view_layer.update()
    points = [o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    return [Vector([f(p[i] for p in points) for i in range(3)]) for f in (min, max)]

lo, hi = bounds()
axis = max(range(3), key=lambda i: (hi-lo)[i])
rotation = Matrix.Rotation(math.pi/2, 4, 'Y' if axis == 0 else 'X') if axis != 2 else Matrix.Identity(4)
for o in objects:
    o.matrix_world = rotation @ o.matrix_world
lo, hi = bounds()
center = Vector(((lo.x+hi.x)/2, (lo.y+hi.y)/2, lo.z))
transform = Matrix.Scale(.29/(hi.z-lo.z), 4) @ Matrix.Translation(-center)
for i, o in enumerate(objects):
    o.matrix_world = transform @ o.matrix_world
    o.name = f'MeshyBeerBottle_{i}'
    o['source_asset'] = str(source.relative_to(ROOT))
    o['meshy_asset'] = True
    for material in o.data.materials:
        if material:
            material.name = 'RS311_' + material.name
lo, hi = bounds()
assert abs((hi-lo).z-.29) < .001
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'blender/source/D22_RecordShop_MeshyBottle.blend'))
print('MESHY_BOTTLE_READY', list(lo), list(hi))
