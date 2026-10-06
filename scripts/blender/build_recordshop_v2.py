"""Rebuild the D22 record shop as a narrow, lived-in Beijing hutong shop.

This intentionally creates a new scene and leaves the previous v1 scene intact.
Meshy props are imported as separate assets so their provenance and transforms stay editable.
"""
import bpy, math, json, random
from pathlib import Path
from mathutils import Vector

ROOT = Path('/Users/yadongliu/WorkSpace/AGiftForD22')
random.seed(2026)
SCENE_NAME = 'D22_RecordShop_v2'
if bpy.data.scenes.get(SCENE_NAME):
    bpy.data.scenes.remove(bpy.data.scenes[SCENE_NAME], do_unlink=True)
scene = bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

COL = {}
for n in ['Architecture', 'Furniture', 'Meshy Props', 'Paper and clutter', 'Lighting', 'Cameras']:
    c = bpy.data.collections.new('RS2 / ' + n)
    scene.collection.children.link(c)
    COL[n] = c

def move_to(obj, group):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    COL[group].objects.link(obj)

def image_node(mat, path, colorspace='sRGB'):
    if not path: return None
    img = bpy.data.images.load(str(ROOT / path), check_existing=True)
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = img
    try: img.colorspace_settings.name = colorspace
    except Exception: pass
    return node

def make_mat(name, color, rough=.72, metallic=0.0, base=None, normal=None, roughmap=None, emission=None):
    m = bpy.data.materials.new('RS2 / ' + name); m.use_nodes = True
    nt = m.node_tree; p = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = rough; p.inputs['Metallic'].default_value = metallic
    if base:
        t = image_node(m, base); nt.links.new(t.outputs['Color'], p.inputs['Base Color'])
    if normal:
        t = image_node(m, normal, 'Non-Color'); n = nt.nodes.new('ShaderNodeNormalMap'); n.inputs['Strength'].default_value=.55
        nt.links.new(t.outputs['Color'], n.inputs['Color']); nt.links.new(n.outputs['Normal'], p.inputs['Normal'])
    if roughmap:
        t = image_node(m, roughmap, 'Non-Color'); nt.links.new(t.outputs['Color'], p.inputs['Roughness'])
    if emission:
        p.inputs['Emission Color'].default_value=(*emission[0],1); p.inputs['Emission Strength'].default_value=emission[1]
    m.diffuse_color=(*color,1); return m

plaster = make_mat('Dirty ivory plaster', (.54,.49,.40), .96, base='assets/recordshop/v2/textures/plaster-albedo.png', normal='assets/recordshop/v2/textures/plaster-normal.png', roughmap='assets/recordshop/v2/textures/plaster-rough.png')
stone = make_mat('Worn cement tile', (.34,.32,.28), .9, base='assets/recordshop/v2/textures/stone-albedo.png', normal='assets/recordshop/v2/textures/stone-normal.png', roughmap='assets/recordshop/v2/textures/stone-rough.png')
timber = make_mat('Old shop timber', (.20,.075,.035), .82, base='assets/recordshop/v2/textures/oxide-timber.jpg')
red = make_mat('Oxide red lower wall', (.27,.055,.038), .78)
black = make_mat('Sooty black steel', (.018,.020,.018), .45, .65)
cream = make_mat('Yellowed paper', (.72,.64,.47), .94)
sage = make_mat('Faded sage paint', (.28,.39,.30), .86)
glass = make_mat('Dusty glass', (.32,.42,.36), .26, .15)
rug = make_mat('Faded woven runner', (.24,.075,.045), .96, base='assets/recordshop/v2/textures/woven-runner.jpg')
warm = make_mat('Warm practical shade', (.72,.30,.09), .64, emission=((1.0,.27,.06),1.8))
vinyl = make_mat('Black vinyl', (.006,.007,.006), .22, .05)

def cube(name, loc, dims, mat, group='Architecture', bevel=.018):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o=bpy.context.object; o.name='RS2 / '+name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to(o, group); o.data.materials.append(mat)
    if bevel:
        b=o.modifiers.new('soft worn edges','BEVEL'); b.width=bevel; b.segments=2
    return o

def plane(name, loc, dims, mat, rotation=(0,0,0), group='Paper and clutter'):
    bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=rotation)
    if len(dims)==2: dims=(dims[0],dims[1],0)
    o=bpy.context.object; o.name='RS2 / '+name; o.dimensions=dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True); move_to(o, group); o.data.materials.append(mat); return o

def text(name, body, loc, size=.12, mat=cream, rotation=(math.pi/2,0,0)):
    c=bpy.data.curves.new('RS2 / '+name,'FONT'); c.body=body; c.align_x='CENTER'; c.size=size; c.extrude=.0015
    o=bpy.data.objects.new('RS2 / '+name,c); COL['Paper and clutter'].objects.link(o); o.location=loc; o.rotation_euler=rotation; c.materials.append(mat); return o

def aim(obj, target): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

# Space: a narrow 4.25m x 8.2m shop. Front is y=0, the offset hutong door is at the back.
W,D,H=4.25,8.2,2.85
cube('Floor',(0,D/2,-.07),(W,D,.14),stone,bevel=.01)
cube('Ceiling',(0,D/2,H+.04),(W,D,.12),black,bevel=.01)
cube('Left plaster wall',(-W/2-.05,D/2,H/2),(.12,D,H),plaster)
cube('Right plaster wall',(W/2+.05,D/2,H/2),(.12,D,H),plaster)
cube('Back wall',(0,D+.05,H/2),(W,.12,H),plaster)
for side in (-1,1):
    cube('Red dado '+str(side),(side*(W/2-.07),D/2,.52),(.045,D,.86),red,bevel=.004)
    cube('Dado trim '+str(side),(side*(W/2-.10),D/2,.97),(.06,D,.055),black,bevel=.004)
# Front frame: open entrance with a dirty old window/door frame.
cube('Front left pier',(-1.84,-.03,H/2),(.55,.12,H),plaster)
cube('Front right pier',(1.84,-.03,H/2),(.55,.12,H),plaster)
cube('Front lintel',(0,-.03,2.58),(3.15,.12,.54),timber)
cube('Front red door leaf',(-1.28,.02,1.25),(.08,.06,2.34),red)
# Back side door is off axis and leaves a visible green-lit threshold.
cube('Back door frame',(1.27,D-.02,1.30),(1.52,.12,.12),timber)
cube('Back door left jamb',(.54,D-.02,1.35),(.12,.12,2.70),timber)
cube('Back door right jamb',(2.0,D-.02,1.35),(.12,.12,2.70),timber)
cube('Back door green reveal',(1.27,D+.01,1.34),(1.30,.02,2.48),sage,bevel=.004)
text('door note','后门 / HUTONG',(1.27,D-.085,2.25),.12,cream,(math.pi/2,0,0))
# Window bay on left: cool daylight and imperfect mullions.
cube('Window recess',(-2.18,2.25,1.82),(.05,3.15,1.72),glass,bevel=.008)
for y in [1.15,2.25,3.35]: cube('Window mullion',(-2.23,y,1.82),(.10,.045,1.75),timber,bevel=.005)
cube('Window sill',(-2.30,2.25,.94),(.35,3.25,.10),timber)
for x in [-.65,.55]: cube('Front transom mullion',(x,-.10,1.83),(.05,.12,1.48),timber,bevel=.004)

# A dark runner makes the path readable and gives the room a lived-in turn.
plane('Worn runner',(0,4.05,.012),(1.05,5.55),rug)

def import_glb(rel, label, loc, target_height=None, target_width=None, yaw=0, group='Meshy Props'):
    before=set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=str(ROOT/rel)); new=list(set(bpy.data.objects)-before)
    meshes=[o for o in new if o.type=='MESH']
    if not meshes: return None
    pts=[o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector((min(p.x for p in pts),min(p.y for p in pts),min(p.z for p in pts))); hi=Vector((max(p.x for p in pts),max(p.y for p in pts),max(p.z for p in pts)))
    ext=hi-lo; factor=(target_height/ext.z if target_height else target_width/ext.x if target_width else 1)
    root=bpy.data.objects.new('RS2 / '+label,None); COL[group].objects.link(root); root.location=loc; root.scale=(factor,)*3; root.rotation_euler.z=yaw
    center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    for o in new:
        if o.parent not in new: o.parent=root; o.matrix_parent_inverse.identity(); o.location-=center
        move_to(o,group); o.name='RS2 / '+label+' / '+o.name
    root['source_asset']=rel; root['source_extent']=list(ext); root['normalized_scale']=factor
    return root

# Generated Meshy props: position them as a composition, not a wall of repeated placeholders.
import_glb('assets/recordshop/v2/meshy/RS201-RecordBin.glb','Front browsing bin',(-1.40,1.35,0),target_width=1.15,yaw=.05)
import_glb('assets/recordshop/v2/meshy/RS201-RecordBin.glb','Mid browsing bin',(-1.28,3.55,0),target_width=1.02,yaw=-.04)
import_glb('assets/recordshop/v2/meshy/RS202-CDTower.glb','Tall CD and cassette tower',(-1.70,6.62,0),target_height=2.32,yaw=.02)
import_glb('assets/recordshop/v2/meshy/RS203-CashDesk.glb','Cashier desk',(0.55,6.93,0),target_width=1.22,yaw=0)
import_glb('assets/recordshop/v2/meshy/RS204-Turntable.glb','Listening turntable',(1.25,3.95,1.07),target_width=.56,yaw=.14)
import_glb('assets/recordshop/v2/meshy/RS205-DeskFan.glb','Window fan',(-1.88,2.35,1.10),target_height=.38,yaw=-math.pi/2)
import_glb('assets/recordshop/v2/meshy/RS206-DeskLamp.glb','Listening lamp',(1.70,4.05,1.10),target_height=.44,yaw=.15)
import_glb('assets/recordshop/v2/meshy/RS207-Thermos.glb','Tea thermos',(0.34,6.88,1.05),target_height=.35,yaw=-.12)
import_glb('assets/recordshop/v2/meshy/RS208-Pothos.glb','Window pothos',(-1.88,2.32,1.04),target_height=.50,yaw=.25)

# Listening table and low stool give the room a social, non-gallery scale.
cube('Listening table top',(1.35,4.15,1.00),(1.65,1.18,.10),timber,'Furniture',.028)
for x in [.67,2.03]:
    for y in [3.67,4.63]: cube('Listening table leg',(x,y,.50),(.08,.08,.90),black,'Furniture',.01)
cube('Low listening stool',(1.80,4.98,.36),(.62,.62,.58),timber,'Furniture',.04)
plane('Stool worn seat',(1.80,4.98,.67),(.56,.56),rug,group='Furniture')
# Wall shelving is deliberately uneven; each shelf receives a few authored spines and flyers.
for side in (-1,1):
    x=side*2.08
    for row in range(3):
        z=1.20+row*.48
        cube('Wall LP shelf', (x,5.00,z), (.18,3.05,.055),timber,'Furniture',.008)
        for j in range(7):
            y=3.72+j*.40
            col=[cream,red,sage,black][(j+row)%4]
            cube('CD spine',(x+side*.015,y,z+.22),(.055,.29,.36),col,'Furniture',.004)

# Paper layer: generated flyer textures, held with tape and not floating in midair.
flyers=sorted((ROOT/'assets/recordshop/v2/textures').glob('flyer-*.jpg'))
def flyer(name, loc, dims, rot, idx, wall='back'):
    mat=make_mat('Flyer '+str(idx),(.7,.6,.4),.93,base=str((Path('assets/recordshop/v2/textures')/flyers[idx%len(flyers)].name)))
    if wall=='back': r=(math.pi/2,0,0)
    elif wall=='right': r=(math.pi/2,0,math.pi/2)
    else: r=(math.pi/2,0,-math.pi/2)
    return plane(name,loc,dims,mat,r)
for i,(x,z) in enumerate([(-1.35,1.68),(-.75,2.15),(-.12,1.72),(.58,2.02),(1.18,1.58),(1.72,2.18)]):
    flyer('Back wall flyer',(x,D-.085,z),(.40,.58),0,i)
for i,(y,z) in enumerate([(1.05,1.60),(1.55,2.10),(2.95,1.65),(3.55,2.05),(5.02,1.62),(5.55,2.12)]):
    flyer('Right wall flyer',(2.17,y,z),(.40,.58),0,i+3,'right')
for i,(y,z) in enumerate([(4.55,1.58),(5.25,2.05),(6.05,1.70)]):
    flyer('Left wall flyer',(-2.17,y,z),(.42,.58),0,i+7,'left')
text('shop mark','SIDE B',(.56,D-.13,2.52),.25,cream)
text('small note','听完再走 / PLAY IT THROUGH',(1.0,6.20,1.72),.07,cream,(math.pi/2,0,0))

# A few grounded clutter pieces: cardboard boxes, handwritten dividers, loose sleeves.
for i,(x,y) in enumerate([(-1.65,2.0),(-1.02,4.15),(.10,5.65),(1.62,5.02)]):
    cube('Loose record crate',(x,y,.16),(.48,.36,.25),timber,'Furniture',.012)
    plane('Divider card',(x,y-.19,.32),(.20,.10),cream,(math.pi/2,0,0))
    text('Divider label','摇滚 / 现场',(x,y-.205,.34),.035,black,(math.pi/2,0,0))

# Lighting: cool street/window spill, warm practical pools, and a very low neutral fill.
def area(name, loc, target, energy, color, size, shape='DISK'):
    d=bpy.data.lights.new('RS2 / '+name,'AREA'); d.energy=energy; d.color=color; d.shape=shape; d.size=size
    o=bpy.data.objects.new('RS2 / '+name,d); COL['Lighting'].objects.link(o); o.location=loc; aim(o,target); return o
area('Window daylight',(-2.35,2.25,2.22),(.1,3.1,1.05),380,(.60,.78,1.0),2.2,'RECTANGLE')
area('Front overcast spill',(0,-.45,2.45),(0,3.5,.9),100,(.55,.65,.75),2.4,'RECTANGLE')
area('Warm centre pendant',(0,2.8,2.58),(0,2.8,.75),155,(1.0,.48,.18),1.15)
area('Warm listening pool',(1.55,4.0,2.35),(1.35,4.1,.9),125,(1.0,.46,.20),.85)
area('Cash desk pool',(.55,6.45,2.35),(.55,6.9,1.1),120,(1.0,.62,.31),.95)
area('Back green bounce',(1.25,7.95,2.0),(1.25,6.2,1.1),85,(.36,.70,.42),1.0)
area('Low ambient fill',(0,4.3,2.7),(0,4.3,0),28,(.68,.71,.66),3.2)
# Visible fixtures: black cord and warm bulb spheres.
for i,y in enumerate([2.8,6.45]):
    cube('Pendant cord',(0,y,2.72),(.012,.012,.27),black,'Lighting',0)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=10, radius=.085, location=(0,y,2.54))
    bulb=bpy.context.object; bulb.name='RS2 / practical bulb '+str(i); move_to(bulb,'Lighting'); bulb.data.materials.append(warm)

# Cameras: entry, listening corner and counter reveal.
def camera(name, loc, target, lens):
    d=bpy.data.cameras.new('RS2 / '+name); o=bpy.data.objects.new('RS2 / '+name,d); COL['Cameras'].objects.link(o); o.location=loc; aim(o,target); d.lens=lens; d.clip_start=.03; d.clip_end=80; return o
entry=camera('CAM_01 entry to door',(.12,.58,1.62),(.18,5.6,1.38),24)
listen=camera('CAM_02 listening corner',(.18,4.95,1.48),(1.18,4.08,1.08),34)
cash=camera('CAM_03 counter and hutong',(-.90,5.30,1.53),(.88,7.0,1.34),29)
scene.camera=entry
scene.render.resolution_x=1440; scene.render.resolution_y=900; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
scene.render.filepath=str(ROOT/'blender/verification/recordshop-v2/entry.png')
try: scene.render.engine='BLENDER_EEVEE_NEXT'
except TypeError: scene.render.engine='BLENDER_EEVEE'
scene.render.image_settings.color_mode='RGBA'; scene.view_settings.look='AgX - Medium High Contrast'
world=bpy.data.worlds.new('RS2 / overcast hutong'); world.use_nodes=True; bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND'); bg.inputs['Color'].default_value=(.075,.09,.10,1); bg.inputs['Strength'].default_value=.18; scene.world=world
scene['art_direction']='00s Beijing hutong independent record shop; free, literary, rock, lived-in'
scene['style_lock']='references/style-lock.md'
scene['source_assets']='assets/recordshop/v2/meshy'

manifest={'version':2,'scene':SCENE_NAME,'dimensions_m':[W,D,H],'camera_views':['entry','listening','cash'],'meshy_assets':[], 'style':'00s Beijing hutong record shop / D22 documentary underground music'}
for o in COL['Meshy Props'].objects:
    if o.get('source_asset'): manifest['meshy_assets'].append({'name':o.name,'source':o['source_asset'],'location':list(o.location),'scale':list(o.scale)})
out=ROOT/'design/recordshop-v2/layout.json'; out.parent.mkdir(parents=True,exist_ok=True); out.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
src=ROOT/'blender/source/D22_RecordShop_v2.blend'; src.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(src))
bpy.ops.render.render(write_still=True)
for cam,name in [(listen,'listening'),(cash,'cash')]:
    scene.camera=cam; scene.render.filepath=str(ROOT/f'blender/verification/recordshop-v2/{name}.png'); bpy.ops.render.render(write_still=True)
scene.camera=entry; scene.render.filepath=str(ROOT/'blender/verification/recordshop-v2/entry.png'); bpy.ops.wm.save_as_mainfile(filepath=str(src))
print(json.dumps({'scene':SCENE_NAME,'objects':len(scene.objects),'meshy_props':len(manifest['meshy_assets']),'blend':str(src)},ensure_ascii=False))
