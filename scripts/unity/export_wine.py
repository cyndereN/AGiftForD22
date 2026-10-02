"""Publish the original web GLB bottle as an upright, 42 cm Unity FBX."""
import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
out=root/'unity/D22Game/Assets/D22/Art/Props/Wine'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root/'legacy/web/Model/Wine.glb'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
print('WINE_BOUNDS',list(low),list(high))
# glTF is imported by Blender into Z-up; publish with the long axis vertical.
size=high-low
axis=max(range(3),key=lambda i:size[i])
from mathutils import Matrix
from math import pi
rotation=Matrix.Rotation(pi/2,4,'Y') if axis==0 else Matrix.Rotation(pi/2,4,'X') if axis==1 else Matrix.Identity(4)
for o in meshes:o.matrix_world=rotation@o.matrix_world
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
low=Vector(tuple(min(p[i] for p in points) for i in range(3)));high=Vector(tuple(max(p[i] for p in points) for i in range(3)))
center=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z));scale=.42/(high.z-low.z)
for o in meshes:
 o.matrix_world=Matrix.Scale(scale,4)@Matrix.Translation(-center)@o.matrix_world
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'wine.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,use_mesh_modifiers=True,path_mode='STRIP')
for image in bpy.data.images:
 if image.size[0]>0:
  image.filepath_raw=str(out/'Textures'/('glb-'+image.name.split('.')[0]+'.png'));image.file_format='PNG';image.save()
print('WINE_PUBLISHED',len(meshes))
