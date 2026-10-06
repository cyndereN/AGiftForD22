"""Meshy-only scene assembly: transforms, lighting, cameras and provenance."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Euler
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'blender/verification/recordshop-v3';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.name='D22_RecordShop_v3_MeshyOnly';s.unit_settings.system='METRIC'
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
W,D,H=4.3,8.2,2.85
for ix in range(4):
 for iy in range(8):place(L+'/B02-TileFloor.glb',f'Floor {ix}_{iy}',(-1.6125+ix*1.075,.5125+iy*1.025,-.085),'Architecture',dims=(1.085,1.035,.085))
place(L+'/B02-TileFloor.glb','Ceiling',(0,4.1,H),'Architecture',dims=(4.6,8.5,.09),tint=(.08,.095,.09))
wall=R+'/RS301-HutongWallPanel.glb'
for side in [-1,1]:
 for i in range(4):
  place(wall,f'Lower wall {side}_{i}',(side*2.22,1.025+i*2.05,0),'Architecture',dims=(2.08,.22,.92),yaw=side*math.pi/2)
  place(R+'/RS310-PlasterOnly.glb',f'Upper wall {side}_{i}',(side*2.22,1.025+i*2.05,.92),'Architecture',dims=(2.08,.22,H-.92),yaw=side*math.pi/2)
for i,x in enumerate([-1.44,0,1.44]):
 place(wall,f'Back lower {i}',(x,8.28,0),'Architecture',dims=(1.48,.22,.92))
 place(R+'/RS310-PlasterOnly.glb',f'Back upper {i}',(x,8.28,.92),'Architecture',dims=(1.48,.22,H-.92))
place(R+'/RS303-RedTimberDoor.glb','Door to hutong',(1.48,8.02,0),'Architecture',dims=(.93,.16,2.14))
place(R+'/RS302-HutongWindowBay.glb','Front window',(-1.03,-.13,.38),'Architecture',dims=(1.65,.25,2.12),yaw=math.pi)
place(R+'/RS302-HutongWindowBay.glb','Left window',(-2.08,1.55,.76),'Architecture',dims=(1.7,.21,1.62),yaw=math.pi/2)
for i,y in enumerate([1.8,5.7]):place(A+'/RS306-CeilingBeamFluorescent.glb',f'Beam light {i}',(0,y,2.65),'Architecture',dims=(4.31,.24,.16))
prop('RS201-RecordBin','New arrivals island',(-.12,2.06,0),dims=(.92,.59,.89),yaw=-.07,collision=True)
prop('RS201-RecordBin','Front browsing bin',(-1.36,2.02,0),dims=(1.15,.70,1.04),yaw=math.pi/2,collision=True)
prop('RS201-RecordBin','Mid browsing bin',(-1.36,3.77,0),dims=(1.08,.70,1.02),yaw=math.pi/2-.035,collision=True)
for y,h in [(5.50,2.06),(6.63,1.90)]:prop('RS202-CDTower','CD wall '+str(y),(-1.98,y,0),height=h,yaw=math.pi/2,collision=True)
place(R+'/RS304-LPWallShelf.glb','Right shelves',(1.95,1.93,0),dims=(1.65,.38,1.94),yaw=-math.pi/2,collision=True)
counter=prop('RS203-CashDesk','Rear cashier counter',(-.62,7.13,0),dims=(1.65,.75,.94),collision=True);ct=bounds(counter)[1][2]
prop('RS212-CassetteStereo','Cashier cassette stereo',(-.76,7.13,ct),width=.48,group='Audio')
prop('RS207-Thermos','Counter thermos',(-1.23,7.12,ct),height=.31,group='Clutter')
prop('RS213-TapeCrate','Cashier lower shelf crate',(-.89,7.13,.16),width=.48,group='Clutter')
place(L+'/B10-Zines.glb','Cashier lower shelf zines',(-.28,7.07,.16),'Paper',width=.36,yaw=.15)
place(L+'/B10-Zines.glb','Counter fanzines',(-.12,7.02,ct),'Paper',width=.29,yaw=.12)
desk=prop('RS209-ListeningDesk','Listening desk',(1.73,4.79,0),dims=(1.87,.73,.79),yaw=-math.pi/2,collision=True);dt=bounds(desk)[1][2]
prop('RS204-Turntable','Turntable',(1.63,4.76,dt),width=.51,yaw=-math.pi/2,group='Audio')
prop('RS211-HifiSpeaker','Near speaker',(1.77,4.13,dt),height=.34,yaw=-math.pi/2+.06,group='Audio')
prop('RS211-HifiSpeaker','Far speaker',(1.77,5.48,dt),height=.34,yaw=-math.pi/2-.08,group='Audio')
prop('RS206-DeskLamp','Listening lamp',(1.98,5.09,dt),height=.41,yaw=-math.pi/2,group='Clutter')
prop('RS210-ListeningChair','Listening chair',(.91,4.92,0),height=.84,yaw=math.pi/2,collision=True)
prop('RS213-TapeCrate','Entry cassette crate',(-1.63,.77,0),width=.45,yaw=-.10,group='Clutter')
prop('RS213-TapeCrate','Under table crate',(1.72,5.23,0),width=.40,yaw=.03,group='Clutter')
prop('RS208-Pothos','Counter plant',(-1.30,7.40,ct),height=.42,group='Clutter')
prop('RS205-DeskFan','Counter fan',(-.28,7.43,ct),height=.34,yaw=-.4,group='Clutter')
place('assets/d22/meshy/P06-Guitar.glb','Red guitar on stand',(1.65,6.55,0),'Audio',height=1.04,yaw=-.3,collision=True)
place('assets/d22/meshy/A01-Amplifier.glb','Small guitar amp',(1.50,7.14,0),'Audio',width=.49,yaw=-.30,collision=True)
place(L+'/B10-Zines.glb','Zines near bins',(-1.60,2.82,0),'Paper',width=.36,yaw=.25)
for i,z in enumerate([.10,.72,1.32]):
 for j,y in enumerate([1.36,1.94,2.5]):
  if (i+j)%2:place(L+'/B10-Zines.glb',f'Shelf zines {i}_{j}',(1.89,y,z),'Paper',width=.24,yaw=.18)
  else:prop('RS213-TapeCrate',f'Shelf crate {i}_{j}',(1.89,y,z),width=.27,yaw=-math.pi/2,group='Clutter')
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
for i,(x,z,h) in enumerate([(-1.60,1.50,.54),(-.97,1.76,.74),(-.28,1.42,.59),(.37,1.87,.53)]):framed_photo(f'Rear band photo {i}',(x,8.045,z),h,0,i)
for i,(y,z,h) in enumerate([(3.17,1.30,.62),(3.65,2.03,.48),(4.32,1.55,.80),(5.28,1.40,.72),(5.94,2.05,.50)]):framed_photo(f'Listening band photo {i}',(2.075,y,z),h,-math.pi/2,i+1)
for i,(y,z,h) in enumerate([(2.8,1.50,.70),(4.3,1.60,.61)]):framed_photo(f'Left band photo {i}',(-2.06,y,z),h,math.pi/2,i+3)

def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,pos,target,power,color,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);C['Lights'].objects.link(o);o.location=pos;aim(o,target)
light('Window afternoon',(-1.94,1.5,2.12),(-.15,3.9,.65),180,(.64,.77,1),1.5)
light('Doorway daylight',(.55,.05,2.2),(0,4.2,.8),130,(.69,.81,1),1.5)
light('Fluorescent sage',(0,5.70,2.54),(0,5.3,.5),125,(.75,.93,.79),1.8)
light('Listening amber lamp',(1.8,5.02,1.19),(1.55,4.7,.77),22,(1,.49,.18),.30)
light('Counter practical',(-.7,7.3,2.5),(-.7,7.15,.8),60,(1,.69,.39),.65)
light('Front ceiling bounce',(0,2,2.66),(0,2,.3),35,(.87,.86,.68),2.5)
light('Shelf bounce',(1.65,2,2.3),(1.75,2,.7),20,(1,.66,.38),1.0)
w=bpy.data.worlds.new('Hutong muted ambient');w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.085,.11,.12,1);w.node_tree.nodes['Background'].inputs[1].default_value=.12;s.world=w
def camera(name,pos,target,lens):
 d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);C['Cameras'].objects.link(o);o.location=pos;aim(o,target);d.lens=lens;d.clip_start=.03;return o
entry=camera('Entry',(.62,.55,1.53),(-.08,5.80,1.35),25)
listen=camera('Listening',(-.38,3.63,1.48),(1.64,4.82,1.02),33)
cash=camera('Cashier',(.5,5.52,1.51),(-.64,7.15,1.30),32)
s.camera=entry;s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
s.render.resolution_x=1440;s.render.resolution_y=960;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.view_settings.view_transform='AgX';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.5
s['rule']='All visible geometry imported from Meshy GLB assets. No authored primitives.'
manifest=dict(version=3,scene=s.name,visitor_route_xy=[[0.62, 0.55], [0.96, 2.06], [0.23, 3.2], [-0.15, 5.35], [0.45, 6.45], [0.65, 7.65], [1.4, 7.75]],dimensions_m=[W,D,H],assets=records,sources=[dict(path=p,sha256=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()) for p in sorted(cache)],camera_views=[dict(name=o.name,position=list(o.location),direction=list(o.rotation_euler.to_matrix()@Vector((0,0,-1))),lens=o.data.lens) for o in [entry,listen,cash]],rule=s['rule'])
folder=ROOT/'design/recordshop-v3';folder.mkdir(parents=True,exist_ok=True);(folder/'layout.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
assert all(o.get('meshy_asset') and (ROOT/o['source_asset']).exists() for o in s.objects if o.type=='MESH')
file=ROOT/'blender/source/D22_RecordShop_v3_MeshyOnly.blend';bpy.ops.wm.save_as_mainfile(filepath=str(file))
for cam,n in [(entry,'entry'),(listen,'listening'),(cash,'cash')]:s.camera=cam;s.render.filepath=str(OUT/(n+'.png'));bpy.ops.render.render(write_still=True)
s.camera=entry;bpy.ops.wm.save_as_mainfile(filepath=str(file));print(json.dumps(dict(mesh_instances=sum(o.type=='MESH' for o in s.objects),unique_meshy_assets=len(cache),provenance='PASS')))
