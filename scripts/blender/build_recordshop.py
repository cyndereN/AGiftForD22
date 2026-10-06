"""Create the record-shop v1 in its own Blender scene; leave livehouse data intact.

Run through Blender MCP or: blender -b -P scripts/blender/build_recordshop.py
The .blend is written with only this scene and its dependencies.
"""
import bpy
import json
import math
import random
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
random.seed(22)
NAME = 'D22_RecordShop_v1'
if bpy.data.scenes.get(NAME):
    raise RuntimeError('Record-shop scene already exists; inspect before rebuilding')
scene = bpy.data.scenes.new(NAME)
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
groups = {}
for name in ['01 Architecture', '02 Record furniture', '03 Meshy reused', '04 Paper', '05 Lights', '06 Camera']:
    c = bpy.data.collections.new('RS / '+name)
    scene.collection.children.link(c)
    groups[name] = c

def place_collection(o, group):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    groups[group].objects.link(o)

def material(name, color, rough=.72, image=None, metal=0, emission=0):
    m = bpy.data.materials.new('RS / '+name)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    if image:
        t = m.node_tree.nodes.new('ShaderNodeTexImage')
        t.image = bpy.data.images.load(str(ROOT/image),check_existing=True)
        t.image.pack()
        m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
    if emission:
        p.inputs['Emission Color'].default_value = (*color,1)
        p.inputs['Emission Strength'].default_value = emission
    m.diffuse_color = (*color,1)
    return m

wood = material('Worn timber',(0.19,.095,.044),image='assets/d22/textures/wood_floor_worn_Diffuse_neutral.jpg')
red = material('Oxide red paint',(.22,.043,.030))
steel = material('Black painted steel',(.014,.017,.014),rough=.48,metal=.45)
plaster = material('Warm plaster',(.38,.34,.265),rough=.94)
paper = material('Warm paper',(.72,.67,.54),rough=.91)
vinyl = material('Vinyl black',(.009,.009,.008),rough=.27)
amber = material('Amber lamp',(.95,.46,.15),rough=.35,emission=3)
label = material('Faded olive label',(.17,.24,.15))
posters = [material('Fictional band %02d'%i,(1,1,1),image='assets/d22/v9/textures/band-%d.jpg'%i) for i in range(6)]

def box(name,loc,dim,mat,group='01 Architecture',bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name='RS / '+name;o.dimensions=dim
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    place_collection(o,group);o.data.materials.append(mat)
    if bevel:
        m=o.modifiers.new('Edge wear radius','BEVEL');m.width=bevel;m.segments=2
    return o

def face(name,loc,wh,mat,rot=(math.pi/2,0,0)):
    bpy.ops.mesh.primitive_plane_add(size=1,location=loc,rotation=rot)
    o=bpy.context.object;o.name='RS / '+name;o.scale=(wh[0],wh[1],1)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    place_collection(o,'04 Paper');o.data.materials.append(mat);return o

def text(name,body,loc,size=.15,mat=paper,rot=(math.pi/2,0,0)):
    curve=bpy.data.curves.new(name,'FONT');curve.body=body;curve.size=size;curve.extrude=.001;curve.align_x='CENTER'
    o=bpy.data.objects.new('RS / '+name,curve);groups['04 Paper'].objects.link(o)
    o.location=loc;o.rotation_euler=rot;curve.materials.append(mat);return o

# 6.4 m wide, 10 m deep; +Y leads to the counter, Z is up.
box('Collision Floor',(0,5,-.075),(6.4,10,.15),wood)
box('Left Wall',(-3.27,5,1.5),(.14,10,3),plaster)
box('Back Wall',(0,10.07,1.5),(6.4,.14,3),plaster)
# The side door at y=7.2 leads to the hutong. Split the wall around its opening.
box('Right Front Wall',(3.27,3.2,1.5),(.14,6.4,3),plaster)
box('Right Rear Wall',(3.27,9,1.5),(.14,2,3),plaster)
box('Door Lintel',(3.27,7.2,2.68),(.14,1.6,.64),red)
box('Ceiling',(0,5,3.10),(6.4,10,.15),steel)
for side in [-1,1]:
    for i in range(28):
        y=.2+i*.35
        if side>0 and 6.4<y<8: continue
        box('Red wainscot %d %02d'%(side,i),(side*3.16,y,.47),(.055,.334,.94),red,bevel=.005)
    box('Lower wall trim %d'%side,(side*3.1,3,.98),(.06,5.8,.07),steel)
for y in [1.1,4.1,7.1,9.7]:
    box('Ceiling beam %.1f'%y,(0,y,2.93),(6.4,.12,.17),steel)
box('Entry Threshold',(0,.15,.035),(2.2,.3,.07),steel)
for x in [-1.13,1.13]: box('Entry jamb',(x,.05,1.45),(.14,.18,2.9),red)
box('Entry header',(0,.05,2.88),(2.4,.18,.16),red)
box('Exit Threshold',(3.2,7.2,.04),(.45,1.6,.08),wood)
box('Exit Reveal',(3.57,7.2,1.2),(.06,1.56,2.4),label)
text('Exit label','HUTONG  /  EXIT',(3.05,7.2,2.48),.15,paper,(math.pi/2,0,-math.pi/2))

def cabinet(name,x,y,w=1.15,d=.70,h=.94):
    group='02 Record furniture'
    box(name+' base',(x,y,.12),(w,d,.18),steel,group)
    for sx in [-1,1]: box(name+' side',(x+sx*(w-.045)/2,y,h/2),( .045,d,h),wood,group)
    box(name+' bottom',(x,y,.58),(w,d,.045),wood,group)
    for sy in [-1,1]:box(name+' rail',(x,y+sy*(d-.035)/2,.78),(w,.035,.40),wood,group)
    box(name+' divider',(x,y,.78),(.025,d,.40),wood,group)
    # Thin sleeves are separate editable leaves; fronts use our existing fictional band art.
    for bay in [-1,1]:
        for j in range(12):
            cy=y-d*.33+j*d*.05
            cover=box(name+' sleeve %d %02d'%(bay,j),(x+bay*w*.24,cy,.92),(.32,.016,.32),paper,group,bevel=.001)
            cover.rotation_euler.x=math.radians(-12)
            f=face(name+' artwork',(x+bay*w*.24,cy-.012,.928),(.30,.30),posters[(j+(bay+1)*2)%6])
            f.rotation_euler.x=math.pi/2-math.radians(12)
    box(name+' category',(x,y-d/2-.012,.78),(.63,.014,.12),label,group,.002)
    text(name+' category text','INDEPENDENT / 01',(x,y-d/2-.023,.755),.043)

for i in range(3):cabinet('Left bin %d'%i,-2.03,2.2+i*1.7)
for i in range(2):cabinet('Right bin %d'%i,2.03,2.4+i*1.8)
# Shallow wall LP racks: avoid the main aisle and give the room a dense, human scale.
for side in [-1,1]:
    for row in range(3):
        z=1.30+row*.45
        for j in range(8):
            y=1.1+j*.55
            x=side*3.035
            rot=(math.pi/2,0,side*math.pi/2)
            face('Wall sleeve %d %d %d'%(side,row,j),(x,y,z),(.34,.34),posters[(j+row)%6],rot)
        box('Wall rack %d %d'%(side,row),(side*3.02,3.05,z-.19),(.15,4.9,.045),wood,'02 Record furniture')
# The existing livehouse posters also appear just inside the front door.
for i in range(3):
    x=-2.35+i*.62
    face('Entry poster %d'%i,(x,.27,1.9),(.51,.75),posters[i],(math.pi/2,0,math.pi))

reuse=[]
def imported(path,name,position,dimension,axis=2,angle=0):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(ROOT/path))
    objects=list(set(bpy.data.objects)-before)
    meshes=[o for o in objects if o.type=='MESH']
    pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    lo=Vector([min(v[i] for v in pts) for i in range(3)])
    hi=Vector([max(v[i] for v in pts) for i in range(3)])
    root=bpy.data.objects.new('RS / '+name,None);groups['03 Meshy reused'].objects.link(root)
    factor=dimension/(hi-lo)[axis]
    center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    for o in objects:
        if o.parent not in objects:
            o.parent=root;o.matrix_parent_inverse.identity();o.location-=center
        place_collection(o,'03 Meshy reused')
        o.name='RS / '+name+' / '+o.name
    root.scale=(factor,)*3;root.rotation_euler.z=angle;root.location=position
    for o in meshes:
        for m in o.data.materials:
            if m and m.use_nodes:
                for n in m.node_tree.nodes:
                    if n.type=='TEX_IMAGE' and n.image and not n.image.packed_file:
                        n.image.pack()
    reuse.append({'path':path,'name':name,'source_extent':list(hi-lo),'scale':factor,'position':position})
    return root

imported('assets/d22/v9/meshy/B03-BarCounter.glb','Meshy counter',(0,8.3,0),1.02)
imported('assets/d22/v9/meshy/B06-BarStool.glb','Meshy listening stool',(1.48,5.5,0),.70)
imported('assets/d22/v9/meshy/B10-Zines.glb','Meshy zine pile',(-.60,8.15,1.05),.40,axis=0)
imported('assets/d22/meshy/A05-Mixer.glb','Meshy mixer',(2.32,5.20,.95),.53,axis=0)
box('Listening tabletop',(2.3,5.3,.91),(1.25,1.3,.09),wood,'02 Record furniture')
for x in [1.76,2.84]:
    for y in [4.79,5.81]: box('Listening leg',(x,y,.43),(.065,.065,.86),steel,'02 Record furniture')
box('Back cabinet',(0,9.6,1.20),(4.9,.45,2.4),steel,'02 Record furniture')
for row in range(5):
    z=.25+row*.43
    box('Back shelf %d'%row,(0,9.32,z),(4.8,.45,.045),wood,'02 Record furniture')
    for i in range(32):
        col=random.choice([paper,red,label,plaster,steel])
        box('Album spine %d %d'%(row,i),(-2.28+i*.145,9.3,z+.19),(.095,.27,.33),col,'02 Record furniture',.001)
text('Shop sign','SIDE B  /  RECORDS',(0,9.04,2.65),.25)
text('Counter note','ASK FOR THE NEXT SIDE',(0,7.80,.79),.08)

def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
lights=[]
def area(name,loc,target,power,color,size):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);groups['05 Lights'].objects.link(o);o.location=loc;aim(o,target)
    lights.append(o);return o
for i,y in enumerate([2.5,5.2,8.1]):
    box('Pendant cable',(0,y,2.81),(.012,.012,.25),steel,'02 Record furniture',0)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=.10,location=(0,y,2.64))
    o=bpy.context.object;o.name='RS / Warm practical lens';place_collection(o,'02 Record furniture');o.data.materials.append(amber)
    area('RS / Pendant %d'%i,(0,y,2.51),(0,y,.7),170,(1,.68,.40),1.6)
for i,y in enumerate([2.4,5.5]):
    imported('assets/d22/v9/meshy/B08-GalleryLamp.glb','Meshy wall lamp %d'%i,(-3.0,y,1.92),.28)
    area('RS / Wall lamp %d'%i,(-2.75,y,2.1),(-1.8,y,1),55,(1,.60,.32),.7)
area('RS / Counter key',(1.1,7.65,2.40),(0,8.3,1),170,(1,.70,.45),1.3)
area('RS / Street window',(0,.15,2.0),(0,4.8,1.0),280,(.66,.79,1),3.0)
world=bpy.data.worlds.new('RS / Subtle room bounce');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.32,.37,.43,1);bg.inputs['Strength'].default_value=.12;scene.world=world
data=bpy.data.cameras.new('RS entry camera');cam=bpy.data.objects.new('RS / Entry camera',data)
groups['06 Camera'].objects.link(cam);cam.location=(.25,.70,1.65);aim(cam,(0,7.7,1.36));data.lens=23;scene.camera=cam
try:scene.render.engine='CYCLES'
except TypeError:pass
if hasattr(scene,'cycles'):scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1440;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'blender/verification/recordshop-v1/entry.png')
scene.view_settings.view_transform='AgX'
scene['design_source']='design/recordshop-v1';scene['shared_style']='references/style-lock.md'
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.region_3d.view_perspective='CAMERA';a.spaces.active.shading.type='MATERIAL'
destination=ROOT/'blender/source/D22_RecordShop_v1.blend'
bpy.data.libraries.write(str(destination),{scene},fake_user=True,compress=True)
manifest={'version':1,'scene':NAME,'source':str(destination.relative_to(ROOT)),'dimensions_m':[6.4,10,3], 'unity_scene':'D22_RecordShop','preserved_scan_scene':'D22_RecordShopScan','reused_meshy':reuse,'new_meshy_credits':0,'spawn_blender':[.25,.7,0],'counter_focus_blender':[0,7.35,1.45],'bottle_blender':[.45,7.98,1.08],'exit_blender':[2.8,7.2,1.6]}
(ROOT/'design/recordshop-v1/layout.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'blend':str(destination),'objects':len(scene.objects),'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'reuse':reuse},ensure_ascii=False))
