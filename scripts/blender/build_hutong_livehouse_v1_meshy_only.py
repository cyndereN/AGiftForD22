"""Meshy-only Beijing hutong music block draft.
Visible geometry is imported from existing Meshy GLBs; this script only assembles,
scales, rotates, applies existing/texture materials, lights and cameras.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Euler
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'blender/verification/hutong-livehouse-v1'; OUT.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'assets'
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene; s.name='D22_HutongLiveHouse_v1_MeshyOnly'; s.unit_settings.system='METRIC'
COL={}
for n in ['Architecture','RecordShop','Courtyard','LiveHouse','Life','Paper','Audio','Lights','Cameras']:
 c=bpy.data.collections.new('Meshy '+n); s.collection.children.link(c); COL[n]=c
cache={}; records=[]
def template(path):
 path=str(path)
 if path in cache:return cache[path]
 old=set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=path); bpy.context.view_layer.update()
 added=set(bpy.data.objects)-old; meshes=[o for o in added if o.type=='MESH']
 pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
 lo=Vector(tuple(min(p[i] for p in pts) for i in range(3))); hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
 tmp=[(o.data,o.matrix_world.copy()) for o in meshes]
 for o in added: bpy.data.objects.remove(o,do_unlink=True)
 cache[path]=(tmp,lo,hi); return cache[path]
def bounds(root):
 bpy.context.view_layer.update(); pts=[o.matrix_world@Vector(v) for o in root.children for v in o.bound_box]
 return [min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]
def place(rel,name,pos,group='Architecture',dims=None,width=None,height=None,yaw=0,rotation=None,collision=False):
 path=ROOT/rel; tmp,lo,hi=template(path); ext=hi-lo
 fac=width/ext.x if width else height/ext.z if height else 1
 scale=tuple(dims[i]/ext[i] for i in range(3)) if dims else (fac,)*3
 rot=Euler(rotation or (0,0,yaw)).to_matrix().to_4x4()
 mat=rot@Matrix.Diagonal((*scale,1))@Matrix.Translation(-Vector(((hi.x+lo.x)/2,(hi.y+lo.y)/2,lo.z)))
 pts=[mat@Vector((x,y,z)) for x in [lo.x,hi.x] for y in [lo.y,hi.y] for z in [lo.z,hi.z]]
 fix=Vector((pos[0]-(min(v.x for v in pts)+max(v.x for v in pts))/2,pos[1]-(min(v.y for v in pts)+max(v.y for v in pts))/2,pos[2]-min(v.z for v in pts)))
 root=bpy.data.objects.new(name,None); COL[group].objects.link(root); root.matrix_world=Matrix.Translation(fix)@mat
 root['source_asset']=rel; root['meshy_asset']=True
 for i,(data,matrix) in enumerate(tmp):
  o=bpy.data.objects.new(name+'__mesh'+str(i),data); COL[group].objects.link(o); o.parent=root; o.matrix_parent_inverse=Matrix.Identity(4); o.matrix_basis=matrix
  o['source_asset']=rel; o['meshy_asset']=True
 low,high=bounds(root); records.append(dict(label=name,source=rel,group=group,position=list(pos),bounds_min=low,bounds_max=high,collision=collision)); return root

def aim(o,target): o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,pos,target,energy,color,size,kind='AREA'):
 d=bpy.data.lights.new(name,kind); d.energy=energy; d.color=color; d.shadow_soft_size=size
 if kind=='AREA': d.shape='DISK'; d.size=size
 o=bpy.data.objects.new(name,d); COL['Lights'].objects.link(o); o.location=pos; aim(o,target); return o
def cam(name,pos,target,lens):
 d=bpy.data.cameras.new(name); o=bpy.data.objects.new(name,d); COL['Cameras'].objects.link(o); o.location=pos; aim(o,target); d.lens=lens; d.clip_start=.03; return o

def tile_floor(name,x0,x1,y0,y1):
 # Existing Meshy floor module, repeated as a walkable surface.
 for ix,x in enumerate(range(x0,x1)):
  for iy,y in enumerate(range(y0,y1)):
   place('assets/d22/v9/meshy/B02-TileFloor.glb',f'{name} {ix}_{iy}',(x+.5,y+.5,-.06),'Architecture',dims=(1.0,1.0,.10))
def wall_run(prefix,x,ys,yaw=0):
 for i,y in enumerate(ys):
  place('assets/recordshop/v3/retextured/RS301-HutongWallPanel.glb',f'{prefix} lower {i}',(x,y,0),'Architecture',dims=(1.95,.22,.92),yaw=yaw)
  place('assets/recordshop/v3/retextured/RS310-PlasterOnly.glb',f'{prefix} upper {i}',(x,y,.92),'Architecture',dims=(1.95,.22,2.1),yaw=yaw)
# 1. continuous lane + courtyard + livehouse footprint
# Main lane 0-12; courtyard 12-18; live house 18-29; quiet exit 29-35.
tile_floor('Hutong floor',-3,3,0,36)
wall_run('Left hutong wall',-2.95,range(1,36,2),math.pi/2)
wall_run('Right hutong wall',2.95,range(1,36,2),math.pi/2)
# Low eaves/doors/windows along the lane; door breaks the long wall silhouette.
place('assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb','Record shop red door',(-2.88,7.1,0),'Architecture',dims=(.95,.18,2.14),yaw=math.pi/2)
place('assets/recordshop/v3/retextured/RS302-HutongWindowBay.glb','Record shop window',(-2.87,8.65,.43),'Architecture',dims=(1.55,.2,1.75),yaw=math.pi/2)
place('assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb','Courtyard gate',(-2.88,13.9,0),'Architecture',dims=(1.05,.18,2.18),yaw=math.pi/2)
place('assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb','Livehouse door',(2.88,19.15,0),'Architecture',dims=(1.05,.18,2.18),yaw=-math.pi/2)
place('assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb','Night exit door',(2.88,31.7,0),'Architecture',dims=(1.05,.18,2.18),yaw=-math.pi/2)
# Meshy gate fragments interrupt the long sightline at the courtyard and venue
# thresholds. The middle remains open so the player can choose a side route.
for gate_y,label in [(11.75,'Shop courtyard threshold'),(18.05,'Courtyard livehouse threshold')]:
 for gx in (-2.10,2.10):
  place('assets/recordshop/v3/retextured/RS301-HutongWallPanel.glb',label+' lower '+str(gx),(gx,gate_y,0),'Architecture',dims=(1.65,.22,.92))
  place('assets/recordshop/v3/retextured/RS310-PlasterOnly.glb',label+' upper '+str(gx),(gx,gate_y,.92),'Architecture',dims=(1.65,.22,2.1))
 place('assets/recordshop/v3/retextured/RS303-RedTimberDoor.glb',label+' red gate',(0,gate_y,.0),'Architecture',dims=(.9,.18,2.12))
# Close the quiet lane with a visible Meshy end wall; the final camera now
# looks back toward the exit door instead of into an unbounded black void.
place('assets/recordshop/v3/retextured/RS301-HutongWallPanel.glb','Night exit end wall',(0,35.15,0),'Architecture',dims=(5.8,.22,.92))
place('assets/recordshop/v3/retextured/RS310-PlasterOnly.glb','Night exit end plaster',(0,35.15,.92),'Architecture',dims=(5.8,.22,2.1))
# 2. ordinary life layer in the approach and courtyard
place('assets/d22/meshy/P06-Guitar.glb','Bicycle-like guitar poster prop',(2.35,2.8,0),'Life',height=.95,yaw=.1)
place('assets/recordshop/v2/meshy/RS213-TapeCrate.glb','Courtyard beverage crate',(-1.98,12.95,0),'Courtyard',width=.48)
place('assets/recordshop/v2/meshy/RS208-Pothos.glb','Courtyard plant',(-2.25,14.15,0),'Life',height=.55)
place('assets/d22/v9/meshy/B11-FlightCase.glb','Courtyard equipment case',(1.95,14.65,0),'Courtyard',width=.62,collision=True)
place('assets/recordshop/v2/meshy/RS205-DeskFan.glb','Courtyard fan',(2.15,16.5,0),'Life',height=.55)
# 3. shop doorway/working cluster; v4 stock remains the dense visual anchor.
place('assets/recordshop/v2/meshy/RS201-RecordBin.glb','Shop window crate',(-1.65,6.55,0),'RecordShop',dims=(.95,.70,.95),collision=True)
place('assets/recordshop/v2/meshy/RS201-RecordBin.glb','Shop center crate',(-.25,8.0,0),'RecordShop',dims=(.95,.70,.95),collision=True)
place('assets/recordshop/v2/meshy/RS201-RecordBin.glb','Shop center crate 2',(.95,8.0,0),'RecordShop',dims=(.95,.70,.95),collision=True)
place('assets/recordshop/v2/meshy/RS203-CashDesk.glb','Shop cashier',(.95,9.75,0),'RecordShop',dims=(1.65,.72,.94),collision=True)
place('assets/recordshop/v2/meshy/RS204-Turntable.glb','Shop listening turntable',(.15,10.05,.94),'RecordShop',width=.50)
place('assets/recordshop/v2/meshy/RS210-ListeningChair.glb','Shop listening chair',(-.65,10.15,0),'RecordShop',height=.74,collision=True)
for i,y in enumerate([5.55,6.8,8.1,9.3]):
 place('assets/recordshop/v3/retextured/RS304-LPWallShelf.glb',f'Shop LP shelf {i}',(-2.68,y,.94),'RecordShop',dims=(.18,1.05,1.25),yaw=math.pi/2)
# 4. temporary ticket desk and poster wall in courtyard
place('assets/d22/v9/meshy/B03-BarCounter.glb','Temporary ticket desk',(-.55,14.9,0),'Courtyard',dims=(1.60,.65,.95),collision=True)
for i,y in enumerate([13.2,14.25,15.3]):
 place('assets/d22/architecture/E09_OriginalFlyer.glb',f'Courtyard poster {i}',(2.68,y,1.05),'Paper',height=.72,yaw=-math.pi/2)
place('assets/d22/v9/meshy/B06-BarStool.glb','Ticket stool',(-1.35,15.1,0),'Courtyard',height=.72,collision=True)
# 5. small livehouse: low stage, three observation points, side/backline
place('assets/d22/architecture/E04_Stage_2x1m.glb','Low livehouse stage',(0,25.0,0),'LiveHouse',dims=(4.0,2.0,.34),collision=True)
place('assets/d22/meshy/P04-Drums.glb','Drum kit',(0,25.3,.34),'Audio',width=.95,collision=True)
place('assets/d22/meshy/P05-PA.glb','Left PA',(-2.15,25.1,.34),'Audio',height=1.65,collision=True)
place('assets/d22/meshy/P05-PA.glb','Right PA',(2.15,25.1,.34),'Audio',height=1.65,collision=True)
place('assets/d22/meshy/P08-Microphone.glb','Vocal mic',(0,24.45,.34),'Audio',height=1.40)
place('assets/d22/meshy/P06-Guitar.glb','Live guitar',(-1.0,25.15,.34),'Audio',height=1.05)
place('assets/d22/meshy/A01-Amplifier.glb','Backline amp',(1.05,25.2,.34),'Audio',width=.62)
place('assets/d22/meshy/P07-Bar.glb','Small bar',(3.75,22.0,0),'LiveHouse',dims=(1.70,.72,1.05),collision=True)
place('assets/d22/meshy/P02-FOH.glb','FOH table',(-3.8,21.25,0),'LiveHouse',width=1.05,collision=True)
place('assets/d22/meshy/A05-Mixer.glb','Mixer',(-3.8,21.25,1.02),'Audio',width=.55)
place('assets/d22/architecture/E06_PracticalLight.glb','Bar green practical',(3.45,22.0,1.40),'LiveHouse',height=.30)
place('assets/d22/architecture/E03_BlackBeam_4m.glb','Livehouse beam',(0,22.4,2.55),'Architecture',width=5.0)
place('assets/d22/architecture/E08_Cable.glb','Stage cable',(-1.0,24.0,.36),'Audio',width=1.4,rotation=(0,0,.2))
# 6. lighting hierarchy: cool lane, warm shop, sodium courtyard, red stage, blue exit.
light('Hutong cool daylight',(-2.2,2.0,3.5),(0,5,.8),240,(.48,.67,1.0),2.5)
light('Shop window wash',(-2.55,8.6,2.0),(0,8.2,.8),180,(.65,.78,1.0),1.2)
light('Shop warm counter',(.75,9.7,2.7),(.7,9.7,.8),220,(1.0,.67,.35),1.0)
light('Courtyard sodium lamp',(-1.0,14.5,2.9),(0,15,.8),125,(1.0,.48,.22),1.3)
light('Livehouse red stage',(0,24.0,3.2),(0,25,.5),320,(1.0,.11,.06),1.1)
light('Livehouse amber side',(-3.2,25.5,2.2),(0,25,.8),160,(1.0,.53,.20),1.0)
light('Green bar practical',(3.4,22.0,1.5),(3.0,22.0,.8),40,(.25,1.0,.55),.4,'POINT')
light('Night exit moon',(2.0,31.0,3.3),(0,33,.8),95,(.36,.58,1.0),2.0)
light('Night exit distant city',(0,34.0,2.0),(0,31,.7),45,(1.0,.30,.12),2.0)
w=bpy.data.worlds.new('Hutong night ambient');w.use_nodes=True;w.node_tree.nodes['Background'].inputs[0].default_value=(.03,.045,.07,1);w.node_tree.nodes['Background'].inputs[1].default_value=.08;s.world=w
cams=[cam('hutong_entry',(0,-.2,1.65),(0,5.2,1.1),28),cam('recordshop_window',(-2.15,6.2,1.55),(-.15,10.2,1.0),30),cam('courtyard_ticket',(1.65,13.6,1.6),(-.4,15.2,1.0),28),cam('stage_side',(.25,20.4,1.65),(0,25.1,1.0),25),cam('night_roof',(.45,29.2,1.65),(2.25,31.7,1.25),30)]
s.camera=cams[0];s.render.engine='BLENDER_EEVEE_NEXT';s.render.resolution_x=1440;s.render.resolution_y=900;s.render.resolution_percentage=80;s.render.image_settings.file_format='PNG';s.view_settings.look='AgX - Medium High Contrast';s.view_settings.exposure=-.4
s['rule']='All visible geometry imported from Meshy GLB assets. Blender only assembles, scales, rotates, reuses textures, lights and cameras.'
manifest=dict(version=1,scene=s.name,time_anchor='2006 Beijing hutong music block',dimensions_m=[6,36,2.9],zones=[dict(id='approach',y=[0,5]),dict(id='record_shop',y=[5,12]),dict(id='courtyard',y=[12,18]),dict(id='livehouse',y=[18,29]),dict(id='night_exit',y=[29,36])],assets=records,sources=sorted({r['source'] for r in records}),cameras=[dict(name=o.name,position=list(o.location),lens=o.data.lens) for o in cams],rule=s['rule'])
folder=ROOT/'design/hutong-livehouse-v1';(folder/'blender-layout.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2));(folder/'sources.json').write_text(json.dumps([dict(path=p,sha256=hashlib.sha256((ROOT/p).read_bytes()).hexdigest()) for p in sorted({r['source'] for r in records})],ensure_ascii=False,indent=2))
file=ROOT/'blender/source/D22_HutongLiveHouse_v1_MeshyOnly.blend';bpy.ops.wm.save_as_mainfile(filepath=str(file))
for c,n in zip(cams,['hutong-entry','recordshop-window','courtyard-ticket','stage-side','night-exit']):s.camera=c;s.render.filepath=str(OUT/(n+'.png'));bpy.ops.render.render(write_still=True)
s.camera=cams[0];bpy.ops.wm.save_as_mainfile(filepath=str(file));print(json.dumps(dict(mesh_instances=sum(o.type=='MESH' for o in s.objects),unique_meshy_assets=len(set(r['source'] for r in records)),provenance='PASS')))
