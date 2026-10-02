"""Publish the open Blender scene to deterministic Unity FBX + PBR assets.

Run in a background Blender process on a copy. Never saves the source .blend.
"""
import bpy
import json
import math
import re
import hashlib
import sys
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'unity/D22Game/Assets/D22/Art'
for folder in ['Models', 'Textures', 'Data']:
    (DEST / folder).mkdir(parents=True, exist_ok=True)

def key(name):
    return re.sub(r'[^A-Za-z0-9_-]+', '_', name).strip('_')[:70] + '_' + hashlib.sha1(name.encode()).hexdigest()[:8]

scene = bpy.context.scene
original_file = bpy.data.filepath
hidden = set()
def hide_tree(collection, parent_hidden=False):
    is_hidden = parent_hidden or collection.hide_render
    if is_hidden:
        hidden.update(o.name for o in collection.objects)
    for child in collection.children:
        hide_tree(child, is_hidden)
hide_tree(scene.collection)
visible = [o for o in scene.objects if not o.hide_render and o.name not in hidden]
geometry = [o for o in visible if o.type in {'MESH', 'CURVE', 'FONT', 'SURFACE'}]
materials = sorted({m for o in geometry for m in o.data.materials if m}, key=lambda m:m.name)
material_ids = {m.name: key(m.name) for m in materials}
image_paths = {}

def save_image(im):
    if im.name in image_paths:
        return image_paths[im.name]
    name = key(im.name)
    if im.packed_file:
        data = im.packed_file.data
        ext = '.png' if data[:8] == b'\x89PNG\r\n\x1a\n' else '.jpg' if data[:2] == b'\xff\xd8' else None
        if ext:
            path = DEST/'Textures'/(name+ext)
            path.write_bytes(data)
        else:
            path = DEST/'Textures'/(name+'.png')
            im.filepath_raw = str(path); im.file_format = 'PNG'; im.save()
    else:
        path = DEST/'Textures'/(name+'.png')
        im.filepath_raw = str(path); im.file_format = 'PNG'; im.save()
    result = 'Assets/D22/Art/Textures/'+path.name
    image_paths[im.name] = result
    return result

def upstream(socket, types, depth=0):
    if depth > 8 or not socket or not socket.is_linked:
        return None
    node = socket.links[0].from_node
    if node.type in types:
        return node
    for inp in node.inputs:
        found = upstream(inp, types, depth+1)
        if found:
            return found
    return None

def base_color(socket):
    if not socket.is_linked:
        return list(socket.default_value), None
    node = socket.links[0].from_node
    image = upstream(socket, {'TEX_IMAGE'})
    color = [1,1,1,1]
    if node.type == 'MIX_RGB' and node.blend_type == 'MULTIPLY':
        fac = node.inputs[0].default_value
        tint = node.inputs[2].default_value
        color = [(1-fac)+fac*tint[i] for i in range(3)]+[1]
    return color, image.image if image and image.image else None

def channel(socket):
    if not socket.is_linked:
        return None, None, float(socket.default_value)
    link = socket.links[0]
    node = link.from_node
    if node.type == 'SEPARATE_COLOR':
        imnode = upstream(node.inputs[0], {'TEX_IMAGE'})
        idx = {'Red':0,'Green':1,'Blue':2}.get(link.from_socket.name,0)
        return (imnode.image if imnode else None), idx, float(socket.default_value)
    imnode = upstream(socket, {'TEX_IMAGE'})
    return (imnode.image if imnode else None), 0, float(socket.default_value)

def scalar_pixels(im, index, default, size):
    if im is None:
        return np.full((size,size), default, dtype=np.float32)
    w,h = im.size
    raw = np.empty(w*h*4, dtype=np.float32); im.pixels.foreach_get(raw)
    raw = raw.reshape(h,w,4)
    yy = np.minimum((np.arange(size)*h/size).astype(int),h-1)
    xx = np.minimum((np.arange(size)*w/size).astype(int),w-1)
    return raw[yy[:,None],xx[None,:],index]

records=[]
for mat in materials:
    ident=material_ids[mat.name]
    p=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
    record=dict(id=ident,source_name=mat.name,base_color=list(mat.diffuse_color),metallic=0.,roughness=.7,normal_scale=1.,emission=[0,0,0,1],emission_strength=0.,base_map='',normal_map='',metallic_map='',emission_map='',alpha_clip=False,transparent=False,notes=[])
    if p:
        color,im=base_color(p.inputs['Base Color']);record['base_color']=color
        if im:record['base_map']=save_image(im)
        record['alpha_clip']=p.inputs['Alpha'].is_linked
        record['transparent']=p.inputs['Transmission Weight'].default_value>.5
        metal,mi,mv=channel(p.inputs['Metallic']);rough,ri,rv=channel(p.inputs['Roughness'])
        record.update(metallic=mv,roughness=rv)
        if metal or rough:
            size=min(2048,max((i.size[0] for i in [metal,rough] if i)))
            pixels=np.ones((size,size,4),dtype=np.float32)
            pixels[:,:,0]=scalar_pixels(metal,mi,mv,size)
            pixels[:,:,3]=1-scalar_pixels(rough,ri,rv,size)
            packed=bpy.data.images.new(ident+'_MetallicSmoothness',width=size,height=size,alpha=True)
            packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(pixels.ravel());packed.file_format='PNG';packed.filepath_raw=str(DEST/'Textures'/(ident+'_MetallicSmoothness.png'));packed.save()
            record['metallic_map']='Assets/D22/Art/Textures/'+Path(packed.filepath_raw).name
            bpy.data.images.remove(packed)
        normal=upstream(p.inputs['Normal'],{'NORMAL_MAP'})
        if normal:
            imnode=upstream(normal.inputs['Color'],{'TEX_IMAGE'})
            if imnode:record['normal_map']=save_image(imnode.image);record['normal_scale']=float(normal.inputs['Strength'].default_value)
        elif p.inputs['Normal'].is_linked:
            record['notes'].append('Procedural micro-bump is not embedded in FBX; color and roughness are preserved.')
        record['emission']=list(p.inputs['Emission Color'].default_value)
        record['emission_strength']=float(p.inputs['Emission Strength'].default_value)
        emit=upstream(p.inputs['Emission Color'],{'TEX_IMAGE'})
        if emit:record['emission_map']=save_image(emit.image)
        if mat.name=='Stage scuffed dark planks':
            record['notes'].append('Object-space wood coordinates converted to explicit planar UVs.')
    records.append(record)
    mat.name=ident

def group_for(o):
    names=[c.name for c in o.users_collection]
    if any(n.startswith('02 Stage') for n in names) or 'Marshall' in o.name or 'Round hanging sign' in o.name or o.name.startswith('Mesh_0.00'):
        return 'StageProps'
    if any(n.startswith(('03 Bar','04 FOH')) for n in names):
        return 'BarFOH'
    if any(n.startswith('05 Paper') for n in names) or 'Band frame' in o.name or 'photograph' in o.name:
        return 'GalleryDecor'
    if any(n.startswith('06 Practical') for n in names) or 'lamp' in o.name.lower() or 'PAR' in o.name:
        return 'Fixtures'
    return 'Architecture'

export_groups={}
objects=[]
dg=bpy.context.evaluated_depsgraph_get()
temporary=bpy.data.collections.new('UNITY_EXPORT_ONLY');scene.collection.children.link(temporary)
for source in geometry:
    group=group_for(source)
    mesh=bpy.data.meshes.new_from_object(source.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    obj=bpy.data.objects.new(key(source.name),mesh);temporary.objects.link(obj);obj.matrix_world=source.matrix_world.copy()
    for poly in mesh.polygons:
        if poly.material_index<len(mesh.materials) and mesh.materials[poly.material_index] and mesh.materials[poly.material_index].name==material_ids.get('Stage scuffed dark planks'):
            uv=mesh.uv_layers.active or mesh.uv_layers.new(name='UVMap')
            for loop in poly.loop_indices:
                v=mesh.vertices[mesh.loops[loop].vertex_index].co
                uv.data[loop].uv=(v.x*.6,v.y*.6)
    if not mesh.uv_layers:
        uv=mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            axis=max(range(3),key=lambda i:abs(poly.normal[i]));axes=[i for i in range(3) if i!=axis]
            for loop in poly.loop_indices:
                v=mesh.vertices[mesh.loops[loop].vertex_index].co;uv.data[loop].uv=(v[axes[0]],v[axes[1]])
    export_groups.setdefault(group,[]).append(obj)
    bounds=[source.matrix_world@Vector(v) for v in source.bound_box]
    objects.append({'id':obj.name,'source_name':source.name,'group':group,'bounds_min':[min(v[i] for v in bounds) for i in range(3)],'bounds_max':[max(v[i] for v in bounds) for i in range(3)],'polygons':len(mesh.polygons),'materials':[m.name if m else '' for m in mesh.materials]})

for group,items in sorted(export_groups.items()):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in items:obj.select_set(True)
    bpy.context.view_layer.objects.active=items[0]
    bpy.ops.export_scene.fbx(filepath=str(DEST/'Models'/f'D22_{group}.fbx'),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=False,bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='STRIP',embed_textures=False,use_custom_props=False)
    print('EXPORTED',group,len(items),flush=True)

def unity_vec(v):return [float(v[0]),float(v[2]),float(v[1])]
lights=[]
for obj in visible:
    if obj.type!='LIGHT' or obj.data.energy<=0:continue
    d=obj.data
    direction=obj.matrix_world.to_quaternion()@Vector((0,0,-1))
    lights.append({'name':obj.name,'type':d.type,'position':unity_vec(obj.matrix_world.translation),'direction':unity_vec(direction),'color':list(d.color),'power':d.energy,'angle':math.degrees(getattr(d,'spot_size',math.pi/2)),'blend':getattr(d,'spot_blend',.5),'size':getattr(d,'size',.1),'size_y':getattr(d,'size_y',getattr(d,'size',.1)),'radius':getattr(d,'shadow_soft_size',.05),'group':obj.users_collection[0].name if obj.users_collection else ''})
report={'version':1,'source':'blender/source/D22_Balanced_Lighting_v18.blend','source_units_m':scene.unit_settings.scale_length,'coordinate_mapping':'Unity (x,y,z) = Blender (x,z,y); FBX converted by importer','geometry_count':len(objects),'materials':records,'objects':objects,'lights':lights,'groups':sorted(export_groups),'approximation_notes':['Procedural micro-bump and Blender transmission require Unity shader equivalents.','Light placement and color are preserved; intensity is calibrated for URP, not numerically copied between renderers.']}
(DEST/'Data/d22-export.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print('UNITY_EXPORT_COMPLETE',len(objects),len(records),len(lights),flush=True)
