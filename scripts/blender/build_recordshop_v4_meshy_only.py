"""Meshy-only scene assembly: transforms, lighting, cameras and provenance."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Euler
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'blender/verification/recordshop-v4';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.name='D22_RecordShop_v4_MeshyOnly';s.unit_settings.system='METRIC'
C={}
for n in ['Architecture','Furniture','Audio','Paper','Clutter','Lights','Cameras']:
 c=bpy.data.collections.new('Meshy '+n);s.collection.children.link(c);C[n]=c
P='assets/recordshop/v2/meshy';R='assets/recordshop/v3/retextured';A='assets/recordshop/v3/meshy';L='assets/d22/v9/meshy'
cache={};records=[]
def template(path):
 if path in cache:return cache[path]
 old=set(bpy.data.objects);bpy.ops.import_scene.gltf(filepath=str(ROOT/path));bpy.context.view_layer.update()
 added=set(bpy.data.objects)-old;meshes=[o for o in added if o.type=='MESH']
 pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)));hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
 tmp=[(o.data,o.matrix_world.copy()) for o in meshes]
 for o in meshes:
  for m in o.data.materials:
   if m and m.use_nodes:
    m.name=Path(path).stem+'__'+m.name
    bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if bs:bs.inputs['Emission Strength'].default_value=0
 for o in added:bpy.data.objects.remove(o,do_unlink=True)
 cache[path]=(tmp,lo,hi);return cache[path]
def bounds(o):
 bpy.context.view_layer.update();pts=[c.matrix_world@Vector(v) for c in o.children for v in c.bound_box]
 return [min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]
def place(path,name,pos,group='Furniture',dims=None,width=None,height=None,yaw=0,rotation=None,collision=False,tint=None):
 tmp,lo,hi=template(path);ext=hi-lo
 fac=width/ext.x if width else height/ext.z if height else 1
 scale=tuple(dims[i]/ext[i] for i in range(3)) if dims else (fac,)*3
 rot=Euler(rotation or (0,0,yaw)).to_matrix().to_4x4()
 mat=rot@Matrix.Diagonal((*scale,1))@Matrix.Translation(-Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z)))
 pts=[mat@Vector((x,y,z)) for x in [lo.x,hi.x] for y in [lo.y,hi.y] for z in [lo.z,hi.z]]
 fix=Vector((pos[0]-(min(v.x for v in pts)+max(v.x for v in pts))/2,pos[1]-(min(v.y for v in pts)+max(v.y for v in pts))/2,pos[2]-min(v.z for v in pts)))
 root=bpy.data.objects.new(name,None);C[group].objects.link(root);root.matrix_world=Matrix.Translation(fix)@mat
 root['source_asset']=path;root['meshy_asset']=True
 for i,(data,matrix) in enumerate(tmp):
  o=bpy.data.objects.new(name+'__mesh'+str(i),data);C[group].objects.link(o);o.parent=root;o.matrix_parent_inverse=Matrix.Identity(4);o.matrix_basis=matrix;o['source_asset']=path;o['meshy_asset']=True
  if tint:
   o.data=data.copy()
   for j,m in enumerate(list(o.data.materials)):
    m=m.copy();o.data.materials[j]=m;m.name=name+'_tint';bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');inp=bs.inputs['Base Color']
    if inp.is_linked:
     link=inp.links[0];src=link.from_socket;m.node_tree.links.remove(link);mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*tint,1);m.node_tree.links.new(src,mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],inp)
    else:inp.default_value=(*tint,1)
 low,high=bounds(root);records.append(dict(label=name,source=path,group=group,position=list(pos),bounds_min=low,bounds_max=high,collision=collision));return root
def prop(id,name,pos,**kw):
 path=R+'/'+id+'.glb'
 if not (ROOT/path).exists():path=P+'/'+id+'.glb'
 return place(path,name,pos,**kw)
N='assets/recordshop/v4/meshy'
W,D,H=5.2,8.2,2.85
for ix in range(5):
 for iy in range(8):place(L+'/B02-TileFloor.glb',f'Floor {ix}_{iy}',(-2.08+ix*1.04,.5125+iy*1.025,-.085),'Architecture',dims=(1.05,1.035,.085))
place(L+'/B02-TileFloor.glb','Front floor threshold',(0,-.22,-.085),'Architecture',dims=(W+.3,.6,.085))
place(L+'/B02-TileFloor.glb','Ceiling',(0,3.9,H),'Architecture',dims=(W+.3,D+1.05,.09),tint=(.08,.095,.09))
for side in [-1,1]:
 for i in range(4):
  wall_y=.83 if i==0 else 1.025+i*2.05
  wall_length=2.5 if i==0 else 2.08
  place(R+'/RS301-HutongWallPanel.glb',f'Lower wall {side}_{i}',(side*(W/2+.07),wall_y,0),'Architecture',dims=(wall_length,.22,.92),yaw=side*math.pi/2)
  place(R+'/RS310-PlasterOnly.glb',f'Upper wall {side}_{i}',(side*(W/2+.07),wall_y,.92),'Architecture',dims=(wall_length,.22,H-.92),yaw=side*math.pi/2)
for i,x in enumerate([-1.74,0,1.74]):
 place(R+'/RS301-HutongWallPanel.glb',f'Back lower {i}',(x,8.28,0),'Architecture',dims=(1.78,.22,.92))
 place(R+'/RS310-PlasterOnly.glb',f'Back upper {i}',(x,8.28,.92),'Architecture',dims=(1.78,.22,H-.92))
# The Meshy window does not fill its bounds. Wall panels overlap behind its frame.
place(R+'/RS301-HutongWallPanel.glb','Front wall below window',(-1.15,-.40,0),'Architecture',dims=(1.95,.22,.50),yaw=math.pi)
place(R+'/RS310-PlasterOnly.glb','Front wall above window',(-1.15,-.40,2.30),'Architecture',dims=(1.95,.22,H-2.30),yaw=math.pi)
place(R+'/RS301-HutongWallPanel.glb','Front wall left lower',(-2.2125,-.40,0),'Architecture',dims=(.775,.22,.92),yaw=math.pi)
place(R+'/RS310-PlasterOnly.glb','Front wall left upper',(-2.2125,-.40,.92),'Architecture',dims=(.775,.22,H-.92),yaw=math.pi)
place(R+'/RS301-HutongWallPanel.glb','Front wall right lower',(1.0625,-.40,0),'Architecture',dims=(3.075,.22,.92),yaw=math.pi)
place(R+'/RS310-PlasterOnly.glb','Front wall right upper',(1.0625,-.40,.92),'Architecture',dims=(3.075,.22,H-.92),yaw=math.pi)
place(R+'/RS303-RedTimberDoor.glb','Door to hutong',(1.63,8.02,0),'Architecture',dims=(.90,.16,2.14))
place(R+'/RS302-HutongWindowBay.glb','Front window',(-1.15,-.13,.38),'Architecture',dims=(1.75,.25,2.12),yaw=math.pi)
place(R+'/RS302-HutongWindowBay.glb','Left window',(-2.53,1.10,.76),'Architecture',dims=(1.6,.21,1.62),yaw=math.pi/2)
for i,y in enumerate([1.8,5.7]):place(A+'/RS306-CeilingBeamFluorescent.glb',f'Beam light {i}',(0,y,2.65),'Architecture',dims=(W,.24,.16))
# Two-sided browsing island. There are independent routes around both ends.
for row,y in enumerate([2.70,3.43]):
 for col,x in enumerate([-.46,.38]):
  prop('RS201-RecordBin',f'Island bin {row}_{col}',(x,y,0),dims=(.78,.68,.96),yaw=0 if row==0 else math.pi,collision=True)
for i,(y,yaw) in enumerate([(2.85,math.pi/2),(4.23,math.pi/2+.022),(5.53,math.pi/2-.015)]):
 prop('RS201-RecordBin',f'Wall browse bin {i}',(-2.14,y,0),dims=(1.05,.68,.95),yaw=yaw,collision=True)
for y,h in [(6.75,1.96),(7.64,1.67)]:prop('RS202-CDTower','Archive CD '+str(y),(-2.42,y,0),height=h,yaw=math.pi/2,collision=True)
# Large square album faces dominate wall space, replacing the heavy bookcase.
for name,pos,dims,yaw in [
 ('Rock sleeve wall',(-2.50,3.31,1.12),(1.65,.15,1.20),math.pi/2),
 ('Jazz sleeve wall',(-2.50,5.08,1.12),(1.43,.15,1.05),math.pi/2),
 ('Used sleeve wall',(2.50,2.70,.86),(1.82,.15,1.30),-math.pi/2),
 ('Staff picks behind counter',(-.70,8.045,1.38),(1.53,.17,1.11),0)]:
 place(N+'/RS401-FaceOutLPDisplay.glb',name,pos,'Paper',dims=dims,yaw=yaw)
# The actual cashier work cluster makes this an operating shop.
counter=prop('RS203-CashDesk','Cashier counter',(-.68,7.08,0),dims=(1.82,.74,.94),collision=True);ct=bounds(counter)[1][2]
place(N+'/RS405-CashierWorkKit.glb','Register receipts tape pens staff cup',(-.93,7.06,ct),'Clutter',width=.72)
prop('RS207-Thermos','Staff thermos',(-1.42,7.34,ct),height=.28,group='Clutter')
prop('RS208-Pothos','Small cashier plant',(-1.49,7.38,ct),height=.30,group='Clutter')
place(L+'/B10-Zines.glb','Work in progress sleeves',(-.22,7.06,ct),'Paper',width=.27,yaw=.19)
prop('RS213-TapeCrate','Counter lower tape stock',(-1.03,7.11,.16),width=.43,group='Clutter')
place(L+'/B10-Zines.glb','Counter lower fanzines',(-.28,7.07,.16),'Paper',width=.36,yaw=.10)
# Listening is a small rear shop service, not the room's domestic centerpiece.
desk=prop('RS209-ListeningDesk','Listening desk',(2.16,5.99,0),dims=(1.70,.68,.79),yaw=-math.pi/2,collision=True);dt=bounds(desk)[1][2]
prop('RS204-Turntable','Listening turntable',(2.10,5.94,dt),width=.47,yaw=-math.pi/2,group='Audio')
for name,y in [('Near speaker',5.39),('Far speaker',6.60)]:prop('RS211-HifiSpeaker',name,(2.25,y,dt),height=.28,yaw=-math.pi/2,group='Audio')
prop('RS206-DeskLamp','Listening amber lamp model',(2.38,6.29,dt),height=.36,yaw=-math.pi/2,group='Clutter')
prop('RS210-ListeningChair','Listening chair',(1.38,6.13,0),height=.73,yaw=math.pi/2+.06,collision=True)
prop('RS213-TapeCrate','Listening local tapes',(2.15,6.34,0),width=.36,group='Clutter')
prop('RS212-CassetteStereo','Demo cassette player',(2.23,5.26,dt),width=.32,yaw=-math.pi/2,group='Audio')
prop('RS205-DeskFan','Staff fan',(-1.77,7.62,0),height=.50,yaw=.40,group='Clutter')
place('assets/d22/meshy/P06-Guitar.glb','Shop guitar',(2.22,7.21,0),'Audio',height=.98,yaw=-.30,collision=True)
place('assets/d22/meshy/A01-Amplifier.glb','Shop amp',(2.23,7.77,0),'Audio',width=.42,yaw=-.2,collision=True)
# Stock is deliberately in different stages: displayed, being sorted, and newly received.
for i,(x,y,z,yaw) in enumerate([(2.15,1.86,0,-.10),(2.16,2.79,0,.09),(-1.73,6.20,0,.13)]):
 place(N+'/RS406-UsedLPCarton.glb',f'Incoming used carton {i}',(x,y,z),'Clutter',width=.51,yaw=yaw,collision=True)
for i,(x,y,yaw) in enumerate([(-.45,2.74,-.025),(.38,3.46,math.pi+.045)]):
 # Low stock crates sit on the lower shelves, not in the walking route.
 place(N+'/RS403-UsedRecordCrate.glb',f'Island lower stock {i}',(x,y,.13),'Clutter',dims=(.54,.47,.40),yaw=yaw)
# Loose hero LPs with visible black discs supply readable music-related silhouettes.
albums=['RS402-VinylAlbum','RS407-VinylJazz']
for i,(pos,yaw,h) in enumerate([
 ((-.58,2.37,.74),-.08,.31),((.26,2.37,.74),.07,.32),
 ((-.36,3.76,.74),math.pi+.04,.31),((.49,3.75,.74),math.pi-.08,.30),
 ((-1.88,2.65,.70),math.pi/2-.10,.32),((-1.88,4.07,.72),math.pi/2+.09,.31),
 ((-1.88,5.45,.71),math.pi/2-.07,.32),
 ((2.03,1.68,.17),-.18,.32),((2.16,2.54,.17),-.13,.31),
 ((-.12,7.33,ct),-.08,.31),((2.35,5.65,dt),-math.pi/2,.31)
]):place(N+'/'+albums[i%len(albums)]+'.glb',f'Featured vinyl {i}',pos,'Paper',height=h,yaw=yaw)
# One album laid down mid-pricing; only its source mesh is transformed.
place(N+'/RS402-VinylAlbum.glb','Album being priced',(-.46,6.86,ct),'Paper',width=.39,rotation=(math.pi/2,0,.12))
def framed_photo(name,pos,h,yaw,index):
 path=L+'/B07-GalleryFrame.glb';r=place(path,name,pos,'Paper',height=h,yaw=yaw)
 _,lo,hi=template(path);ext=hi-lo
 m=bpy.data.materials.new('Meshy band photo '+str(index));m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.88
 tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/f'assets/d22/v9/textures/band-{index%6}.jpg'),check_existing=True);tex.image.pack()
 m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
 for o in r.children:
  o.data=o.data.copy();o.data.materials.append(m);slot=len(o.data.materials)-1
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
  for poly in o.data.polygons:
   center=o.matrix_basis@poly.center
   nx=(center.x-lo.x)/ext.x;nz=(center.z-lo.z)/ext.z
   normal=o.matrix_basis.to_3x3()@poly.normal
   if abs(normal.y)>.65 and .075<nx<.925 and .075<nz<.925:
    poly.material_index=slot
    for l in poly.loop_indices:
     v=o.matrix_basis@o.data.vertices[o.data.loops[l].vertex_index].co
     uv.data[l].uv=(((v.x-lo.x)/ext.x-.075)/.85,((v.z-lo.z)/ext.z-.075)/.85)
 r['artwork_source']='Meshy nano-banana-2 task 01a0d7e5-8314-7177-936c-757f8c1de777'
# A few music flyers remain; wall stock now carries more visual weight than framed decor.
framed_photo('Local gig poster',(-2.18,1.87,.04),.68,math.pi/2+.12,3)
framed_photo('Counter gig flyer',(.44,8.04,1.74),.52,0,4)
framed_photo('Listening gig flyer',(2.51,6.14,1.48),.66,-math.pi/2,1)
framed_photo('Listening small flyer',(2.51,6.84,1.80),.43,-math.pi/2,5)

def category(text,key,pos,width=.34,yaw=0):
 path=N+'/RS404-CategoryCard.glb';r=place(path,'Category '+key,pos,'Paper',width=width,yaw=yaw)
 _,lo,hi=template(path);ext=hi-lo
 m=bpy.data.materials.new('Category ink '+key);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Roughness'].default_value=.94
 tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/'assets/recordshop/v4/labels'/f'{key}.png'),check_existing=True);tex.image.pack();m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
 for o in r.children:
  o.data=o.data.copy();o.data.materials.append(m);slot=len(o.data.materials)-1
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='Label UV')
  for poly in o.data.polygons:
   # UVs stay in the Meshy card's local frame. Using world-space x/z here
   # mirrors labels whenever a card is rotated to face the other aisle.
   normal=poly.normal
   nx=(poly.center.x-lo.x)/ext.x;nz=(poly.center.z-lo.z)/ext.z
   if abs(normal.y)>.62 and .025<nx<.975 and nz>.21:
    poly.material_index=slot
    for l in poly.loop_indices:
     v=o.data.vertices[o.data.loops[l].vertex_index].co
     u=1-(v.x-lo.x)/ext.x
     if normal.y<0: u=1-u
     uv.data[l].uv=(u,((v.z-lo.z)/ext.z-.21)/.79)
 r['label_text']=text
 return r
for text,key,pos,width,yaw in [
 ('NEW ARRIVALS','new',(-.47,2.65,.96),.42,-.035),
 ('ROCK','rock',(.39,2.65,.96),.31,.025),
 ('JAZZ','jazz',(-.47,3.50,.96),.30,math.pi+.04),
 ('ELECTRONIC','electronic',(.37,3.50,.96),.41,math.pi-.03),
 ('ROCK','rock_wall',(-2.07,2.99,.95),.32,math.pi/2-.025),
 ('SOUL','soul',(-2.09,4.37,.95),.30,math.pi/2+.08),
 ('USED','used',(-2.10,5.66,.95),.30,math.pi/2-.06),
 ('USED 35','used35',(2.15,2.27,.38),.30,-math.pi/2+.08),
 ('LOCAL DEMO','demo',(2.10,5.22,dt+.18),.30,-math.pi/2),
 ('CASH','cash',(-.08,6.80,ct),.28,-.035),
 ('OPEN 12-22','open',(-1.08,.005,1.65),.31,math.pi),
 ('STAFF PICKS','staff',(-.73,8.01,2.52),.40,0)
]:category(text,key,pos,width,yaw)

# Cold daylight stays near the entrance. Warm cashier and listening pools are the focus.
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,pos,target,power,color,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);C['Lights'].objects.link(o);o.location=pos;aim(o,target)
light('Window afternoon',(-2.36,1.1,2.13),(-.25,2.70,.75),92,(.60,.73,1),1.65)
light('Doorway daylight',(.65,.05,2.2),(0,3.2,.8),55,(.67,.80,1),1.50)
light('Browse warm pool',(-.2,3.25,2.55),(-.2,3.25,.85),105,(1,.85,.65),1.7)
light('Fluorescent sage',(0,5.60,2.54),(-.3,5.3,.5),95,(.79,.94,.81),1.8)
light('Listening amber lamp',(2.19,6.27,1.14),(1.9,6.05,.77),20,(1,.53,.22),.3)
light('Counter practical',(-.67,6.85,2.53),(-.65,7.13,1.0),115,(1,.75,.48),1.20)
light('Staff picks wash',(-.65,7.1,2.48),(-.70,8.0,1.95),45,(1,.81,.57),1.20)
light('Front ceiling bounce',(0,1.9,2.60),(0,2,.3),15,(.82,.83,.73),2.50)
light('Shelf bounce',(1.98,3.0,2.25),(2.40,2.75,1.4),14,(1,.75,.53),1.0)
# The right hand wall is deliberately kept readable from the browse camera;
# this low fill prevents the rear aisle from collapsing into a black void.
light('Right rear wall fill',(2.48,5.35,2.05),(2.15,5.35,1.40),78,(.72,.78,1),1.35)
w=bpy.data.worlds.new('Hutong ambient');w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.08,.10,.12,1);w.node_tree.nodes['Background'].inputs[1].default_value=.09;s.world=w

def camera(name,pos,target,lens):
 d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);C['Cameras'].objects.link(o);o.location=pos;aim(o,target);d.lens=lens;d.clip_start=.03;return o
entry=camera('Entry',(1.03,.56,1.58),(-.20,5.25,1.16),24)
browse=camera('Browse',(-1.33,4.52,1.57),(.10,6.28,1.08),28)
cash=camera('Cashier',(.27,5.64,1.52),(-.71,7.08,1.19),30)
listen=camera('Listening',(.50,5.0,1.55),(2.03,6.0,1.00),29)
s.camera=entry;s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
s.render.resolution_x=1440;s.render.resolution_y=960;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.35
s['rule']='All visible geometry imported from Meshy GLB assets. No authored primitives. Legible category lettering added as textures on Meshy card meshes.'
route=[[1.03,.56],[1.30,2.25],[1.30,4.38],[.0,4.68],[-1.32,4.15],[-1.32,2.14],[-.15,1.52],[1.03,.56]]
branch=[[1.30,4.38],[.3,4.9],[.35,5.6],[.50,6.5],[.65,7.66],[1.63,7.72]]
manifest=dict(version=4,era='2003 Beijing',scene=s.name,visitor_route_xy=route,exit_route_xy=branch,dimensions_m=[W,D,H],assets=records,sources=[dict(path=p,sha256=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()) for p in sorted(cache)],camera_views=[dict(name=o.name,position=list(o.location),direction=list(o.rotation_euler.to_matrix()@Vector((0,0,-1))),lens=o.data.lens) for o in [entry,browse,cash,listen]],story_anchors=dict(shopkeeper=[-.68,7.60,1.40],bottle=[-.35,6.82,.94],exit=[1.63,7.85,1.40]),rule=s['rule'])
folder=ROOT/'design/recordshop-v4';folder.mkdir(parents=True,exist_ok=True);(folder/'layout.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
assert all(o.get('meshy_asset') and (ROOT/o['source_asset']).exists() for o in s.objects if o.type=='MESH')
file=ROOT/'blender/source/D22_RecordShop_v4_MeshyOnly.blend';bpy.ops.wm.save_as_mainfile(filepath=str(file))
import sys
if '--draft' in sys.argv:s.cycles.samples=16;s.render.resolution_percentage=70
for cam,n in [(entry,'entry'),(browse,'browse'),(cash,'cash'),(listen,'listening')]:s.camera=cam;s.render.filepath=str(OUT/(n+'.png'));bpy.ops.render.render(write_still=True)
s.camera=entry;bpy.ops.wm.save_as_mainfile(filepath=str(file));print(json.dumps(dict(mesh_instances=sum(o.type=='MESH' for o in s.objects),unique_meshy_assets=len(cache),provenance='PASS')))
