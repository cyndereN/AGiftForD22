"""Export the v2 record-shop scene into Unity-ready FBX groups and a manifest."""
import bpy, json, hashlib, re
from pathlib import Path
from mathutils import Vector

ROOT=Path('/Users/yadongliu/WorkSpace/AGiftForD22')
DEST=ROOT/'unity/D22Game/Assets/D22/Art/RecordShopV2'
for name in ['Models','Textures','Data']:
    (DEST/name).mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.get('D22_RecordShop_v2') or bpy.context.scene
bpy.context.window.scene=scene

def ident(s): return re.sub(r'[^A-Za-z0-9_-]+','_',s).strip('_')[:64]+'_'+hashlib.sha1(s.encode()).hexdigest()[:8]

# Copy packed images into the Unity folder. Blender's packed image bytes are authoritative.
images={}
for img in bpy.data.images:
    if not img.packed_file: continue
    data=bytes(img.packed_file.data)
    ext='.png' if data[:8]==b'\x89PNG\r\n\x1a\n' else '.jpg' if data[:2]==b'\xff\xd8' else '.png'
    path=DEST/'Textures'/(ident(img.name)+ext); path.write_bytes(data)
    images[img.name]='Assets/D22/Art/RecordShopV2/Textures/'+path.name

def objects_in(c):
    out=[]
    for o in c.objects: out.append(o)
    for child in c.children: out += objects_in(child)
    return [o for o in out if o.type in {'MESH','CURVE','FONT','SURFACE'}]

groups=[]
for c in scene.collection.children:
    if not c.name.startswith('RS2 / '): continue
    group=c.name[6:].replace(' ','_')
    objs=objects_in(c)
    if not objs: continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    path=DEST/'Models'/f'RS2_{group}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={'MESH','EMPTY','CURVE','SURFACE'}, path_mode='COPY', embed_textures=False, apply_unit_scale=True, axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False)
    groups.append({'name':group,'path':'Assets/D22/Art/RecordShopV2/Models/'+path.name,'objects':len(objs)})

lights=[]
for o in scene.objects:
    if o.type!='LIGHT': continue
    lights.append({'name':o.name,'position':list(o.location),'direction':list((o.rotation_euler.to_matrix()@Vector((0,0,-1))).normalized()),'power':o.data.energy,'color':list(o.data.color)})

manifest={'version':2,'scene':'D22_RecordShop_v2','source':'blender/source/D22_RecordShop_v2.blend','dimensions_m':[4.25,8.2,2.85],'groups':groups,'lights':lights,'meshy_sources':sorted({o.get('source_asset') for o in scene.objects if o.get('source_asset')}),'camera_views':['entry','listening','cash'],'notes':['Blender owns layout, UVs, source materials and physical light intent. Unity owns runtime collisions, probes and player camera.','This v2 scene is a redesign; the v1 scene and Gaussian scan remain preserved for comparison.']}
(DEST/'Data'/'recordshop-v2-export.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'groups':groups,'lights':len(lights),'manifest':str(DEST/'Data'/'recordshop-v2-export.json')},ensure_ascii=False))
