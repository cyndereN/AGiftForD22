import bpy, math, json
from mathutils import Vector
from pathlib import Path
R=Path(__file__).resolve().parents[2];O=R/'blender/verification/hutong-outdoor-v4/assets';O.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=True;s.render.resolution_x=600;s.render.resolution_y=600;s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('Studio');s.world.use_nodes=True;s.world.node_tree.nodes.get('Background').inputs[1].default_value=.6
for p in sorted((R/'assets/hutong/meshy-v4').glob('*.glb')):
 for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.ops.import_scene.gltf(filepath=str(p));bpy.context.view_layer.update();mm=[o for o in bpy.data.objects if o.type=='MESH'];pts=[o.matrix_world@Vector(v) for o in mm for v in o.bound_box];lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)]);cen=(lo+hi)/2;ext=hi-lo
 print(p.name,'BOUNDS',list(ext))
 bpy.ops.object.camera_add();cam=bpy.context.object;cam.location=cen+Vector((1.5,-2.5,1.4))*max(ext);cam.rotation_euler=(cen-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(ext)*1.6;s.camera=cam
 bpy.ops.object.light_add(type='AREA',location=cen+Vector((-2,-3,4))*max(ext));l=bpy.context.object;l.data.energy=450;l.data.size=3;l.rotation_euler=(cen-l.location).to_track_quat('-Z','Y').to_euler()
 s.render.filepath=str(O/(p.stem+'.png'));bpy.ops.render.render(write_still=True)
